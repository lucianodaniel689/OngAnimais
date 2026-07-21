using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using OngAnimais.Common;
using OngAnimais.Common.Auditoria;
using OngAnimais.Configurations;
using OngAnimais.Middleware;
using OngAnimais.Modulos;

var builder = WebApplication.CreateBuilder(args);

// === Configurações de serviços ===
builder.Services.AddDatabaseConfiguration(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
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

var app = builder.Build();

// === Pipeline HTTP ===
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSwaggerConfiguration();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors(CorsConfig.PolicyName);
// Depois do UseStaticFiles de propósito: as fotos em /uploads não consomem a cota de requisições.
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Popula o banco com dados de exemplo (somente em Development e se habilitado).
await app.UseDatabaseSeedAsync();

app.Run();
