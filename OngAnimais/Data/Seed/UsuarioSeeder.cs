using Dapper;
using OngAnimais.Data;

namespace OngAnimais.Data.Seed
{
    /// <summary>
    /// Popula a tabela 'usuarios' com dados de exemplo para desenvolvimento.
    /// Idempotente: não insere nada se a tabela já tiver registros.
    /// </summary>
    public class UsuarioSeeder
    {
        private readonly DbConnectionFactory _dbFactory;
        private readonly ILogger<UsuarioSeeder> _logger;

        /// <summary>
        /// Senha em texto puro usada por todos os usuários de exemplo.
        /// Apenas para desenvolvimento — facilita o login durante os testes.
        /// </summary>
        public const string SenhaPadrao = "Senha@123";

        /// <summary>
        /// Inicializa o seeder com as dependências necessárias.
        /// </summary>
        /// <param name="dbFactory">Fábrica de conexão com o banco.</param>
        /// <param name="logger">Logger para registrar o progresso do seed.</param>
        public UsuarioSeeder(DbConnectionFactory dbFactory, ILogger<UsuarioSeeder> logger)
        {
            _dbFactory = dbFactory;
            _logger = logger;
        }

        /// <summary>
        /// Insere os usuários de exemplo, caso a tabela esteja vazia.
        /// </summary>
        public async Task SeedAsync()
        {
            using var connection = _dbFactory.CreateConnection();

            var total = await connection.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM usuarios");
            if (total > 0)
            {
                _logger.LogInformation("Seed de usuários ignorado: tabela já possui {Total} registro(s).", total);
                return;
            }

            var senhaHash = BCrypt.Net.BCrypt.HashPassword(SenhaPadrao);

            var usuarios = new[]
            {
                new { Nome = "Administrador ONG", Email = "admin@onganimais.dev",  Perfil = Enums.PerfilUsuario.Admin.ToString() },
                new { Nome = "Maria Silva",       Email = "maria.silva@email.dev", Perfil = Enums.PerfilUsuario.Comum.ToString() },
                new { Nome = "João Pereira",      Email = "joao.pereira@email.dev", Perfil = Enums.PerfilUsuario.Comum.ToString() },
                new { Nome = "Ana Souza",         Email = "ana.souza@email.dev",   Perfil = Enums.PerfilUsuario.Comum.ToString() }
            };

            const string sql = @"
                INSERT INTO usuarios (nome, email, senha_hash, perfil, ativo, created_at, updated_at)
                VALUES (@Nome, @Email, @SenhaHash, @Perfil, TRUE, NOW(), NOW())";

            foreach (var usuario in usuarios)
            {
                await connection.ExecuteAsync(sql, new { usuario.Nome, usuario.Email, SenhaHash = senhaHash, usuario.Perfil });
            }

            _logger.LogInformation(
                "Seed de usuários concluído: {Total} usuário(s) inserido(s). Senha padrão: {Senha}",
                usuarios.Length, SenhaPadrao);
        }
    }
}
