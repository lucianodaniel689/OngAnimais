import { Router, RedirectCommand } from '@angular/router';

/** Rota da página 404 — também usada pelos guards para esconder áreas restritas. */
export const ROTA_NAO_ENCONTRADO = '/nao-encontrado';

/**
 * Resultado de guard que exibe a página "não encontrada" **sem trocar a URL** na barra
 * (`skipLocationChange`). Quem tenta abrir `/admin/...` sem acesso vê exatamente o mesmo
 * que veria numa URL inexistente — não dá para saber que a área restrita existe.
 */
export function paginaNaoEncontrada(router: Router): RedirectCommand {
  return new RedirectCommand(router.parseUrl(ROTA_NAO_ENCONTRADO), { skipLocationChange: true });
}
