using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi;
using System.Reflection;

namespace OngAnimais.Configurations
{
    /// <summary>
    /// Extensão para configuração do Swagger/OpenAPI.
    /// </summary>
    public static class SwaggerConfig
    {
        /// <summary>
        /// Configura o Swagger com suporte a autenticação JWT e documentação XML.
        /// </summary>
        /// <param name="services">Coleção de serviços.</param>
        /// <returns>Coleção de serviços.</returns>
        public static IServiceCollection AddSwaggerConfiguration(this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();

            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "OngAnimais API",
                    Version = "v1",
                    Description = "API para gerenciamento de ONG de animais."
                });

                // IDs de operação no formato "Controller_Action".
                // Permite ao NSwag gerar clients limpos (ex: AnimalClient.listar(), obterPorId()).
                options.CustomOperationIds(apiDesc =>
                    apiDesc.ActionDescriptor is ControllerActionDescriptor descritor
                        ? $"{descritor.ControllerName}_{descritor.ActionName}"
                        : null);

                // Nomes únicos e legíveis para tipos genéricos (ApiResponse<T>, ResultadoPaginado<T>).
                // Necessário porque o OpenAPI não tem genéricos — sem isso há conflito de schemaId.
                options.CustomSchemaIds(GerarSchemaId);

                // Configuração de JWT no Swagger
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Insira o token JWT. Exemplo: Bearer {seu_token}"
                });

                options.AddSecurityRequirement(document =>
                {
                    var requirement = new OpenApiSecurityRequirement();
                    var schemeRef = new OpenApiSecuritySchemeReference("Bearer", document);
                    requirement.Add(schemeRef, new List<string>());
                    return requirement;
                });

                // Documentação XML
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    options.IncludeXmlComments(xmlPath);
                }
            });

            return services;
        }

        /// <summary>
        /// Habilita o Swagger UI no pipeline HTTP (apenas para Development).
        /// </summary>
        /// <param name="app">Aplicação web.</param>
        /// <returns>Aplicação web.</returns>
        public static WebApplication UseSwaggerConfiguration(this WebApplication app)
        {
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(options =>
                {
                    options.SwaggerEndpoint("/swagger/v1/swagger.json", "OngAnimais API v1");
                });
            }

            return app;
        }

        /// <summary>
        /// Gera um schemaId único e legível para o OpenAPI, tratando tipos genéricos
        /// (ex: <c>ApiResponse&lt;AnimalRespostaDto&gt;</c> → <c>ApiResponseOfAnimalRespostaDto</c>).
        /// </summary>
        /// <param name="type">Tipo a ser nomeado.</param>
        /// <returns>Nome do schema.</returns>
        private static string GerarSchemaId(Type type)
        {
            if (!type.IsGenericType)
                return type.Name;

            var nomeBase = type.Name.Split('`')[0];
            var argumentos = string.Join("And", type.GetGenericArguments().Select(GerarSchemaId));
            return $"{nomeBase}Of{argumentos}";
        }
    }
}
