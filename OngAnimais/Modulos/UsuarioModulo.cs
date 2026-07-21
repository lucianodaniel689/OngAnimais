using Dapper;
using OngAnimais.Auth;
using OngAnimais.Common.Auditoria;
using OngAnimais.Common.Exceptions;
using OngAnimais.Common.Paginacao;
using OngAnimais.Data;
using OngAnimais.DTOs;
using OngAnimais.Enums;
using OngAnimais.Models;

namespace OngAnimais.Modulos
{
    /// <summary>
    /// Módulo responsável pela regra de negócio e acesso a dados da entidade Usuário.
    /// </summary>
    public class UsuarioModulo
    {
        private readonly DbConnectionFactory _dbFactory;
        private readonly JwtTokenService _jwtTokenService;
        private readonly AuditoriaModulo _auditoria;

        /// <summary>
        /// Colunas permitidas para ordenação (nome amigável → coluna real).
        /// </summary>
        private static readonly Dictionary<string, string> ColunasOrdenacao = new(StringComparer.OrdinalIgnoreCase)
        {
            ["nome"] = "nome",
            ["email"] = "email",
            ["dataCriacao"] = "created_at"
        };

        /// <summary>
        /// Inicializa o módulo com as dependências necessárias.
        /// </summary>
        /// <param name="dbFactory">Fábrica de conexão com o banco.</param>
        /// <param name="jwtTokenService">Serviço de geração de tokens JWT.</param>
        /// <param name="auditoria">Módulo de auditoria (gestão de contas e eventos de login).</param>
        public UsuarioModulo(DbConnectionFactory dbFactory, JwtTokenService jwtTokenService, AuditoriaModulo auditoria)
        {
            _dbFactory = dbFactory;
            _jwtTokenService = jwtTokenService;
            _auditoria = auditoria;
        }

        /// <summary>
        /// Registra um novo usuário no sistema.
        /// </summary>
        /// <param name="dto">Dados de registro do usuário.</param>
        /// <returns>Dados do usuário criado.</returns>
        public async Task<UsuarioRespostaDto> RegistrarAsync(UsuarioRegistroDto dto)
        {
            using var connection = _dbFactory.CreateConnection();

            // Verificar se o email já está em uso
            var emailExiste = await connection.QueryFirstOrDefaultAsync<int>(
                "SELECT COUNT(1) FROM usuarios WHERE email = @Email",
                new { dto.Email });

            if (emailExiste > 0)
                throw new RegraDeNegocioException("Já existe um usuário cadastrado com este email.");

            // Gerar hash da senha
            var senhaHash = BCrypt.Net.BCrypt.HashPassword(dto.Senha);

            // Inserir usuário no banco
            const string sql = @"
                INSERT INTO usuarios (nome, email, senha_hash, perfil, ativo, created_at, updated_at)
                VALUES (@Nome, @Email, @SenhaHash, @Perfil, TRUE, NOW(), NOW())
                RETURNING id, nome, email, perfil, ativo, created_at";

            var usuario = await connection.QueryFirstAsync<UsuarioModel>(sql, new
            {
                dto.Nome,
                dto.Email,
                SenhaHash = senhaHash,
                Perfil = dto.Perfil!.Value.ToString()
            });

            await _auditoria.RegistrarSeguroAsync(
                CategoriaAuditoria.Usuario, AcoesAuditoria.FuncionarioCriado, usuario.Id,
                $"Nome: {usuario.Nome}; email: {usuario.Email}; perfil: {usuario.Perfil}");

            return MapearParaResposta(usuario);
        }

