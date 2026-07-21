import { inject, Signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { BreakpointObserver } from '@angular/cdk/layout';
import { map } from 'rxjs';

/** Breakpoint compartilhado do app: telas até 768px são tratadas como "celular". */
export const BREAKPOINT_MOBILE = '(max-width: 768px)';

/**
 * Signal reativo que indica se a tela atual é pequena (celular/tablet em pé).
 * Deve ser chamado em contexto de injeção (inicializador de campo ou construtor).
 */
export function telaPequena(): Signal<boolean> {
  const bp = inject(BreakpointObserver);
  return toSignal(bp.observe(BREAKPOINT_MOBILE).pipe(map((r) => r.matches)), {
    initialValue: false,
  });
}
