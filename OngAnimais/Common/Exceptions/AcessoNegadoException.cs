namespace OngAnimais.Common.Exceptions
{
    /// <summary>
    /// Exceção lançada quando o operador passou pelo <c>[Authorize]</c> (o JWT diz que ele tem
    /// o perfil exigido), mas a checagem no banco mostra que não tem mais — ex.: Admin rebaixado
    /// a Comum ou desativado enquanto o token ainda era válido (o JWT dura até 60 min).
    /// É tratada pelo middleware global e convertida em HTTP 404 "Recurso não encontrado." —
    /// mesma resposta do <c>OnForbidden</c> do JWT, para não confirmar que o recurso existe.
    /// A mensagem desta exceção vai só para o log, nunca para o cliente.
    /// </summary>
    public class AcessoNegadoException : Exception
    {
        /// <summary>
        /// Cria a exceção com o motivo da recusa (registrado apenas no log).
        /// </summary>
        /// <param name="motivo">Motivo técnico da recusa.</param>
        public AcessoNegadoException(string motivo) : base(motivo)
        {
        }
    }
}