        /// <summary>
        /// Realiza o login do usuário e retorna um token JWT.
        /// </summary>
        /// <param name="dto">Dados de login do usuário.</param>
        /// <returns>Token JWT gerado.</returns>
        public async Task<string> LoginAsync(UsuarioLoginDto dto)
        {
            using var connection = _dbFactory.CreateConnection();

            const string sql = "SELECT id, nome, email, senha_hash, perfil, ativo FROM usuarios WHERE email = @Email";
            var usuario = await connection.QueryFirstOrDefaultAsync<UsuarioModel>(sql, new { dto.Email });

            // Eventos de segurança gravados de forma tolerante (RegistrarSeguroAsync):
            // uma falha na auditoria não pode impedir (nem causar) um login.
            if (usuario is null)
            {
                await _auditoria.RegistrarSeguroAsync(
                    CategoriaAuditoria.Seguranca, AcoesAuditoria.LoginFalhou,
                    detalhes: $"Email: {dto.Email} (não cadastrado)");
                throw new UnauthorizedAccessException("Email ou senha inválidos.");
            }

            if (!usuario.Ativo)
            {
                await _auditoria.RegistrarSeguroAsync(
                    CategoriaAuditoria.Seguranca, AcoesAuditoria.LoginContaInativa,
                    detalhes: $"Email: {dto.Email}",
                    usuarioId: usuario.Id, usuarioNome: usuario.Nome);
                throw new UnauthorizedAccessException("Usuário inativo. Entre em contato com o administrador.");
            }

            // Verificar senha
            if (!BCrypt.Net.BCrypt.Verify(dto.Senha, usuario.SenhaHash))
            {
                await _auditoria.RegistrarSeguroAsync(
                    CategoriaAuditoria.Seguranca, AcoesAuditoria.LoginFalhou,
                    detalhes: $"Email: {dto.Email} (senha incorreta)",
                    usuarioId: usuario.Id, usuarioNome: usuario.Nome);
                throw new UnauthorizedAccessException("Email ou senha inválidos.");
            }

            await _auditoria.RegistrarSeguroAsync(
                CategoriaAuditoria.Seguranca, AcoesAuditoria.LoginSucesso,
                detalhes: $"Email: {dto.Email}",
                usuarioId: usuario.Id, usuarioNome: usuario.Nome);

            // Gerar token JWT (o perfil vira o claim de role)
            return _jwtTokenService.GerarToken(usuario.Id, usuario.Email, usuario.Nome, usuario.Perfil.ToString());
        }

        /// <summary>
        /// Lista os usuários de forma paginada, com busca e ordenação.
        /// </summary>
        /// <param name="filtro">Filtros, paginação e ordenação.</param>
        /// <returns>Página de usuários com metadados de paginação.</returns>
        public async Task<ResultadoPaginado<UsuarioRespostaDto>> ListarPaginadoAsync(UsuarioFiltroDto filtro)
        {
            using var connection = _dbFactory.CreateConnection();

            var condicoes = new List<string>();
            var parametros = new DynamicParameters();

            if (!string.IsNullOrWhiteSpace(filtro.Busca))
            {
                condicoes.Add("(nome ILIKE @Busca OR email ILIKE @Busca)");
                parametros.Add("Busca", $"%{filtro.Busca.Trim()}%");
            }
            if (filtro.Ativo is not null)
            {
                condicoes.Add("ativo = @Ativo");
                parametros.Add("Ativo", filtro.Ativo);
            }

            var where = condicoes.Count > 0 ? "WHERE " + string.Join(" AND ", condicoes) : string.Empty;
            var coluna = OrdenacaoHelper.ResolverColuna(filtro.OrdenarPor, ColunasOrdenacao, "nome");
            var direcao = OrdenacaoHelper.ResolverDirecao(filtro.Descendente);

            parametros.Add("Offset", filtro.Offset);
            parametros.Add("TamanhoPagina", filtro.TamanhoPagina);

            var sql = $@"
                SELECT COUNT(*) FROM usuarios {where};

                SELECT id, nome, email, perfil, ativo, created_at
                FROM usuarios
                {where}
                ORDER BY {coluna} {direcao}
                LIMIT @TamanhoPagina OFFSET @Offset;";

            using var multi = await connection.QueryMultipleAsync(sql, parametros);
            var total = await multi.ReadSingleAsync<long>();
            var usuarios = await multi.ReadAsync<UsuarioModel>();

            return new ResultadoPaginado<UsuarioRespostaDto>(
                usuarios.Select(MapearParaResposta), total, filtro.Pagina, filtro.TamanhoPagina);
        }

