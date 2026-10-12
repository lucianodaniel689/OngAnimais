using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OngAnimais.Common;
using OngAnimais.Common.Paginacao;
using OngAnimais.DTOs;
using OngAnimais.Modulos;

namespace OngAnimais.Controllers
{
    /// <summary>
    /// Controller responsável pelos endpoints de animais (CRUD).
    /// </summary>
    [ApiController]
    [Route("api/animal")]
    [Authorize]
    public class AnimalController : ControllerBase
    {
        private readonly AnimalModulo _modulo;

        /// <summary>
        /// Inicializa o controller com o módulo de animais.
        /// </summary>
        /// <param name="modulo">Módulo de regra de negócio de animais.</param>
        public AnimalController(AnimalModulo modulo)
        {
            _modulo = modulo;
        }

        /// <summary>
        /// Cadastra um novo animal no sistema.
        /// </summary>
        /// <param name="dto">Dados do animal.</param>
        /// <returns>Dados do animal criado.</returns>
        /// <response code="201">Animal cadastrado com sucesso.</response>
        /// <response code="400">Dados inválidos.</response>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<AnimalRespostaDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Criar([FromBody] AnimalCreateDto dto)
        {
            var animal = await _modulo.CriarAsync(dto);
            var response = ApiResponse<AnimalRespostaDto>.Ok(animal, "Animal cadastrado com sucesso.");
            return CreatedAtAction(nameof(ObterPorId), new { id = animal.Id }, response);
        }

        /// <summary>
        /// Lista todos os animais disponíveis para adoção.
        /// </summary>
        /// <remarks>Endpoint público para exibição na página inicial de adoção.</remarks>
        /// <returns>Lista de animais disponíveis.</returns>
        /// <response code="200">Lista retornada com sucesso.</response>
        [HttpGet("disponiveis")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<AnimalRespostaDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ListarDisponiveis()
        {
            var animais = await _modulo.ListarDisponiveisAsync();
            return Ok(ApiResponse<IEnumerable<AnimalRespostaDto>>.Ok(animais, "Animais disponíveis para adoção."));
        }

        /// <summary>
        /// Lista os animais cadastrados de forma paginada, com filtros e ordenação.
        /// </summary>
        /// <param name="filtro">Filtros, paginação e ordenação (via query string).</param>
        /// <returns>Página de animais.</returns>
        /// <response code="200">Lista de animais retornada com sucesso.</response>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<ResultadoPaginado<AnimalRespostaDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Listar([FromQuery] AnimalFiltroDto filtro)
        {
            var resultado = await _modulo.ListarPaginadoAsync(filtro);
            return Ok(ApiResponse<ResultadoPaginado<AnimalRespostaDto>>.Ok(resultado, "Animais listados com sucesso."));
        }

        /// <summary>
        /// Obtém um animal pelo seu identificador.
        /// </summary>
        /// <param name="id">ID do animal.</param>
        /// <returns>Dados do animal.</returns>
        /// <response code="200">Animal encontrado.</response>
        /// <response code="404">Animal não encontrado.</response>
        [HttpGet("{id}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<AnimalRespostaDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ObterPorId(int id)
        {
            var animal = await _modulo.ObterPorIdAsync(id);

            if (animal is null)
                return NotFound(ApiResponse<object>.Erro("Animal não encontrado."));

            return Ok(ApiResponse<AnimalRespostaDto>.Ok(animal, "Animal encontrado com sucesso."));
        }

        /// <summary>
        /// Atualiza os dados de um animal existente.
        /// </summary>
        /// <param name="id">ID do animal a ser atualizado.</param>
        /// <param name="dto">Novos dados do animal.</param>
        /// <returns>Dados atualizados do animal.</returns>
        /// <response code="200">Animal atualizado com sucesso.</response>
        /// <response code="404">Animal não encontrado.</response>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponse<AnimalRespostaDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Atualizar(int id, [FromBody] AnimalUpdateDto dto)
        {
            var animal = await _modulo.AtualizarAsync(id, dto);

            if (animal is null)
                return NotFound(ApiResponse<object>.Erro("Animal não encontrado."));

            return Ok(ApiResponse<AnimalRespostaDto>.Ok(animal, "Animal atualizado com sucesso."));
        }

        /// <summary>
        /// Lista o que está vinculado a um animal (adoções, vacinas e fotos) — ou seja, o que
        /// também seria apagado ao excluí-lo com confirmação.
        /// </summary>
        /// <param name="id">ID do animal.</param>
        /// <returns>Contagem dos registros vinculados.</returns>
        /// <response code="200">Vínculos retornados com sucesso.</response>
        /// <response code="404">Animal não encontrado.</response>
        [HttpGet("{id}/vinculos")]
        [ProducesResponseType(typeof(ApiResponse<AnimalVinculosDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ObterVinculos(int id)
        {
            var vinculos = await _modulo.ObterVinculosAsync(id);

            if (vinculos is null)
                return NotFound(ApiResponse<object>.Erro("Animal não encontrado."));

            return Ok(ApiResponse<AnimalVinculosDto>.Ok(vinculos, "Vínculos obtidos com sucesso."));
        }

        /// <summary>
        /// Exclui um animal do sistema. Com adoções, vacinas ou fotos vinculadas, só exclui se
        /// <c>excluirVinculos=true</c> (confirmação explícita) — nesse caso apaga também esses registros.
        /// </summary>
        /// <param name="id">ID do animal a ser excluído.</param>
        /// <param name="excluirVinculos">Confirma a exclusão dos registros vinculados (adoções, vacinas e fotos).</param>
        /// <returns>Confirmação da exclusão.</returns>
        /// <response code="200">Animal excluído com sucesso.</response>
        /// <response code="400">Há registros vinculados e a exclusão deles não foi confirmada.</response>
        /// <response code="404">Animal não encontrado.</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Excluir(int id, [FromQuery] bool excluirVinculos = false)
        {
            var excluido = await _modulo.ExcluirAsync(id, excluirVinculos);

            if (!excluido)
                return NotFound(ApiResponse<object>.Erro("Animal não encontrado."));

            return Ok(ApiResponse<object>.Ok(true, "Animal excluído com sucesso."));
        }
    }
}
