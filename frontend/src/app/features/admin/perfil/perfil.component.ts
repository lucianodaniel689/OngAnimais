import { ChangeDetectionStrategy, Component, ViewChild, inject, signal } from '@angular/core';
import {
  AbstractControl,
  FormBuilder,
  FormGroupDirective,
  ReactiveFormsModule,
  ValidationErrors,
  ValidatorFn,
  Validators,
} from '@angular/forms';
import { catchError, map, of } from 'rxjs';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { UsuarioService } from '@app/core/api';
import { mensagemErro } from '@app/core/http/error.interceptor';
import { LoadingComponent } from '@app/shared/components';

/** Validador de grupo: exige que `confirmarSenha` seja igual a `novaSenha`. */
const senhasIguaisValidator: ValidatorFn = (grupo: AbstractControl): ValidationErrors | null => {
  const novaSenha = grupo.get('novaSenha')?.value;
  const confirmarSenha = grupo.get('confirmarSenha')?.value;
  return novaSenha && confirmarSenha && novaSenha !== confirmarSenha ? { senhasDiferentes: true } : null;
};

/** Página de autoatendimento do funcionário: editar os próprios dados e trocar a senha. */
@Component({
  selector: 'app-perfil',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    LoadingComponent,
  ],
  templateUrl: './perfil.component.html',
  styleUrl: './perfil.component.scss',
})
export class PerfilComponent {
  private fb = inject(FormBuilder);
  private usuarioService = inject(UsuarioService);
  private snackbar = inject(MatSnackBar);

  readonly carregandoDados = signal(true);
  readonly salvandoDados = signal(false);
  readonly erroDados = signal<string | null>(null);

  readonly alterandoSenha = signal(false);
  readonly erroSenha = signal<string | null>(null);

  readonly formDados = this.fb.nonNullable.group({
    nome: ['', [Validators.required, Validators.maxLength(150)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(255)]],
  });

  readonly formSenha = this.fb.nonNullable.group(
    {
      senhaAtual: ['', [Validators.required]],
      novaSenha: ['', [Validators.required, Validators.minLength(6), Validators.maxLength(100)]],
      confirmarSenha: ['', [Validators.required]],
    },
    { validators: senhasIguaisValidator },
  );

  // Referência ao FormGroupDirective (não só o FormGroup): reset() no FormGroup
  // limpa os valores, mas não a flag `submitted` da diretiva — sem isso, os
  // campos vazios reapareceriam com erro de "obrigatório" logo após o sucesso.
  @ViewChild('formSenhaDirective') private formSenhaDirective?: FormGroupDirective;

  constructor() {
    this.carregarMeusDados();
  }

  salvarDados(): void {
    if (this.formDados.invalid) {
      this.formDados.markAllAsTouched();
      return;
    }

    const v = this.formDados.getRawValue();
    this.salvandoDados.set(true);
    this.erroDados.set(null);

    // Endpoint de autoatendimento (PUT /me): opera sobre o usuário autenticado
    // via claims do JWT — funciona para qualquer perfil (o PUT /{id} é só de Admin).
    this.usuarioService
      .usuarioAtualizarMe({
        usuarioMeUpdateDto: { nome: v.nome, email: v.email },
      })
      .subscribe({
        next: () => {
          this.salvandoDados.set(false);
          this.snackbar.open(
            'Dados atualizados! O nome exibido no menu só atualiza após um novo login.',
            'Fechar',
            { duration: 6000 },
          );
        },
        error: (err) => {
          this.salvandoDados.set(false);
          this.erroDados.set(mensagemErro(err, 'Não foi possível salvar seus dados.'));
        },
      });
  }

  alterarSenha(): void {
    if (this.formSenha.invalid) {
      this.formSenha.markAllAsTouched();
      return;
    }

    const v = this.formSenha.getRawValue();
    this.alterandoSenha.set(true);
    this.erroSenha.set(null);

    this.usuarioService
      .usuarioAlterarMinhaSenha({
        usuarioSenhaDto: { senhaAtual: v.senhaAtual, novaSenha: v.novaSenha },
      })
      .subscribe({
        next: () => {
          this.alterandoSenha.set(false);
          this.formSenhaDirective?.resetForm();
          this.snackbar.open('Senha alterada com sucesso!', 'Fechar', { duration: 4000 });
        },
        error: (err) => {
          this.alterandoSenha.set(false);
          this.erroSenha.set(mensagemErro(err, 'Não foi possível alterar a senha.'));
        },
      });
  }

  private carregarMeusDados(): void {
    this.usuarioService
      .usuarioObterMe()
      .pipe(
        map((r) => r.dados),
        catchError(() => {
          this.erroDados.set('Não foi possível carregar seus dados.');
          return of(null);
        }),
      )
      .subscribe((usuario) => {
        this.carregandoDados.set(false);
        if (!usuario) {
          return;
        }
        this.formDados.patchValue({
          nome: usuario.nome ?? '',
          email: usuario.email ?? '',
        });
      });
  }
}
