using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OngAnimais.Common;
using OngAnimais.Common.Paginacao;
using OngAnimais.DTOs;
using OngAnimais.Modulos;

namespace OngAnimais.Controllers
{
    /// <summary>
    /// Controller responsável pelos endpoints de fotos (galeria por animal).
    /// </summary>
    [ApiController]
    [Route("api/foto")]
    [Authorize]
    public class FotoController : ControllerBase
    {
        private readonly FotoModulo _modulo;

        /// <summary>
        /// Inicializa o controller com o módulo de fotos.
        /// </summary>
        /// <param name="modulo">Módulo de regra de negócio de fotos.</param>
        public FotoController(FotoModulo modulo)
        {
            _modulo = modulo;
        }

        /// <summary>
        /// Faz upload de uma foto e adiciona à galeria de um animal.
        /// </summary>
        /// <param name="dto">Arquivo de imagem e dados da foto (multipart/form-data).</param>
        /// <returns>Dados da foto criada.</returns>
        /// <response code="201">Foto adicionada com sucesso.</response>
        /// <response code="400">Animal inexistente/inativo, arquivo inválido ou dados inválidos.</response>
        [HttpPost]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponse<FotoRespostaDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Criar([FromForm] FotoUploadDto dto)
        {
            var foto = await _modulo.CriarAsync(dto);
            var response = ApiResponse<FotoRespostaDto>.Ok(foto, "Foto adicionada com sucesso.");
            return CreatedAtAction(nameof(ObterPorId), new { id = foto.Id }, response);
        }

        /// <summary>
        /// Lista as fotos cadastradas de forma paginada, com filtros e ordenação.
        /// </summary>
        /// <param name="filtro">Filtros, paginação e ordenação (via query string).</param>
        /// <returns>Página de fotos.</returns>
        /// <response code="200">Lista retornada com sucesso.</response>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<ResultadoPaginado<FotoRespostaDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Listar([FromQuery] FotoFiltroDto filtro)
        {
            var resultado = await _modulo.ListarPaginadoAsync(filtro);
            return Ok(ApiResponse<ResultadoPaginado<FotoRespostaDto>>.Ok(resultado, "Fotos listadas com sucesso."));
        }

        /// <summary>
        /// Lista a galeria de fotos de um animal específico.
        /// </summary>
        /// <param name="animalId">ID do animal.</param>
        /// <returns>Lista de fotos do animal (a principal primeiro).</returns>
        /// <response code="200">Galeria retornada com sucesso.</response>
        [HttpGet("animal/{animalId}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<FotoRespostaDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ListarPorAnimal(int animalId)
        {
            var fotos = await _modulo.ListarPorAnimalAsync(animalId);
            return Ok(ApiResponse<IEnumerable<FotoRespostaDto>>.Ok(fotos, "Galeria de fotos do animal."));
        }

        /// <summary>
        /// Obtém uma foto pelo seu identificador.
        /// </summary>
        /// <param name="id">ID da foto.</param>
        /// <returns>Dados da foto.</returns>
        /// <response code="200">Foto encontrada.</response>
        /// <response code="404">Foto não encontrada.</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<FotoRespostaDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ObterPorId(int id)
        {
            var foto = await _modulo.ObterPorIdAsync(id);

            if (foto is null)
                return NotFound(ApiResponse<object>.Erro("Foto não encontrada."));

            return Ok(ApiResponse<FotoRespostaDto>.Ok(foto, "Foto encontrada com sucesso."));
        }

        /// <summary>
        /// Atualiza a descrição e o indicador de principal de uma foto.
        /// </summary>
        /// <param name="id">ID da foto a ser atualizada.</param>
        /// <param name="dto">Novos dados da foto.</param>
        /// <returns>Dados atualizados da foto.</returns>
        /// <response code="200">Foto atualizada com sucesso.</response>
        /// <response code="404">Foto não encontrada.</response>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponse<FotoRespostaDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Atualizar(int id, [FromBody] FotoUpdateDto dto)
        {
            var foto = await _modulo.AtualizarAsync(id, dto);

            if (foto is null)
                return NotFound(ApiResponse<object>.Erro("Foto não encontrada."));

            return Ok(ApiResponse<FotoRespostaDto>.Ok(foto, "Foto atualizada com sucesso."));
        }

        /// <summary>
        /// Exclui uma foto da galeria.
        /// </summary>
        /// <param name="id">ID da foto a ser excluída.</param>
        /// <returns>Confirmação da exclusão.</returns>
        /// <response code="200">Foto excluída com sucesso.</response>
        /// <response code="404">Foto não encontrada.</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Excluir(int id)
        {
            var excluido = await _modulo.ExcluirAsync(id);

            if (!excluido)
                return NotFound(ApiResponse<object>.Erro("Foto não encontrada."));

            return Ok(ApiResponse<object>.Ok(true, "Foto excluída com sucesso."));
        }
    }
}
