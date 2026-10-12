import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  input,
  signal,
} from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AdocaoService, AnimalRespostaDto, AnimalService } from '@app/core/api';
import { mensagemErro } from '@app/core/http/error.interceptor';
import {
  LoadingComponent,
  MascaraTelefoneDirective,
  telefoneValidator,
} from '@app/shared/components';

/**
 * Formulário para o funcionário registrar manualmente uma solicitação de
 * adoção (ex.: contato recebido por telefone/presencial).
 */
@Component({
  selector: 'app-adocao-form',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    LoadingComponent,
    MascaraTelefoneDirective,
  ],
  templateUrl: './adocao-form.component.html',
  styleUrl: './adocao-form.component.scss',
})
export class AdocaoFormComponent {
  private fb = inject(FormBuilder);
  private adocaoService = inject(AdocaoService);
  private animalService = inject(AnimalService);
  private router = inject(Router);
  private snackbar = inject(MatSnackBar);

  /**
   * Query params vindos da ficha do animal (`?animalId=` pré-seleciona o animal;
   * `?origem=ficha` faz salvar/cancelar voltarem para a ficha, aba Candidaturas).
   */
  readonly animalId = input<string>();
  readonly origem = input<string>();

  private readonly voltaParaFicha = computed(
    () => this.origem() === 'ficha' && !!this.animalId(),
  );
  /** Destino do "Voltar"/"Cancelar" — ficha do animal ou lista de adoções. */
  readonly linkVoltar = computed(() =>
    this.voltaParaFicha() ? `/admin/animais/${Number(this.animalId())}` : '/admin/adocoes',
  );
  readonly queryParamsVoltar = computed(() =>
    this.voltaParaFicha() ? { aba: 'candidaturas' } : null,
  );

  readonly animaisDisponiveis = signal<AnimalRespostaDto[]>([]);
  readonly carregandoOpcoes = signal(true);
  readonly carregando = signal(false);
  readonly erro = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    animalId: [null as number | null, Validators.required],
    nome: ['', [Validators.required, Validators.maxLength(150)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(255)]],
    telefone: ['', [Validators.required, Validators.maxLength(20), telefoneValidator]],
    observacoes: [''],
  });

  constructor() {
    this.carregarOpcoes();

    effect(() => {
      const animalId = this.animalId();
      if (animalId) {
        this.form.controls.animalId.setValue(Number(animalId));
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

    this.adocaoService
      .adocaoCriar({
        adocaoCreateDto: {
          animalId: v.animalId!,
          nomeAdotante: v.nome,
          emailAdotante: v.email,
          telefoneAdotante: v.telefone,
          observacoes: v.observacoes || undefined,
        },
      })
      .subscribe({
        next: () => {
          this.carregando.set(false);
          this.snackbar.open('Solicitação de adoção aberta com sucesso!', 'Fechar', {
            duration: 4000,
          });
          if (this.voltaParaFicha()) {
            this.router.navigate(['/admin/animais', Number(this.animalId())], {
              queryParams: { aba: 'candidaturas' },
            });
          } else {
            this.router.navigateByUrl('/admin/adocoes');
          }
        },
        error: (err) => {
          this.carregando.set(false);
          this.erro.set(mensagemErro(err, 'Não foi possível abrir a solicitação.'));
        },
      });
  }

  private carregarOpcoes(): void {
    this.carregandoOpcoes.set(true);
    this.animalService
      .animalListarDisponiveis()
      .pipe(
        map((r) => r.dados ?? []),
        catchError(() => of([] as AnimalRespostaDto[])),
      )
      .subscribe((animais) => {
        this.animaisDisponiveis.set(animais);
        this.carregandoOpcoes.set(false);
      });
  }
}
