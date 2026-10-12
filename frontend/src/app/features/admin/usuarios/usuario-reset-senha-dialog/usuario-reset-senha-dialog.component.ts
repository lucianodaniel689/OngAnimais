import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { UsuarioRespostaDto } from '@app/core/api';

/** Dados de entrada do `UsuarioResetSenhaDialogComponent`. */
export interface UsuarioResetSenhaDialogData {
  usuario: UsuarioRespostaDto;
}

/** Resultado retornado quando o Admin confirma o reset. */
export interface UsuarioResetSenhaDialogResultado {
  novaSenha: string;
}

/**
 * Diálogo para um Admin definir uma nova senha para outro funcionário
 * (cenário de senha esquecida — não exige a senha atual; o backend registra
 * a operação na auditoria). Quem chama faz a requisição com o resultado.
 * Uso: `dialog.open(UsuarioResetSenhaDialogComponent, { data: { usuario } })`.
 */
@Component({
  selector: 'app-usuario-reset-senha-dialog',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
  ],
  templateUrl: './usuario-reset-senha-dialog.component.html',
  styleUrl: './usuario-reset-senha-dialog.component.scss',
})
export class UsuarioResetSenhaDialogComponent {
  private fb = inject(FormBuilder);
  readonly dialogRef = inject(MatDialogRef<UsuarioResetSenhaDialogComponent>);
  readonly data = inject<UsuarioResetSenhaDialogData>(MAT_DIALOG_DATA);

  readonly esconderSenha = signal(true);

  readonly form = this.fb.nonNullable.group(
    {
      novaSenha: ['', [Validators.required, Validators.minLength(6), Validators.maxLength(100)]],
      confirmarSenha: ['', [Validators.required]],
    },
    { validators: [senhasIguais] },
  );

  confirmar(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.dialogRef.close({
      novaSenha: this.form.getRawValue().novaSenha,
    } satisfies UsuarioResetSenhaDialogResultado);
  }
}

/** Validador de grupo: nova senha e confirmação precisam ser iguais. */
function senhasIguais(grupo: AbstractControl): { senhasDiferentes: true } | null {
  const nova = grupo.get('novaSenha')?.value;
  const confirmar = grupo.get('confirmarSenha')?.value;
  return nova && confirmar && nova !== confirmar ? { senhasDiferentes: true } : null;
}
