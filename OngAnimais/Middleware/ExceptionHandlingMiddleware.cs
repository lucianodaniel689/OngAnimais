using OngAnimais.Common;
using Npgsql;
using OngAnimais.Common.Exceptions;
using System.Diagnostics;
using System.Net;
using System.Text.Json;

namespace OngAnimais.Middleware
{
    /// <summary>
    /// Middleware para tratamento global de exceções.
    /// Converte exceções conhecidas em respostas HTTP padronizadas (<see cref="ApiResponse{T}"/>)
    /// e registra logs com o nível apropriado.
    /// </summary>
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        /// <summary>
        /// Inicializa o middleware de tratamento de exceções.
        /// </summary>
        /// <param name="next">Próximo middleware no pipeline.</param>
        /// <param name="logger">Logger para registro de erros.</param>
        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        /// <summary>
        /// Executa o middleware, capturando exceções não tratadas.
        /// </summary>
        /// <param name="context">Contexto HTTP da requisição.</param>
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            // Mapeia o tipo da exceção para o status HTTP e a mensagem exibida ao cliente.
            // UnauthorizedAccessException (falha de permissão de arquivo do .NET) cai de propósito
            // no 500 genérico — login inválido usa CredenciaisInvalidasException.
            var (statusCode, mensagem) = exception switch
            {
                RegraDeNegocioException => (HttpStatusCode.BadRequest, exception.Message),
                CredenciaisInvalidasException => (HttpStatusCode.Unauthorized, exception.Message),
                // Perfil revalidado no banco e recusado: mesma resposta do OnForbidden do JWT
                // (404, não 403) — a mensagem da exceção é o motivo técnico, só vai para o log.
                AcessoNegadoException => (HttpStatusCode.NotFound, "Recurso não encontrado."),
                // Violação de UNIQUE que escapou da checagem prévia do módulo (ex.: dois cadastros
                // simultâneos com o mesmo email) — é conflito de dados, não erro de servidor.
                PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } =>
                    (HttpStatusCode.BadRequest, "Já existe um registro com esses dados."),
                _ => (HttpStatusCode.InternalServerError, "Ocorreu um erro interno no servidor.")
            };

            // Erros de negócio (400/401/404) são esperados → Warning. Erros inesperados → Error.
            if (statusCode == HttpStatusCode.InternalServerError)
            {
                _logger.LogError(exception, "Erro não tratado em {Path}: {Message}", context.Request.Path, exception.Message);
                // Telemetria: a exceção é engolida aqui, então a instrumentação do ASP.NET Core só
                // veria o status 500 — registra o tipo/stack na span da requisição (no-op sem OTel).
                Activity.Current?.AddException(exception);
            }
            else
                _logger.LogWarning("Requisição rejeitada ({StatusCode}) em {Path}: {Message}",
                    (int)statusCode, context.Request.Path, exception.Message);

            // Se a resposta já começou a ser enviada, não há como reescrever os headers.
            if (context.Response.HasStarted)
            {
                _logger.LogWarning("A resposta já foi iniciada; não foi possível padronizar o erro.");
                return;
            }

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            var response = ApiResponse<object>.Erro(mensagem);
            var json = JsonSerializer.Serialize(response, JsonOptions);

            await context.Response.WriteAsync(json);
        }
    }
}
