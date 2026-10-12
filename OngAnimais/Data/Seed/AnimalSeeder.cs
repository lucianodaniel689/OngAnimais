using Dapper;
using OngAnimais.Data;
using OngAnimais.Enums;

namespace OngAnimais.Data.Seed
{
    /// <summary>
    /// Popula a tabela 'animais' com dados de exemplo para desenvolvimento.
    /// Idempotente: não insere nada se a tabela já tiver registros.
    /// </summary>
    public class AnimalSeeder
    {
        private readonly DbConnectionFactory _dbFactory;
        private readonly ILogger<AnimalSeeder> _logger;

        /// <summary>
        /// Inicializa o seeder com as dependências necessárias.
        /// </summary>
        /// <param name="dbFactory">Fábrica de conexão com o banco.</param>
        /// <param name="logger">Logger para registrar o progresso do seed.</param>
        public AnimalSeeder(DbConnectionFactory dbFactory, ILogger<AnimalSeeder> logger)
        {
            _dbFactory = dbFactory;
            _logger = logger;
        }

        /// <summary>
        /// Insere os animais de exemplo, caso a tabela esteja vazia.
        /// </summary>
        public async Task SeedAsync()
        {
            using var connection = _dbFactory.CreateConnection();

            var total = await connection.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM animais");
            if (total > 0)
            {
                _logger.LogInformation("Seed de animais ignorado: tabela já possui {Total} registro(s).", total);
                return;
            }

            var animais = new[]
            {
                new
                {
                    Nome = "Rex", Especie = EspecieAnimal.Cachorro, Sexo = SexoAnimal.Macho,
                    IdadeAproximada = "3 anos", Porte = PorteAnimal.Grande,
                    Descricao = "Cão dócil e brincalhão, ótimo com crianças.",
                    Status = StatusAnimal.Disponivel, Castrado = true, Vermifugado = true
                },
                new
                {
                    Nome = "Mia", Especie = EspecieAnimal.Gato, Sexo = SexoAnimal.Femea,
                    IdadeAproximada = "1 ano", Porte = PorteAnimal.Pequeno,
                    Descricao = "Gata carinhosa e independente.",
                    Status = StatusAnimal.Disponivel, Castrado = true, Vermifugado = true
                },
                new
                {
                    Nome = "Thor", Especie = EspecieAnimal.Cachorro, Sexo = SexoAnimal.Macho,
                    IdadeAproximada = "5 anos", Porte = PorteAnimal.Medio,
                    Descricao = "Companheiro tranquilo, já castrado e vacinado.",
                    Status = StatusAnimal.Reservado, Castrado = true, Vermifugado = true
                },
                new
                {
                    Nome = "Luna", Especie = EspecieAnimal.Gato, Sexo = SexoAnimal.Femea,
                    IdadeAproximada = "6 meses", Porte = PorteAnimal.Pequeno,
                    Descricao = "Filhote curiosa em busca de um lar.",
                    Status = StatusAnimal.Adotado, Castrado = false, Vermifugado = true
                },
                new
                {
                    Nome = "Bidu", Especie = EspecieAnimal.Cachorro, Sexo = SexoAnimal.Macho,
                    IdadeAproximada = "2 anos", Porte = PorteAnimal.Pequeno,
                    Descricao = "Em recuperação de uma cirurgia na pata.",
                    Status = StatusAnimal.EmTratamento, Castrado = false, Vermifugado = true
                },
                new
                {
                    Nome = "Pingo", Especie = EspecieAnimal.Ave, Sexo = SexoAnimal.Macho,
                    IdadeAproximada = "1 ano", Porte = PorteAnimal.Pequeno,
                    Descricao = "Calopsita sociável e cantora.",
                    Status = StatusAnimal.Disponivel, Castrado = false, Vermifugado = false
                },
                // Animais extras variados (porte/sexo/idade/espécie) para alimentar os
                // filtros da vitrine do site público.
                new
                {
                    Nome = "Amora", Especie = EspecieAnimal.Cachorro, Sexo = SexoAnimal.Femea,
                    IdadeAproximada = "4 meses", Porte = PorteAnimal.Grande,
                    Descricao = "Filhote esperta e cheia de energia — vai crescer bastante e precisa de espaço.",
                    Status = StatusAnimal.Disponivel, Castrado = false, Vermifugado = true
                },
                new
                {
                    Nome = "Fred", Especie = EspecieAnimal.Cachorro, Sexo = SexoAnimal.Macho,
                    IdadeAproximada = "9 anos", Porte = PorteAnimal.Pequeno,
                    Descricao = "Idoso calmo e educado, adora um sofá e uma soneca ao sol.",
                    Status = StatusAnimal.Disponivel, Castrado = true, Vermifugado = true
                },
                new
                {
                    Nome = "Nina", Especie = EspecieAnimal.Cachorro, Sexo = SexoAnimal.Femea,
                    IdadeAproximada = "2 anos", Porte = PorteAnimal.Medio,
                    Descricao = "Adora passear e correr — companheira certa pra qualquer aventura.",
                    Status = StatusAnimal.Disponivel, Castrado = true, Vermifugado = true
                },
                new
                {
                    Nome = "Salem", Especie = EspecieAnimal.Gato, Sexo = SexoAnimal.Macho,
                    IdadeAproximada = "4 anos", Porte = PorteAnimal.Pequeno,
                    Descricao = "Independente e tranquilo, se vira bem sozinho durante o dia.",
                    Status = StatusAnimal.Disponivel, Castrado = true, Vermifugado = true
                },
                new
                {
                    Nome = "Mel", Especie = EspecieAnimal.Gato, Sexo = SexoAnimal.Femea,
                    IdadeAproximada = "3 meses", Porte = PorteAnimal.Pequeno,
                    Descricao = "Filhote brincalhona — uma bebê que pede cuidado e paciência.",
                    Status = StatusAnimal.Disponivel, Castrado = false, Vermifugado = false
                },
                new
                {
                    Nome = "Chico", Especie = EspecieAnimal.Roedor, Sexo = SexoAnimal.Macho,
                    IdadeAproximada = "8 meses", Porte = PorteAnimal.Pequeno,
                    Descricao = "Porquinho-da-índia dócil, ótimo primeiro pet.",
                    Status = StatusAnimal.Disponivel, Castrado = false, Vermifugado = true
                }
            };

            const string sql = @"
                INSERT INTO animais
                    (nome, especie, sexo, idade_aproximada, porte, descricao, status, castrado, vermifugado, data_cadastro, ativo, updated_at)
                VALUES
                    (@Nome, @Especie, @Sexo, @IdadeAproximada, @Porte, @Descricao, @Status, @Castrado, @Vermifugado, NOW(), TRUE, NOW())";

            foreach (var animal in animais)
            {
                await connection.ExecuteAsync(sql, new
                {
                    animal.Nome,
                    Especie = animal.Especie.ToString(),
                    Sexo = animal.Sexo.ToString(),
                    animal.IdadeAproximada,
                    Porte = animal.Porte.ToString(),
                    animal.Descricao,
                    Status = animal.Status.ToString(),
                    animal.Castrado,
                    animal.Vermifugado
                });
            }

            _logger.LogInformation("Seed de animais concluído: {Total} animal(is) inserido(s).", animais.Length);
        }
    }
}
