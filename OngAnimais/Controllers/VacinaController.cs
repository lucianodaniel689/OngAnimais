using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OngAnimais.Common;
using OngAnimais.Common.Paginacao;
using OngAnimais.DTOs;
using OngAnimais.Modulos;

namespace OngAnimais.Controllers
{
    /// <summary>
    /// Controller responsável pelos endpoints de vacinas (histórico por animal).
    /// </summary>
    [ApiController]
    [Route("api/vacina")]
    [Authorize]
    public class VacinaController : ControllerBase
    {
        private readonly VacinaModulo _modulo;

        /// <summary>
        /// Inicializa o controller com o módulo de vacinas.
        /// </summary>
        /// <param name="modulo">Módulo de regra de negócio de vacinas.</param>
        public VacinaController(VacinaModulo modulo)
        {
            _modulo = modulo;
        }

        /// <summary>
        /// Registra uma nova vacina aplicada em um animal.
        /// </summary>
        /// <param name="dto">Dados da vacina.</param>
        /// <returns>Dados da vacina registrada.</returns>
        /// <response code="201">Vacina registrada com sucesso.</response>
        /// <response code="400">Animal inexistente/inativo ou dados inválidos.</response>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<VacinaRespostaDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Criar([FromBody] VacinaCreateDto dto)
        {
            var vacina = await _modulo.CriarAsync(dto);
            var response = ApiResponse<VacinaRespostaDto>.Ok(vacina, "Vacina registrada com sucesso.");
            return CreatedAtAction(nameof(ObterPorId), new { id = vacina.Id }, response);
        }

        /// <summary>
        /// Lista as vacinas registradas de forma paginada, com filtros e ordenação.
        /// </summary>
        /// <param name="filtro">Filtros, paginação e ordenação (via query string).</param>
        /// <returns>Página de vacinas.</returns>
        /// <response code="200">Lista retornada com sucesso.</response>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<ResultadoPaginado<VacinaRespostaDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Listar([FromQuery] VacinaFiltroDto filtro)
        {
            var resultado = await _modulo.ListarPaginadoAsync(filtro);
            return Ok(ApiResponse<ResultadoPaginado<VacinaRespostaDto>>.Ok(resultado, "Vacinas listadas com sucesso."));
        }

        /// <summary>
        /// Lista o histórico de vacinas de um animal específico.
        /// </summary>
        /// <param name="animalId">ID do animal.</param>
        /// <returns>Lista de vacinas do animal.</returns>
        /// <response code="200">Histórico retornado com sucesso.</response>
        [HttpGet("animal/{animalId}")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<VacinaRespostaDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ListarPorAnimal(int animalId)
        {
            var vacinas = await _modulo.ListarPorAnimalAsync(animalId);
            return Ok(ApiResponse<IEnumerable<VacinaRespostaDto>>.Ok(vacinas, "Histórico de vacinas do animal."));
        }

        /// <summary>
        /// Obtém uma vacina pelo seu identificador.
        /// </summary>
        /// <param name="id">ID da vacina.</param>
        /// <returns>Dados da vacina.</returns>
        /// <response code="200">Vacina encontrada.</response>
        /// <response code="404">Vacina não encontrada.</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<VacinaRespostaDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ObterPorId(int id)
        {
            var vacina = await _modulo.ObterPorIdAsync(id);

            if (vacina is null)
                return NotFound(ApiResponse<object>.Erro("Vacina não encontrada."));

            return Ok(ApiResponse<VacinaRespostaDto>.Ok(vacina, "Vacina encontrada com sucesso."));
        }

        /// <summary>
        /// Atualiza os dados de uma vacina existente.
        /// </summary>
        /// <param name="id">ID da vacina a ser atualizada.</param>
        /// <param name="dto">Novos dados da vacina.</param>
        /// <returns>Dados atualizados da vacina.</returns>
        /// <response code="200">Vacina atualizada com sucesso.</response>
        /// <response code="404">Vacina não encontrada.</response>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponse<VacinaRespostaDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Atualizar(int id, [FromBody] VacinaUpdateDto dto)
        {
            var vacina = await _modulo.AtualizarAsync(id, dto);

            if (vacina is null)
                return NotFound(ApiResponse<object>.Erro("Vacina não encontrada."));

            return Ok(ApiResponse<VacinaRespostaDto>.Ok(vacina, "Vacina atualizada com sucesso."));
        }

        /// <summary>
        /// Exclui uma vacina do sistema.
        /// </summary>
        /// <param name="id">ID da vacina a ser excluída.</param>
        /// <returns>Confirmação da exclusão.</returns>
        /// <response code="200">Vacina excluída com sucesso.</response>
        /// <response code="404">Vacina não encontrada.</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Excluir(int id)
        {
            var excluido = await _modulo.ExcluirAsync(id);

            if (!excluido)
                return NotFound(ApiResponse<object>.Erro("Vacina não encontrada."));

            return Ok(ApiResponse<object>.Ok(true, "Vacina excluída com sucesso."));
        }
    }
}
