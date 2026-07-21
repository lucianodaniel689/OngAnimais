using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OngAnimais.Common;
using OngAnimais.Common.Paginacao;
using OngAnimais.Configurations;
using OngAnimais.DTOs;
using OngAnimais.Modulos;

namespace OngAnimais.Controllers
{
    /// <summary>
    /// Controller responsável pelos endpoints de adoções.
    /// </summary>
    [ApiController]
    [Route("api/adocao")]
    [Authorize]
    public class AdocaoController : ControllerBase
    {
        private readonly AdocaoModulo _modulo;

        /// <summary>
        /// Inicializa o controller com o módulo de adoções.
        /// </summary>
        /// <param name="modulo">Módulo de regra de negócio de adoções.</param>
        public AdocaoController(AdocaoModulo modulo)
        {
            _modulo = modulo;
        }

        /// <summary>
        /// Abre uma candidatura de adoção para um animal.
        /// O animal deve estar com status 'Disponivel' e continua disponível para outras
        /// candidaturas — a reserva só acontece quando a ONG aprova uma delas.
        /// Endpoint público — o adotante não tem conta no sistema (login é exclusivo dos
        /// funcionários).
        /// </summary>
        /// <param name="dto">Dados da solicitação e do adotante.</param>
        /// <returns>Dados da adoção criada.</returns>
        /// <response code="201">Solicitação aberta com sucesso.</response>
        /// <response code="400">Animal indisponível ou dados inválidos.</response>
        /// <response code="429">Muitas solicitações seguidas — aguarde antes de tentar de novo.</response>
        [HttpPost]
        [AllowAnonymous]
        [EnableRateLimiting(RateLimitConfig.PoliticaEscritaPublica)]
        [ProducesResponseType(typeof(ApiResponse<AdocaoRespostaDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Criar([FromBody] AdocaoCreateDto dto)
        {
            var adocao = await _modulo.CriarAsync(dto);
            var response = ApiResponse<AdocaoRespostaDto>.Ok(adocao, "Solicitação de adoção aberta com sucesso.");
            return CreatedAtAction(nameof(ObterPorId), new { id = adocao.Id }, response);
        }

        /// <summary>
        /// Lista as solicitações de adoção de forma paginada, com filtros e ordenação.
        /// </summary>
        /// <param name="filtro">Filtros, paginação e ordenação (via query string).</param>
        /// <returns>Página de adoções com nome do animal e do solicitante.</returns>
        /// <response code="200">Lista retornada com sucesso.</response>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<ResultadoPaginado<AdocaoRespostaDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Listar([FromQuery] AdocaoFiltroDto filtro)
        {
            var resultado = await _modulo.ListarPaginadoAsync(filtro);
            return Ok(ApiResponse<ResultadoPaginado<AdocaoRespostaDto>>.Ok(resultado, "Adoções listadas com sucesso."));
        }

        /// <summary>
        /// Obtém uma adoção pelo seu identificador.
        /// </summary>
        /// <param name="id">ID da adoção.</param>
        /// <returns>Dados da adoção.</returns>
        /// <response code="200">Adoção encontrada.</response>
        /// <response code="404">Adoção não encontrada.</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<AdocaoRespostaDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ObterPorId(int id)
        {
            var adocao = await _modulo.ObterPorIdAsync(id);

            if (adocao is null)
                return NotFound(ApiResponse<object>.Erro("Adoção não encontrada."));

            return Ok(ApiResponse<AdocaoRespostaDto>.Ok(adocao, "Adoção encontrada com sucesso."));
        }

        /// <summary>
        /// Atualiza o status de uma adoção (aprovar, recusar, concluir ou cancelar).
        /// Aprovar reserva o animal; concluir marca o animal como 'Adotado' e recusa
        /// automaticamente as demais candidaturas pendentes; recusar/cancelar uma adoção
        /// aprovada (ou cancelar uma concluída — devolução) libera o animal.
        /// Transições fora da máquina de status retornam 400.
        /// </summary>
        /// <param name="id">ID da adoção.</param>
        /// <param name="dto">Novo status e observações.</param>
        /// <returns>Dados atualizados da adoção.</returns>
        /// <response code="200">Adoção atualizada com sucesso.</response>
        /// <response code="400">Dados inválidos ou transição de status não permitida.</response>
        /// <response code="404">Adoção não encontrada.</response>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponse<AdocaoRespostaDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Atualizar(int id, [FromBody] AdocaoUpdateDto dto)
        {
            var adocao = await _modulo.AtualizarAsync(id, dto);

            if (adocao is null)
                return NotFound(ApiResponse<object>.Erro("Adoção não encontrada."));

            return Ok(ApiResponse<AdocaoRespostaDto>.Ok(adocao, "Adoção atualizada com sucesso."));
        }
    }
}