        /// <summary>
        /// Obtém um usuário pelo seu identificador.
        /// </summary>
        /// <param name="id">ID do usuário.</param>
        /// <returns>Dados do usuário ou null se não encontrado.</returns>
        public async Task<UsuarioRespostaDto?> ObterPorIdAsync(int id)
        {
            using var connection = _dbFactory.CreateConnection();

            const string sql = "SELECT id, nome, email, perfil, ativo, created_at FROM usuarios WHERE id = @Id";
            var usuario = await connection.QueryFirstOrDefaultAsync<UsuarioModel>(sql, new { Id = id });

            return usuario is null ? null : MapearParaResposta(usuario);
        }

        /// <summary>
        /// Atualiza os dados de um usuário existente.
        /// </summary>
        /// <param name="id">ID do usuário a ser atualizado.</param>
        /// <param name="dto">Novos dados do usuário.</param>
        /// <returns>Dados atualizados do usuário ou null se não encontrado.</returns>
        public async Task<UsuarioRespostaDto?> AtualizarAsync(int id, UsuarioUpdateDto dto)
        {
            using var connection = _dbFactory.CreateConnection();

            // Verificar se o usuário existe (guarda os dados atuais para a auditoria)
            var usuarioAntes = await connection.QueryFirstOrDefaultAsync<UsuarioModel>(
                "SELECT id, nome, email, perfil, ativo FROM usuarios WHERE id = @Id",
                new { Id = id });

            if (usuarioAntes is null)
                return null;

            // Verificar se o novo email já está em uso por outro usuário
            var emailEmUso = await connection.QueryFirstOrDefaultAsync<int>(
                "SELECT COUNT(1) FROM usuarios WHERE email = @Email AND id != @Id",
                new { dto.Email, Id = id });

            if (emailEmUso > 0)
                throw new RegraDeNegocioException("Este email já está em uso por outro usuário.");

            const string sql = @"
                UPDATE usuarios
                SET nome = @Nome, email = @Email, perfil = @Perfil, ativo = @Ativo, updated_at = NOW()
                WHERE id = @Id
                RETURNING id, nome, email, perfil, ativo, created_at";

            var usuario = await connection.QueryFirstAsync<UsuarioModel>(sql, new
            {
                Id = id,
                dto.Nome,
                dto.Email,
                Perfil = dto.Perfil!.Value.ToString(),
                dto.Ativo
            });

            var detalhes = MontarDetalhesAlteracao(usuarioAntes, usuario);
            if (detalhes is not null)
                await _auditoria.RegistrarSeguroAsync(
                    CategoriaAuditoria.Usuario, AcoesAuditoria.FuncionarioAtualizado, usuario.Id, detalhes);

            return MapearParaResposta(usuario);
        }

        /// <summary>
        /// Descreve o que mudou entre o estado anterior e o novo do usuário
        /// (para a auditoria). Retorna null se nada relevante mudou.
        /// </summary>
        private static string? MontarDetalhesAlteracao(UsuarioModel antes, UsuarioModel depois)
        {
            var mudancas = new List<string>();

            if (antes.Nome != depois.Nome)
                mudancas.Add($"nome: '{antes.Nome}' → '{depois.Nome}'");
            if (antes.Email != depois.Email)
                mudancas.Add($"email: '{antes.Email}' → '{depois.Email}'");
            if (antes.Perfil != depois.Perfil)
                mudancas.Add($"perfil: {antes.Perfil} → {depois.Perfil}");
            if (antes.Ativo != depois.Ativo)
                mudancas.Add(depois.Ativo ? "conta reativada" : "conta desativada");

            return mudancas.Count > 0 ? string.Join("; ", mudancas) : null;
        }

