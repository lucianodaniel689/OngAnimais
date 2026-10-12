using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OngAnimais.Common;
using OngAnimais.Configurations;
using OngAnimais.Common.Paginacao;
using OngAnimais.DTOs;
using OngAnimais.Enums;
using OngAnimais.Modulos;

namespace OngAnimais.Controllers
{
    /// <summary>
    /// Controller responsável pelos endpoints de usuários (registro, login e CRUD).
    /// </summary>
    [ApiController]
    [Route("api/usuario")]
    public class UsuarioController : ControllerBase
    {
        private readonly UsuarioModulo _modulo;

        /// <summary>
        /// Inicializa o controller com o módulo de usuários.
        /// </summary>
        /// <param name="modulo">Módulo de regra de negócio de usuários.</param>
        public UsuarioController(UsuarioModulo modulo)
        {
            _modulo = modulo;
        }

        /// <summary>
        /// Registra um novo usuário (funcionário) no sistema.
        /// Exige um funcionário Admin logado — não há auto-cadastro público, já que
        /// o login é exclusivo da equipe da ONG (quem adota não tem conta).
        /// </summary>
        /// <param name="dto">Dados de registro do usuário.</param>
        /// <returns>Dados do usuário criado.</returns>
        /// <response code="201">Usuário registrado com sucesso.</response>
        /// <response code="400">Dados inválidos ou email já cadastrado.</response>
        /// <response code="404">Usuário autenticado não é Admin (o 403 é respondido como 404 para não revelar o recurso).</response>
        [HttpPost("registrar")]
        [Authorize(Roles = nameof(PerfilUsuario.Admin))]
        [ProducesResponseType(typeof(ApiResponse<UsuarioRespostaDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Registrar([FromBody] UsuarioRegistroDto dto)
        {
            var usuario = await _modulo.RegistrarAsync(dto);
            var response = ApiResponse<UsuarioRespostaDto>.Ok(usuario, "Usuário registrado com sucesso.");
            return CreatedAtAction(nameof(ObterPorId), new { id = usuario.Id }, response);
        }

        /// <summary>
        /// Realiza o login do usuário e retorna um token JWT.
        /// </summary>
        /// <param name="dto">Dados de login do usuário.</param>
        /// <returns>Token JWT para autenticação.</returns>
        /// <response code="200">Login realizado com sucesso.</response>
        /// <response code="401">Credenciais inválidas.</response>
        /// <response code="429">Muitas tentativas de login — aguarde antes de tentar de novo.</response>
        [HttpPost("login")]
        [AllowAnonymous]
        [EnableRateLimiting(RateLimitConfig.PoliticaLogin)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] UsuarioLoginDto dto)
        {
            var token = await _modulo.LoginAsync(dto);
            return Ok(ApiResponse<object>.Ok(new { Token = token }, "Login realizado com sucesso."));
        }

        /// <summary>
        /// Lista os usuários cadastrados de forma paginada, com busca e ordenação.
        /// </summary>
        /// <param name="filtro">Filtros, paginação e ordenação (via query string).</param>
        /// <returns>Página de usuários.</returns>
        /// <response code="200">Lista de usuários retornada com sucesso.</response>
        /// <response code="404">Usuário autenticado não é Admin (o 403 é respondido como 404 para não revelar o recurso).</response>
        [HttpGet]
        [Authorize(Roles = nameof(PerfilUsuario.Admin))]
        [ProducesResponseType(typeof(ApiResponse<ResultadoPaginado<UsuarioRespostaDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Listar([FromQuery] UsuarioFiltroDto filtro)
        {
            var resultado = await _modulo.ListarPaginadoAsync(filtro);
            return Ok(ApiResponse<ResultadoPaginado<UsuarioRespostaDto>>.Ok(resultado, "Usuários listados com sucesso."));
        }

        /// <summary>
        /// Obtém um usuário pelo seu identificador.
        /// </summary>
        /// <param name="id">ID do usuário.</param>
        /// <returns>Dados do usuário.</returns>
        /// <response code="200">Usuário encontrado.</response>
        /// <response code="404">Usuário não encontrado (ou usuário autenticado não é Admin — o 403 é respondido como 404).</response>
        [HttpGet("{id}")]
        [Authorize(Roles = nameof(PerfilUsuario.Admin))]
        [ProducesResponseType(typeof(ApiResponse<UsuarioRespostaDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ObterPorId(int id)
        {
            var usuario = await _modulo.ObterPorIdAsync(id);

            if (usuario is null)
                return NotFound(ApiResponse<object>.Erro("Usuário não encontrado."));

            return Ok(ApiResponse<UsuarioRespostaDto>.Ok(usuario, "Usuário encontrado com sucesso."));
        }

        /// <summary>
        /// Atualiza os dados de um usuário existente (uso administrativo:
        /// nome, email, perfil e situação).
        /// </summary>
        /// <param name="id">ID do usuário a ser atualizado.</param>
        /// <param name="dto">Novos dados do usuário.</param>
        /// <returns>Dados atualizados do usuário.</returns>
        /// <response code="200">Usuário atualizado com sucesso.</response>
        /// <response code="400">Email já em uso por outro usuário.</response>
        /// <response code="404">Usuário não encontrado (ou usuário autenticado não é Admin — o 403 é respondido como 404).</response>
        [HttpPut("{id}")]
        [Authorize(Roles = nameof(PerfilUsuario.Admin))]
        [ProducesResponseType(typeof(ApiResponse<UsuarioRespostaDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Atualizar(int id, [FromBody] UsuarioUpdateDto dto)
        {
            var usuario = await _modulo.AtualizarAsync(id, dto);

            if (usuario is null)
                return NotFound(ApiResponse<object>.Erro("Usuário não encontrado."));

            return Ok(ApiResponse<UsuarioRespostaDto>.Ok(usuario, "Usuário atualizado com sucesso."));
        }

        /// <summary>
        /// Obtém os dados do funcionário autenticado (autoatendimento, qualquer perfil).
        /// </summary>
        /// <returns>Dados do usuário logado.</returns>
        /// <response code="200">Usuário encontrado.</response>
        /// <response code="404">Usuário não encontrado.</response>
        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<UsuarioRespostaDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ObterMe()
        {
            var id = ObterIdUsuarioAutenticado();
            var usuario = await _modulo.ObterPorIdAsync(id);

            if (usuario is null)
                return NotFound(ApiResponse<object>.Erro("Usuário não encontrado."));

            return Ok(ApiResponse<UsuarioRespostaDto>.Ok(usuario, "Usuário encontrado com sucesso."));
        }

        /// <summary>
        /// Atualiza os dados pessoais (nome e email) do funcionário autenticado —
        /// autoatendimento, disponível a qualquer perfil. Não permite alterar
        /// perfil nem situação (isso é exclusivo do PUT administrativo por id).
        /// </summary>
        /// <param name="dto">Novos dados pessoais.</param>
        /// <returns>Dados atualizados do usuário.</returns>
        /// <response code="200">Dados atualizados com sucesso.</response>
        /// <response code="400">Email já em uso por outro usuário.</response>
        /// <response code="404">Usuário não encontrado.</response>
        [HttpPut("me")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<UsuarioRespostaDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AtualizarMe([FromBody] UsuarioMeUpdateDto dto)
        {
            var id = ObterIdUsuarioAutenticado();
            var usuario = await _modulo.AtualizarMeAsync(id, dto);

            if (usuario is null)
                return NotFound(ApiResponse<object>.Erro("Usuário não encontrado."));

            return Ok(ApiResponse<UsuarioRespostaDto>.Ok(usuario, "Dados atualizados com sucesso."));
        }

        /// <summary>
        /// Altera a senha do funcionário autenticado (o próprio, não de terceiros).
        /// Exige a senha atual como confirmação.
        /// </summary>
        /// <param name="dto">Senha atual e nova senha.</param>
        /// <returns>Confirmação da troca.</returns>
        /// <response code="200">Senha alterada com sucesso.</response>
        /// <response code="400">Senha atual incorreta ou dados inválidos.</response>
        [HttpPut("me/senha")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AlterarMinhaSenha([FromBody] UsuarioSenhaDto dto)
        {
            var id = ObterIdUsuarioAutenticado();
            var alterada = await _modulo.AlterarSenhaAsync(id, dto);

            if (!alterada)
                return NotFound(ApiResponse<object>.Erro("Usuário não encontrado."));

            return Ok(ApiResponse<object>.Ok(true, "Senha alterada com sucesso."));
        }

        /// <summary>
        /// Reseta a senha de um funcionário (uso administrativo — para quem esqueceu a
        /// própria senha e não consegue logar). Não exige a senha atual; a operação
        /// fica registrada na auditoria. Não vale para a própria conta do admin —
        /// essa troca passa por <c>PUT /api/usuario/me/senha</c>, que exige a senha atual.
        /// </summary>
        /// <param name="id">ID do funcionário que terá a senha resetada.</param>
        /// <param name="dto">Nova senha.</param>
        /// <returns>Confirmação do reset.</returns>
        /// <response code="200">Senha resetada com sucesso.</response>
        /// <response code="400">Dados inválidos ou tentativa de resetar a própria senha.</response>
        /// <response code="404">Usuário não encontrado (ou usuário autenticado não é Admin — o 403 é respondido como 404).</response>
        [HttpPut("{id}/senha")]
        [Authorize(Roles = nameof(PerfilUsuario.Admin))]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ResetarSenha(int id, [FromBody] UsuarioResetSenhaDto dto)
        {
            var resetada = await _modulo.ResetarSenhaAsync(id, ObterIdUsuarioAutenticado(), dto);

            if (!resetada)
                return NotFound(ApiResponse<object>.Erro("Usuário não encontrado."));

            return Ok(ApiResponse<object>.Ok(true, "Senha resetada com sucesso."));
        }

        /// <summary>
        /// Exclui um usuário do sistema.
        /// </summary>
        /// <param name="id">ID do usuário a ser excluído.</param>
        /// <returns>Confirmação da exclusão.</returns>
        /// <response code="200">Usuário excluído com sucesso.</response>
        /// <response code="404">Usuário não encontrado (ou usuário autenticado não é Admin — o 403 é respondido como 404).</response>
        [HttpDelete("{id}")]
        [Authorize(Roles = nameof(PerfilUsuario.Admin))]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Excluir(int id)
        {
            var excluido = await _modulo.ExcluirAsync(id);

            if (!excluido)
                return NotFound(ApiResponse<object>.Erro("Usuário não encontrado."));

            return Ok(ApiResponse<object>.Ok(true, "Usuário excluído com sucesso."));
        }

        /// <summary>
        /// Extrai o ID do usuário autenticado a partir das claims do token JWT (claim "sub").
        /// </summary>
        private int ObterIdUsuarioAutenticado()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            return int.Parse(idClaim!);
        }
    }
}
