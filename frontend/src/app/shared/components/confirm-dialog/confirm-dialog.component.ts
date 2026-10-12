import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

/** Linha de detalhe (ícone + texto) exibida em lista abaixo da mensagem do `ConfirmDialogComponent`. */
export interface ConfirmDialogItem {
  /** Nome do ícone do Material (ex: `vaccines`). */
  icone: string;
  texto: string;
}

/** Dados exibidos pelo `ConfirmDialogComponent`. */
export interface ConfirmDialogData {
  titulo: string;
  mensagem: string;
  /**
   * Lista opcional de detalhes do que a ação afeta (ex: registros vinculados que serão
   * apagados junto). Aparece entre a mensagem e o rodapé.
   */
  itens?: ConfirmDialogItem[];
  /** Frase final em destaque, depois da lista (ex: "Essa ação não pode ser desfeita."). */
  rodape?: string;
  /** Rótulo do botão de confirmação (padrão: "Confirmar"). */
  textoConfirmar?: string;
  /** Rótulo do botão de cancelamento (padrão: "Cancelar"). */
  textoCancelar?: string;
  /** Quando true, usa a cor de perigo no botão de confirmação (ex: exclusões). */
  perigoso?: boolean;
}

/**
 * Diálogo genérico de confirmação para ações irreversíveis (ex: exclusão de um
 * cadastro). Retorna `true`/`false` via `MatDialogRef.afterClosed()`.
 *
 * Com `itens`, mostra também a lista do que a ação afeta (ícone + texto) — usado na
 * exclusão de um animal que tem adoções, vacinas ou fotos vinculadas.
 *
 * Uso:
 * ```ts
 * const ref = this.dialog.open(ConfirmDialogComponent, {
 *   data: { titulo: 'Excluir animal', mensagem: '...', perigoso: true } satisfies ConfirmDialogData,
 * });
 * ref.afterClosed().subscribe((confirmado) => { ... });
 * ```
 */
@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatDialogModule, MatButtonModule, MatIconModule],
  template: `
    <h2 mat-dialog-title>{{ data.titulo }}</h2>
    <mat-dialog-content>
      <p class="confirm-dialog__mensagem">{{ data.mensagem }}</p>
      @if (data.itens?.length) {
        <ul class="confirm-dialog__itens">
          @for (item of data.itens; track item.texto) {
            <li>
              <mat-icon aria-hidden="true">{{ item.icone }}</mat-icon>
              <span>{{ item.texto }}</span>
            </li>
          }
        </ul>
      }
      @if (data.rodape) {
        <p class="confirm-dialog__rodape">{{ data.rodape }}</p>
      }
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button (click)="dialogRef.close(false)">
        {{ data.textoCancelar ?? 'Cancelar' }}
      </button>
      <button
        mat-flat-button
        [color]="data.perigoso ? 'warn' : 'primary'"
        (click)="dialogRef.close(true)"
      >
        {{ data.textoConfirmar ?? 'Confirmar' }}
      </button>
    </mat-dialog-actions>
  `,
  styles: `
    .confirm-dialog__mensagem,
    .confirm-dialog__rodape {
      margin: 0;
    }

    .confirm-dialog__itens {
      list-style: none;
      margin: var(--esp-3) 0;
      padding: var(--esp-3);
      display: flex;
      flex-direction: column;
      gap: var(--esp-2);
      background: var(--cor-perigo-bg);
      border-radius: var(--raio-md);
    }

    .confirm-dialog__itens li {
      display: flex;
      align-items: center;
      gap: var(--esp-2);
    }

    .confirm-dialog__itens mat-icon {
      color: var(--cor-perigo);
      flex-shrink: 0;
    }

    .confirm-dialog__rodape {
      font-weight: var(--peso-medio);
    }
  `,
})
export class ConfirmDialogComponent {
  readonly dialogRef = inject(MatDialogRef<ConfirmDialogComponent>);
  readonly data = inject<ConfirmDialogData>(MAT_DIALOG_DATA);
}
