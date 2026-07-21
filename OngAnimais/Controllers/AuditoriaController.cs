using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OngAnimais.Common;
using OngAnimais.Common.Paginacao;
using OngAnimais.DTOs;
using OngAnimais.Enums;
using OngAnimais.Modulos;

namespace OngAnimais.Controllers
{
    /// <summary>
    /// Controller responsável pela consulta da trilha de auditoria.
    /// Somente leitura e exclusivo de Admin — os registros são imutáveis
    /// (não existem endpoints de criação, edição ou exclusão).
    /// </summary>
    [ApiController]
    [Route("api/auditoria")]
    [Authorize(Roles = nameof(PerfilUsuario.Admin))]
    public class AuditoriaController : ControllerBase
    {
        private readonly AuditoriaModulo _modulo;

        /// <summary>
        /// Inicializa o controller com o módulo de auditoria.
        /// </summary>
        /// <param name="modulo">Módulo de regra de negócio de auditoria.</param>
        public AuditoriaController(AuditoriaModulo modulo)
        {
            _modulo = modulo;
        }

        /// <summary>
        /// Lista os registros de auditoria de forma paginada, com filtros por
        /// categoria, funcionário, período e busca textual.
        /// </summary>
        /// <param name="filtro">Filtros, paginação e ordenação (via query string).</param>
        /// <returns>Página de registros de auditoria.</returns>
        /// <response code="200">Registros listados com sucesso.</response>
        /// <response code="403">Usuário autenticado não é Admin.</response>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<ResultadoPaginado<AuditoriaRespostaDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Listar([FromQuery] AuditoriaFiltroDto filtro)
        {
            var resultado = await _modulo.ListarPaginadoAsync(filtro);
            return Ok(ApiResponse<ResultadoPaginado<AuditoriaRespostaDto>>.Ok(resultado, "Auditoria listada com sucesso."));
        }
    }
}
