import { ChangeDetectionStrategy, Component, computed, effect, inject, input, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSelectModule } from '@angular/material/select';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { PerfilUsuario, UsuarioService } from '@app/core/api';
import { PERFIS_USUARIO, ROTULO_PERFIL_USUARIO } from '@app/core/models';
import { AuthService } from '@app/core/auth/auth.service';
import { mensagemErro } from '@app/core/http/error.interceptor';
import {
  ConfirmDialogComponent,
  ConfirmDialogData,
  LoadingComponent,
} from '@app/shared/components';

/** Formulário de cadastro/edição de funcionário (área administrativa). */
@Component({
  selector: 'app-usuario-form',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatCheckboxModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatTooltipModule,
    LoadingComponent,
  ],
  templateUrl: './usuario-form.component.html',
  styleUrl: './usuario-form.component.scss',
})
export class UsuarioFormComponent {
  private fb = inject(FormBuilder);
  private usuarioService = inject(UsuarioService);
  private auth = inject(AuthService);
  private router = inject(Router);
  private snackbar = inject(MatSnackBar);
  private dialog = inject(MatDialog);

  /** Id do funcionário vindo da rota `/admin/usuarios/:id/editar`; ausente em modo de cadastro. */
  readonly id = input<string>();
  readonly editando = computed(() => this.id() !== undefined);
  /** Impede desativar (ou rebaixar de Admin) a própria conta — ficaria sem acesso. */
  readonly editandoAPropriaConta = computed(
    () => this.editando() && Number(this.id()) === this.auth.usuario()?.id,
  );

  readonly perfis = PERFIS_USUARIO;
  readonly rotuloPerfil = ROTULO_PERFIL_USUARIO;

  readonly carregandoDados = signal(false);
  readonly carregando = signal(false);
  readonly excluindo = signal(false);
  readonly erro = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    nome: ['', [Validators.required, Validators.maxLength(150)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(255)]],
    senha: ['', [Validators.minLength(6), Validators.maxLength(100)]],
    perfil: [PerfilUsuario.Comum, [Validators.required]],
    ativo: [true],
  });

  constructor() {
    effect(() => {
      const id = this.id();
      if (id) {
        this.form.controls.senha.clearValidators();
        this.form.controls.senha.updateValueAndValidity();
        this.carregarUsuario(Number(id));
      } else {
        this.form.controls.senha.setValidators([
          Validators.required,
          Validators.minLength(6),
          Validators.maxLength(100),
        ]);
        this.form.controls.senha.updateValueAndValidity();
      }
    });
  }

  enviar(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const v = this.form.getRawValue();
    this.carregando.set(true);
    this.erro.set(null);

    const id = this.id();
    const requisicao = id
      ? this.usuarioService.usuarioAtualizar({
          id: Number(id),
          usuarioUpdateDto: { nome: v.nome, email: v.email, perfil: v.perfil, ativo: v.ativo },
        })
      : this.usuarioService.usuarioRegistrar({
          usuarioRegistroDto: { nome: v.nome, email: v.email, senha: v.senha, perfil: v.perfil },
        });

    requisicao.subscribe({
      next: (r) => {
        this.carregando.set(false);
        this.snackbar.open(
          `Funcionário "${r.dados?.nome}" ${id ? 'atualizado' : 'cadastrado'} com sucesso!`,
          'Fechar',
          { duration: 4000 },
        );
        this.router.navigateByUrl('/admin/usuarios');
      },
      error: (err) => {
        this.carregando.set(false);
        this.erro.set(mensagemErro(err, 'Não foi possível salvar o funcionário.'));
      },
    });
  }

  /** Abre o diálogo de confirmação antes de excluir — exclusão só acontece aqui, não na lista. */
  confirmarExclusao(): void {
    if (this.editandoAPropriaConta()) {
      return;
    }

    const ref = this.dialog.open(ConfirmDialogComponent, {
      data: {
        titulo: 'Excluir funcionário',
        mensagem: `Tem certeza que deseja excluir "${this.form.controls.nome.value}"? Essa ação não pode ser desfeita.`,
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
    this.usuarioService.usuarioExcluir({ id: Number(this.id()) }).subscribe({
      next: () => {
        this.snackbar.open('Funcionário excluído com sucesso.', 'Fechar', { duration: 4000 });
        this.router.navigateByUrl('/admin/usuarios');
      },
      error: (err) => {
        this.excluindo.set(false);
        this.erro.set(mensagemErro(err, 'Não foi possível excluir o funcionário.'));
      },
    });
  }

  private carregarUsuario(id: number): void {
    this.carregandoDados.set(true);
    this.usuarioService
      .usuarioObterPorId({ id })
      .pipe(
        map((r) => r.dados),
        catchError(() => {
          this.erro.set('Não foi possível carregar os dados do funcionário.');
          return of(null);
        }),
      )
      .subscribe((usuario) => {
        this.carregandoDados.set(false);
        if (!usuario) {
          return;
        }
        this.form.patchValue({
          nome: usuario.nome ?? '',
          email: usuario.email ?? '',
          perfil: usuario.perfil ?? PerfilUsuario.Comum,
          ativo: usuario.ativo ?? true,
        });
      });
  }
}
