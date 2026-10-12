import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { PerfilUsuario, UsuarioService } from '@app/core/api';
import { PERFIS_USUARIO, ROTULO_PERFIL_USUARIO } from '@app/core/models';
import { mensagemErro } from '@app/core/http/error.interceptor';

/**
 * Formulário de cadastro de funcionário (área administrativa). Só cadastro: a edição e a
 * exclusão acontecem na ficha do funcionário em modal (`UsuarioFichaDialogComponent`),
 * aberta ao clicar na linha da lista.
 */
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
    MatProgressSpinnerModule,
    MatSelectModule,
  ],
  templateUrl: './usuario-form.component.html',
  styleUrl: './usuario-form.component.scss',
})
export class UsuarioFormComponent {
  private fb = inject(FormBuilder);
  private usuarioService = inject(UsuarioService);
  private router = inject(Router);
  private snackbar = inject(MatSnackBar);

  readonly perfis = PERFIS_USUARIO;
  readonly rotuloPerfil = ROTULO_PERFIL_USUARIO;

  readonly carregando = signal(false);
  readonly erro = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    nome: ['', [Validators.required, Validators.maxLength(150)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(255)]],
    senha: ['', [Validators.required, Validators.minLength(6), Validators.maxLength(100)]],
    perfil: [PerfilUsuario.Comum, [Validators.required]],
  });

  enviar(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const v = this.form.getRawValue();
    this.carregando.set(true);
    this.erro.set(null);

    this.usuarioService
      .usuarioRegistrar({
        usuarioRegistroDto: { nome: v.nome, email: v.email, senha: v.senha, perfil: v.perfil },
      })
      .subscribe({
        next: (r) => {
          this.carregando.set(false);
          this.snackbar.open(`Funcionário "${r.dados?.nome}" cadastrado com sucesso!`, 'Fechar', {
            duration: 4000,
          });
          this.router.navigateByUrl('/admin/usuarios');
        },
        error: (err) => {
          this.carregando.set(false);
          this.erro.set(mensagemErro(err, 'Não foi possível salvar o funcionário.'));
        },
      });
  }
}
