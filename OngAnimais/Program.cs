using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using OngAnimais.Common;
using OngAnimais.Common.Auditoria;
using OngAnimais.Configurations;
using OngAnimais.Middleware;
using OngAnimais.Modulos;

var builder = WebApplication.CreateBuilder(args);

// wwwroot só contém uploads/ (fora do git), então num clone/deploy novo a pasta não existe na
// subida — e aí o ASP.NET liga os arquivos estáticos num provider vazio: as fotos enviadas
// dariam 404 até reiniciar a API. Cria a pasta e reaponta o provider antes do UseStaticFiles.
var webRoot = builder.Environment.WebRootPath ?? Path.Combine(builder.Environment.ContentRootPath, "wwwroot");
Directory.CreateDirectory(webRoot);
builder.Environment.WebRootPath = webRoot;
builder.Environment.WebRootFileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(webRoot);

// === Configurações de serviços ===
builder.Services.AddDatabaseConfiguration(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration, builder.Environment);
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        // Padroniza os erros de validação (DataAnnotations) no formato ApiResponse,
        // em vez do ProblemDetails padrão — facilita o consumo pelo frontend.
        options.InvalidModelStateResponseFactory = context =>
        {
            var erros = context.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .SelectMany(e => e.Value!.Errors.Select(err => err.ErrorMessage))
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .ToList();

            var mensagem = erros.Count > 0
                ? string.Join(" ", erros)
                : "Dados inválidos.";

            return new BadRequestObjectResult(ApiResponse<object>.Erro(mensagem));
        };
    });

builder.Services.AddSwaggerConfiguration();
builder.Services.AddCorsConfiguration(builder.Configuration);
builder.Services.AddRateLimitConfiguration(builder.Configuration);
builder.Services.AddSeedConfiguration();
builder.Services.AddArmazenamentoConfiguration(builder.Configuration);

// OpenTelemetry (traces/métricas/logs via OTLP) — só liga se OTEL_EXPORTER_OTLP_ENDPOINT existir.
builder.Services.AddTelemetriaConfiguration(builder.Configuration, builder.Environment);

// Contexto do operador para a auditoria (claims do JWT + IP da requisição)
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ContextoOperador>();

// === Módulos de negócio ===
builder.Services.AddScoped<UsuarioModulo>();
builder.Services.AddScoped<AnimalModulo>();
builder.Services.AddScoped<AdocaoModulo>();
builder.Services.AddScoped<VacinaModulo>();
builder.Services.AddScoped<FotoModulo>();
builder.Services.AddScoped<AuditoriaModulo>();
builder.Services.AddScoped<ConfiguracaoModulo>();

var app = builder.Build();

// === Pipeline HTTP ===
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSwaggerConfiguration();
app.UseHttpsRedirection();
app.UseStaticFiles(new StaticFileOptions
{
    // Fotos em /uploads têm nome GUID e nunca são sobrescritas (editar = arquivo novo), então
    // podem ficar em cache "para sempre" no navegador/CDN: quem volta ao site não baixa de novo.
    OnPrepareResponse = contexto =>
    {
        if (contexto.Context.Request.Path.StartsWithSegments("/uploads"))
            contexto.Context.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
    }
});
app.UseCors(CorsConfig.PolicyName);

// Depois do UseStaticFiles de propósito: as fotos em /uploads não consomem a cota de requisições.
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Popula o banco com dados de exemplo (somente em Development e se habilitado).
await app.UseDatabaseSeedAsync();

app.Run();
