import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { AdocaoRespostaDto, StatusAdocao } from '@app/core/api';
import { ROTULO_STATUS_ADOCAO } from '@app/core/models';

/**
 * Transições de status permitidas (espelho da máquina de status do backend —
 * `AdocaoModulo.TransicoesPermitidas`). Fora disso o backend responde 400.
 * 'Recusada' e 'Cancelada' são finais; 'Concluida' só pode ser cancelada (devolução).
 */
const TRANSICOES_PERMITIDAS: Record<StatusAdocao, StatusAdocao[]> = {
  [StatusAdocao.Pendente]: [StatusAdocao.Aprovada, StatusAdocao.Recusada, StatusAdocao.Cancelada],
  [StatusAdocao.Aprovada]: [StatusAdocao.Concluida, StatusAdocao.Recusada, StatusAdocao.Cancelada],
  [StatusAdocao.Concluida]: [StatusAdocao.Cancelada],
  [StatusAdocao.Recusada]: [],
  [StatusAdocao.Cancelada]: [],
};

/** Dados de entrada do `AdocaoStatusDialogComponent`. */
export interface AdocaoStatusDialogData {
  adocao: AdocaoRespostaDto;
}

/** Resultado retornado quando o usuário confirma a mudança de status. */
export interface AdocaoStatusDialogResultado {
  status: StatusAdocao;
  observacoes?: string;
}

/**
 * Diálogo para alterar o status de uma solicitação de adoção (aprovar, recusar,
 * concluir ou cancelar) e registrar uma observação (ex.: motivo da recusa).
 * Só oferece as transições válidas a partir do status atual (manter o status
 * atual também é permitido — atualiza apenas as observações).
 * Uso: `dialog.open(AdocaoStatusDialogComponent, { data: { adocao } })`.
 */
@Component({
  selector: 'app-adocao-status-dialog',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
  ],
  templateUrl: './adocao-status-dialog.component.html',
  styleUrl: './adocao-status-dialog.component.scss',
})
export class AdocaoStatusDialogComponent {
  private fb = inject(FormBuilder);
  readonly dialogRef = inject(MatDialogRef<AdocaoStatusDialogComponent>);
  readonly data = inject<AdocaoStatusDialogData>(MAT_DIALOG_DATA);

  /** Status atual + transições permitidas a partir dele. */
  readonly statusOpcoes = [
    this.data.adocao.status ?? StatusAdocao.Pendente,
    ...TRANSICOES_PERMITIDAS[this.data.adocao.status ?? StatusAdocao.Pendente],
  ];
  readonly rotuloStatus = ROTULO_STATUS_ADOCAO;

  readonly form = this.fb.nonNullable.group({
    status: [this.data.adocao.status ?? StatusAdocao.Pendente, Validators.required],
    observacoes: [this.data.adocao.observacoes ?? ''],
  });

  confirmar(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const v = this.form.getRawValue();
    this.dialogRef.close({
      status: v.status,
      observacoes: v.observacoes || undefined,
    } satisfies AdocaoStatusDialogResultado);
  }
}
