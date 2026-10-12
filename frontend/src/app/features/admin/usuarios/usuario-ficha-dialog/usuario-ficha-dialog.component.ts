import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { filter, merge } from 'rxjs';
import {
  MAT_DIALOG_DATA,
  MatDialog,
  MatDialogModule,
  MatDialogRef,
} from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { PerfilUsuario, UsuarioRespostaDto, UsuarioService } from '@app/core/api';
import { PERFIS_USUARIO, ROTULO_PERFIL_USUARIO } from '@app/core/models';
import { mensagemErro } from '@app/core/http/error.interceptor';
import {
  AvatarIniciaisComponent,
  ConfirmDialogComponent,
  ConfirmDialogData,
  SeloComponent,
} from '@app/shared/components';

/** Dados de entrada do `UsuarioFichaDialogComponent`. */
export interface UsuarioFichaDialogData {
  usuario: UsuarioRespostaDto;
  /**
   * True quando a ficha é do próprio funcionário logado: reset de senha, troca de perfil,
   * desativação e exclusão ficam indisponíveis (ficaria sem acesso / o caminho é "Meu perfil").
   */
  ehVoce: boolean;
}

/** Resultado ao fechar: se a lista precisa recarregar e se deve abrir o reset de senha. */
export interface UsuarioFichaDialogResultado {
  /** Houve alteração salva (edição ou exclusão) — a lista recarrega. */
  alterado: boolean;
  /** O Admin escolheu "Resetar senha" — a lista abre o `UsuarioResetSenhaDialogComponent`. */
  resetarSenha?: boolean;
  /** Dados mais recentes do funcionário (podem ter sido editados na própria modal). */
  usuario?: UsuarioRespostaDto;
}

/**
 * Ficha do funcionário em modal, aberta ao clicar na linha da lista de funcionários — mesmo
 * padrão das listas de animais/adoções (linha inteira clicável, sem coluna de ícones), mas em
 * modal porque o funcionário tem poucos dados e não precisa de página própria com abas.
 *
 * Leitura e edição no mesmo lugar (como a ficha do animal): "Editar" troca o cartão de
 * identidade pelos campos, e Salvar/Excluir chamam a API daqui mesmo. Visual de "cartão de
 * identidade" (`.etiqueta`) como o cabeçalho das fichas de animal/adoção.
 *
 * Fecha sempre com um `UsuarioFichaDialogResultado` (o diálogo é aberto com `disableClose` e
 * trata Esc/clique fora aqui) — assim a lista sabe se precisa recarregar mesmo quando a pessoa
 * salva e fecha pelo X. Durante a edição, Esc/clique fora não fecham (evita perder o que foi
 * digitado); o caminho é Cancelar.
 * Uso: `dialog.open(UsuarioFichaDialogComponent, { data: { usuario, ehVoce }, disableClose: true })`.
 */
@Component({
  selector: 'app-usuario-ficha-dialog',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DatePipe,
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatCheckboxModule,
    MatProgressSpinnerModule,
    AvatarIniciaisComponent,
    SeloComponent,
  ],
  templateUrl: './usuario-ficha-dialog.component.html',
  styleUrl: './usuario-ficha-dialog.component.scss',
})
export class UsuarioFichaDialogComponent {
  private fb = inject(FormBuilder);
  private usuarioService = inject(UsuarioService);
  private dialog = inject(MatDialog);
  private snackbar = inject(MatSnackBar);
  readonly dialogRef = inject(
    MatDialogRef<UsuarioFichaDialogComponent, UsuarioFichaDialogResultado>,
  );
  readonly data = inject<UsuarioFichaDialogData>(MAT_DIALOG_DATA);

  readonly perfis = PERFIS_USUARIO;
  readonly rotulosPerfil = ROTULO_PERFIL_USUARIO;

  /** Estado exibido — atualizado com a resposta do PUT ao salvar. */
  readonly usuario = signal<UsuarioRespostaDto>(this.data.usuario);
  readonly editando = signal(false);
  readonly salvando = signal(false);
  readonly excluindo = signal(false);
  readonly erro = signal<string | null>(null);
  private alterado = false;

