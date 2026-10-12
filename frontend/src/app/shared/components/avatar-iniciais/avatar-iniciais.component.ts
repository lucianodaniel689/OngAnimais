import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

/**
 * Avatar circular com as iniciais do nome (até 2 letras: primeiro + último nome) — para
 * pessoas sem foto, como funcionários. `inativo` deixa o avatar acinzentado.
 * Uso: `<app-avatar-iniciais [nome]="usuario.nome" [inativo]="!usuario.ativo" />`
 */
@Component({
  selector: 'app-avatar-iniciais',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `{{ iniciais() }}`,
  host: {
    'aria-hidden': 'true',
    '[class.avatar--inativo]': 'inativo()',
    '[style.--avatar-tamanho.px]': 'tamanho()',
  },
  styles: `
    :host {
      display: inline-grid;
      flex-shrink: 0;
      place-items: center;
      width: var(--avatar-tamanho);
      height: var(--avatar-tamanho);
      border-radius: var(--raio-full);
      background-color: var(--cor-primaria-clara);
      color: var(--cor-primaria-escura);
      font-size: calc(var(--avatar-tamanho) * 0.36);
      font-weight: var(--peso-bold);
      line-height: 1;
      user-select: none;
    }

    :host(.avatar--inativo) {
      background-color: var(--cor-superficie-alt);
      color: var(--cor-texto-suave);
    }
  `,
})
export class AvatarIniciaisComponent {
  /** Nome completo de onde saem as iniciais. */
  readonly nome = input<string | null | undefined>('');
  /** Diâmetro em px (padrão 56). */
  readonly tamanho = input(56);
  /** Conta inativa/desabilitada → tom neutro. */
  readonly inativo = input(false);

  readonly iniciais = computed(() => {
    const partes = (this.nome() ?? '').trim().split(/\s+/).filter(Boolean);
    if (partes.length === 0) {
      return '?';
    }
    const ultima = partes.length > 1 ? partes[partes.length - 1][0] : '';
    return (partes[0][0] + ultima).toUpperCase();
  });
}
