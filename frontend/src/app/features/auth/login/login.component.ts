import { ChangeDetectionStrategy, Component, inject, input, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AuthService } from '@app/core/auth/auth.service';
import { mensagemErro } from '@app/core/http/error.interceptor';

/** Tela de login. Reactive Form com validações espelhando o backend. */
@Component({
  selector: 'app-login',
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
  ],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss',
})
export class LoginComponent {
  private fb = inject(FormBuilder);
  private auth = inject(AuthService);
  private router = inject(Router);

  /**
   * URL para voltar após o login (vinda de ?returnUrl=, via componentInputBinding). Pode vir
   * `undefined` — o binding sobrescreve o default quando o query param não existe.
   */
  readonly returnUrl = input<string | undefined>(undefined);

  readonly carregando = signal(false);
  readonly erro = signal<string | null>(null);
  readonly esconderSenha = signal(true);

  readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email, Validators.maxLength(255)]],
    senha: ['', [Validators.required, Validators.minLength(6), Validators.maxLength(100)]],
  });

  alternarSenha(): void {
    this.esconderSenha.update((v) => !v);
  }

  enviar(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.carregando.set(true);
    this.erro.set(null);

    this.auth.login(this.form.getRawValue()).subscribe({
      next: () => {
        this.carregando.set(false);
        this.router.navigateByUrl(this.destinoAposLogin());
      },
      error: (err) => {
        this.carregando.set(false);
        this.erro.set(mensagemErro(err, 'Não foi possível entrar. Verifique suas credenciais.'));
      },
    });
  }

  /**
   * Para onde ir depois de logar: o painel (`/admin`), ou a tela do admin que a pessoa tentava
   * abrir quando a sessão expirou (`returnUrl`). Qualquer `returnUrl` fora do `/admin` (home,
   * o próprio `/login`, URL externa forjada no link) é ignorado — login é só de funcionário,
   * então o destino natural é sempre o painel.
   */
  private destinoAposLogin(): string {
    const url = this.returnUrl();
    return url && /^\/admin(?:[/?#]|$)/.test(url) ? url : '/admin';
  }
}