  readonly ehAdmin = computed(() => this.usuario().perfil === PerfilUsuario.Admin);
  readonly rotuloPerfil = computed(() => {
    const perfil = this.usuario().perfil;
    return perfil ? ROTULO_PERFIL_USUARIO[perfil] : '—';
  });

  // Mesmas validações do backend (UsuarioUpdateDto). Perfil/ativo da própria conta ficam
  // desabilitados — `getRawValue()` ainda os envia com o valor atual.
  readonly form = this.fb.nonNullable.group({
    nome: ['', [Validators.required, Validators.maxLength(150)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(255)]],
    perfil: [PerfilUsuario.Comum, [Validators.required]],
    ativo: [true],
  });

  constructor() {
    if (this.data.ehVoce) {
      this.form.controls.perfil.disable();
      this.form.controls.ativo.disable();
    }

    // Esc / clique fora: fecha só fora da edição (e nunca no meio de uma requisição).
    merge(
      this.dialogRef.backdropClick(),
      this.dialogRef.keydownEvents().pipe(filter((e) => e.key === 'Escape')),
    )
      .pipe(takeUntilDestroyed())
      .subscribe(() => {
        if (!this.editando() && !this.salvando() && !this.excluindo()) {
          this.fechar();
        }
      });
  }

  fechar(): void {
    this.dialogRef.close({ alterado: this.alterado });
  }

  resetarSenha(): void {
    this.dialogRef.close({ alterado: this.alterado, resetarSenha: true, usuario: this.usuario() });
  }

  iniciarEdicao(): void {
    const u = this.usuario();
    this.form.reset({
      nome: u.nome ?? '',
      email: u.email ?? '',
      perfil: u.perfil ?? PerfilUsuario.Comum,
      ativo: u.ativo ?? true,
    });
    this.erro.set(null);
    this.editando.set(true);
  }

  cancelarEdicao(): void {
    this.erro.set(null);
    this.editando.set(false);
  }

  salvar(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const v = this.form.getRawValue();
    this.salvando.set(true);
    this.erro.set(null);

    this.usuarioService
      .usuarioAtualizar({
        id: this.usuario().id!,
        usuarioUpdateDto: { nome: v.nome, email: v.email, perfil: v.perfil, ativo: v.ativo },
      })
      .subscribe({
        next: (r) => {
          this.salvando.set(false);
          this.alterado = true;
          this.usuario.set(r.dados ?? { ...this.usuario(), ...v });
          this.editando.set(false);
          this.snackbar.open(`Funcionário "${r.dados?.nome}" atualizado com sucesso!`, 'Fechar', {
            duration: 4000,
          });
        },
        error: (err) => {
          this.salvando.set(false);
          this.erro.set(mensagemErro(err, 'Não foi possível salvar o funcionário.'));
        },
      });
  }

  /** Confirmação antes de excluir. A própria conta não pode ser excluída (botão desabilitado). */
  confirmarExclusao(): void {
    if (this.data.ehVoce) {
      return;
    }

    const ref = this.dialog.open(ConfirmDialogComponent, {
      data: {
        titulo: 'Excluir funcionário',
        mensagem: `Tem certeza que deseja excluir "${this.usuario().nome}"? Essa ação não pode ser desfeita.`,
        textoConfirmar: 'Excluir',
        perigoso: true,
      } satisfies ConfirmDialogData,
      width: 'min(400px, 92vw)',
    });

    ref.afterClosed().subscribe((confirmado) => {
      if (confirmado) {
        this.excluir();
      }
    });
  }

  private excluir(): void {
    this.excluindo.set(true);
    this.erro.set(null);
    this.usuarioService.usuarioExcluir({ id: this.usuario().id! }).subscribe({
      next: () => {
        this.snackbar.open('Funcionário excluído com sucesso.', 'Fechar', { duration: 4000 });
        this.alterado = true;
        this.fechar();
      },
      error: (err) => {
        this.excluindo.set(false);
        this.erro.set(mensagemErro(err, 'Não foi possível excluir o funcionário.'));
      },
    });
  }
}
