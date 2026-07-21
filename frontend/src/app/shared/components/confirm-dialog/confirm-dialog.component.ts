import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';

/** Dados exibidos pelo `ConfirmDialogComponent`. */
export interface ConfirmDialogData {
  titulo: string;
  mensagem: string;
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
  imports: [MatDialogModule, MatButtonModule],
  template: `
    <h2 mat-dialog-title>{{ data.titulo }}</h2>
    <mat-dialog-content>{{ data.mensagem }}</mat-dialog-content>
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
})
export class ConfirmDialogComponent {
  readonly dialogRef = inject(MatDialogRef<ConfirmDialogComponent>);
  readonly data = inject<ConfirmDialogData>(MAT_DIALOG_DATA);
}