        /// <summary>
        /// Atualiza os dados do próprio usuário autenticado (autoatendimento).
        /// Só altera nome e email — perfil e situação (ativo) são exclusivos da
        /// atualização administrativa (<see cref="AtualizarAsync"/>).
        /// </summary>
        /// <param name="id">ID do usuário (o próprio usuário autenticado).</param>
        /// <param name="dto">Novos dados pessoais.</param>
        /// <returns>Dados atualizados do usuário ou null se não encontrado.</returns>
        public async Task<UsuarioRespostaDto?> AtualizarMeAsync(int id, UsuarioMeUpdateDto dto)
        {
            using var connection = _dbFactory.CreateConnection();

            // Verificar se o novo email já está em uso por outro usuário
            var emailEmUso = await connection.QueryFirstOrDefaultAsync<int>(
                "SELECT COUNT(1) FROM usuarios WHERE email = @Email AND id != @Id",
                new { dto.Email, Id = id });

            if (emailEmUso > 0)
                throw new RegraDeNegocioException("Este email já está em uso por outro usuário.");

            const string sql = @"
                UPDATE usuarios
                SET nome = @Nome, email = @Email, updated_at = NOW()
                WHERE id = @Id
                RETURNING id, nome, email, perfil, ativo, created_at";

            var usuario = await connection.QueryFirstOrDefaultAsync<UsuarioModel>(sql, new
            {
                Id = id,
                dto.Nome,
                dto.Email
            });

            return usuario is null ? null : MapearParaResposta(usuario);
        }

        /// <summary>
        /// Altera a senha do próprio usuário, exigindo a senha atual como confirmação.
        /// </summary>
        /// <param name="id">ID do usuário (o próprio usuário autenticado).</param>
        /// <param name="dto">Senha atual e nova senha.</param>
        /// <returns>True se a senha foi alterada, false se o usuário não foi encontrado.</returns>
        public async Task<bool> AlterarSenhaAsync(int id, UsuarioSenhaDto dto)
        {
            using var connection = _dbFactory.CreateConnection();

            var senhaHashAtual = await connection.QueryFirstOrDefaultAsync<string>(
                "SELECT senha_hash FROM usuarios WHERE id = @Id", new { Id = id });

            if (senhaHashAtual is null)
                return false;

            if (!BCrypt.Net.BCrypt.Verify(dto.SenhaAtual, senhaHashAtual))
                throw new RegraDeNegocioException("Senha atual incorreta.");

            var novoHash = BCrypt.Net.BCrypt.HashPassword(dto.NovaSenha);
            await connection.ExecuteAsync(
                "UPDATE usuarios SET senha_hash = @Hash, updated_at = NOW() WHERE id = @Id",
                new { Hash = novoHash, Id = id });

            await _auditoria.RegistrarSeguroAsync(
                CategoriaAuditoria.Usuario, AcoesAuditoria.SenhaAlterada, id);

            return true;
        }

        /// <summary>
        /// Exclui um usuário do sistema.
        /// </summary>
        /// <param name="id">ID do usuário a ser excluído.</param>
        /// <returns>True se o usuário foi excluído, false se não encontrado.</returns>
        public async Task<bool> ExcluirAsync(int id)
        {
            using var connection = _dbFactory.CreateConnection();

            // RETURNING preserva nome/email para a auditoria (snapshot pós-exclusão)
            const string sql = "DELETE FROM usuarios WHERE id = @Id RETURNING nome, email";
            var excluido = await connection.QueryFirstOrDefaultAsync<UsuarioModel>(sql, new { Id = id });

            if (excluido is null)
                return false;

            await _auditoria.RegistrarSeguroAsync(
                CategoriaAuditoria.Usuario, AcoesAuditoria.FuncionarioExcluido, id,
                $"Nome: {excluido.Nome}; email: {excluido.Email}");

            return true;
        }

        /// <summary>
        /// Mapeia um UsuarioModel para UsuarioRespostaDto.
        /// </summary>
        private static UsuarioRespostaDto MapearParaResposta(UsuarioModel usuario)
        {
            return new UsuarioRespostaDto
            {
                Id = usuario.Id,
                Nome = usuario.Nome,
                Email = usuario.Email,
                Perfil = usuario.Perfil,
                Ativo = usuario.Ativo,
                CreatedAt = usuario.CreatedAt
            };
        }
    }
}
