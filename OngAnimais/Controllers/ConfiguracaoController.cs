using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OngAnimais.Common;
using OngAnimais.DTOs;
using OngAnimais.Enums;
using OngAnimais.Modulos;

namespace OngAnimais.Controllers
{
    /// <summary>
    /// Controller das configurações da ONG (painel de customização).
    /// Exclusivo de Admin por padrão — só a leitura pública (<c>GET /publica</c>), usada pelo
    /// site para montar as páginas, é aberta. Além do <c>[Authorize]</c>, o módulo revalida no
    /// banco que o operador ainda é um Admin ativo (o perfil do JWT pode estar desatualizado).
    /// </summary>
    [ApiController]
    [Route("api/configuracao")]
    [Authorize(Roles = nameof(PerfilUsuario.Admin))]
    public class ConfiguracaoController : ControllerBase
    {
        private readonly ConfiguracaoModulo _modulo;

        /// <summary>
        /// Inicializa o controller com o módulo de configurações.
        /// </summary>
        /// <param name="modulo">Módulo de regra de negócio das configurações da ONG.</param>
        public ConfiguracaoController(ConfiguracaoModulo modulo)
        {
            _modulo = modulo;
        }

        /// <summary>
        /// Obtém as configurações que o site precisa para montar as páginas (identidade,
        /// contato, conteúdo da home, espécies atendidas, formulário de adoção e módulos).
        /// Público — não traz as regras internas de adoção.
        /// </summary>
        /// <returns>Configurações públicas da ONG.</returns>
        /// <response code="200">Configurações retornadas com sucesso.</response>
        /// <response code="404">Configuração da ONG não encontrada (script 011 não executado).</response>
        [HttpGet("publica")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<ConfiguracaoOngPublicaDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ObterPublica()
        {
            var configuracao = await _modulo.ObterPublicaAsync();
            if (configuracao is null)
                return NotFound(ApiResponse<ConfiguracaoOngPublicaDto>.Erro("Configuração da ONG não encontrada."));

            return Ok(ApiResponse<ConfiguracaoOngPublicaDto>.Ok(configuracao, "Configurações obtidas com sucesso."));
        }

        /// <summary>
        /// Obtém a configuração completa da ONG para a tela de edição do painel.
        /// </summary>
        /// <returns>Configuração completa (inclui regras de adoção e formas de ajudar inativas).</returns>
        /// <response code="200">Configuração retornada com sucesso.</response>
        /// <response code="404">Usuário não é Admin (o 403 é respondido como 404 para não revelar o recurso) ou configuração não encontrada.</response>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<ConfiguracaoOngRespostaDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Obter()
        {
            var configuracao = await _modulo.ObterAsync();
            if (configuracao is null)
                return NotFound(ApiResponse<ConfiguracaoOngRespostaDto>.Erro("Configuração da ONG não encontrada."));

            return Ok(ApiResponse<ConfiguracaoOngRespostaDto>.Ok(configuracao, "Configuração obtida com sucesso."));
        }

        /// <summary>
        /// Atualiza a configuração inteira da ONG. A alteração fica registrada na auditoria
        /// (categoria Configuracao) com os campos que mudaram.
        /// </summary>
        /// <param name="dto">Nova configuração completa.</param>
        /// <returns>Configuração atualizada.</returns>
        /// <response code="200">Configuração atualizada com sucesso.</response>
        /// <response code="400">Dados inválidos.</response>
        /// <response code="404">Usuário não é Admin (o 403 é respondido como 404 para não revelar o recurso) ou configuração não encontrada.</response>
        [HttpPut]
        [ProducesResponseType(typeof(ApiResponse<ConfiguracaoOngRespostaDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Atualizar([FromBody] ConfiguracaoOngUpdateDto dto)
        {
            var configuracao = await _modulo.AtualizarAsync(dto);
            if (configuracao is null)
                return NotFound(ApiResponse<ConfiguracaoOngRespostaDto>.Erro("Configuração da ONG não encontrada."));

            return Ok(ApiResponse<ConfiguracaoOngRespostaDto>.Ok(configuracao, "Configurações atualizadas com sucesso."));
        }

        /// <summary>
        /// Envia (ou troca) o logo da ONG. A imagem vira o logo do site e, a partir dela, o
        /// backend gera o favicon da aba do navegador e o ícone de atalho do celular. Salvo na
        /// hora — não depende do <c>PUT</c> da configuração. Fica registrado na auditoria.
        /// </summary>
        /// <param name="dto">Imagem do logo (multipart/form-data).</param>
        /// <returns>Configuração atualizada (com as URLs do logo e dos ícones).</returns>
        /// <response code="200">Logo salvo com sucesso.</response>
        /// <response code="400">Arquivo inválido (tipo, tamanho ou resolução).</response>
        /// <response code="404">Usuário não é Admin (o 403 é respondido como 404 para não revelar o recurso) ou configuração não encontrada.</response>
        [HttpPost("logo")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponse<ConfiguracaoOngRespostaDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AtualizarLogo([FromForm] ConfiguracaoLogoUploadDto dto)
        {
            var configuracao = await _modulo.AtualizarLogoAsync(dto.Arquivo!);
            if (configuracao is null)
                return NotFound(ApiResponse<ConfiguracaoOngRespostaDto>.Erro("Configuração da ONG não encontrada."));

            return Ok(ApiResponse<ConfiguracaoOngRespostaDto>.Ok(configuracao, "Logo atualizado com sucesso."));
        }

        /// <summary>
        /// Remove o logo da ONG — o site volta ao símbolo e ao favicon padrão. Fica registrado na auditoria.
        /// </summary>
        /// <returns>Configuração atualizada.</returns>
        /// <response code="200">Logo removido com sucesso.</response>
        /// <response code="404">Usuário não é Admin (o 403 é respondido como 404 para não revelar o recurso) ou configuração não encontrada.</response>
        [HttpDelete("logo")]
        [ProducesResponseType(typeof(ApiResponse<ConfiguracaoOngRespostaDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RemoverLogo()
        {
            var configuracao = await _modulo.RemoverLogoAsync();
            if (configuracao is null)
                return NotFound(ApiResponse<ConfiguracaoOngRespostaDto>.Erro("Configuração da ONG não encontrada."));

            return Ok(ApiResponse<ConfiguracaoOngRespostaDto>.Ok(configuracao, "Logo removido."));
        }
    }
}
