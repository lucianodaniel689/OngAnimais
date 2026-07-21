import { ChangeDetectionStrategy, Component, effect, inject, input, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatDialog } from '@angular/material/dialog';
import { catchError, concatMap, from, map, of, tap } from 'rxjs';
import { FotoRespostaDto, FotoService } from '@app/core/api';
import {
  ArquivoRejeitado,
  ConfirmDialogComponent,
  ConfirmDialogData,
  LoadingComponent,
  UploadImagensComponent,
} from '@app/shared/components';
import { mensagemErro } from '@app/core/http/error.interceptor';
import { urlMidia } from '@app/core/http/media-url.util';

/**
 * Galeria de fotos de um animal, embutida na aba Fotos da ficha
 * (`/admin/animais/:id`). Permite subir múltiplas imagens de uma vez,
 * marcar a foto principal e excluir fotos existentes.
 * Uso: `<app-animal-fotos [animalId]="123" />`.
 */
@Component({
  selector: 'app-animal-fotos',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    UploadImagensComponent,
    LoadingComponent,
  ],
  templateUrl: './animal-fotos.component.html',
  styleUrl: './animal-fotos.component.scss',
})
export class AnimalFotosComponent {
  private fotoService = inject(FotoService);
  private snackbar = inject(MatSnackBar);
  private dialog = inject(MatDialog);

  readonly animalId = input.required<number>();

  readonly fotos = signal<FotoRespostaDto[]>([]);
  readonly carregando = signal(true);
  readonly progresso = signal<{ atual: number; total: number } | null>(null);

  readonly urlFoto = urlMidia;

  constructor() {
    effect(() => this.carregar(this.animalId()));
  }

  aoSelecionarArquivos(arquivos: File[]): void {
    const galeriaVaziaAntes = this.fotos().length === 0;
    const total = arquivos.length;
    const erros: string[] = [];

    this.progresso.set({ atual: 0, total });

    from(arquivos)
      .pipe(
        concatMap((arquivo, indice) =>
          this.fotoService
            .fotoCriar({
              animalId: this.animalId(),
              arquivo,
              principal: galeriaVaziaAntes && indice === 0,
            })
            .pipe(
              tap(() => this.progresso.set({ atual: indice + 1, total })),
              map(() => null),
              catchError((err) => {
                erros.push(mensagemErro(err, `Falha ao enviar "${arquivo.name}".`));
                return of(null);
              }),
            ),
        ),
      )
      .subscribe({
        complete: () => {
          this.progresso.set(null);
          this.carregar(this.animalId());
          if (erros.length > 0) {
            this.snackbar.open(erros.join(' '), 'Fechar', { duration: 6000 });
          } else {
            this.snackbar.open(
              total === 1 ? 'Foto enviada com sucesso!' : `${total} fotos enviadas com sucesso!`,
              'Fechar',
              { duration: 4000 },
            );
          }
        },
      });
  }

  aoRejeitarArquivos(rejeitados: ArquivoRejeitado[]): void {
    const mensagens = rejeitados.map((r) => `"${r.arquivo.name}": ${r.motivo}`);
    this.snackbar.open(mensagens.join(' '), 'Fechar', { duration: 6000 });
  }

  tornarPrincipal(foto: FotoRespostaDto): void {
    if (!foto.id || foto.principal) {
      return;
    }
    this.fotoService
      .fotoAtualizar({
        id: foto.id,
        fotoUpdateDto: { descricao: foto.descricao, principal: true },
      })
      .subscribe({
        next: () => {
          this.carregar(this.animalId());
          this.snackbar.open('Foto principal atualizada.', 'Fechar', { duration: 3000 });
        },
        error: (err) => {
          this.snackbar.open(mensagemErro(err, 'Não foi possível atualizar a foto.'), 'Fechar', {
            duration: 4000,
          });
        },
      });
  }

  confirmarExclusao(foto: FotoRespostaDto): void {
    const ref = this.dialog.open(ConfirmDialogComponent, {
      data: {
        titulo: 'Excluir foto',
        mensagem: 'Tem certeza que deseja excluir esta foto? Essa ação não pode ser desfeita.',
        textoConfirmar: 'Excluir',
        perigoso: true,
      } satisfies ConfirmDialogData,
      width: 'min(400px, 92vw)',
    });

    ref.afterClosed().subscribe((confirmado) => {
      if (confirmado && foto.id) {
        this.excluir(foto.id);
      }
    });
  }

  private excluir(id: number): void {
    this.fotoService.fotoExcluir({ id }).subscribe({
      next: () => {
        this.carregar(this.animalId());
        this.snackbar.open('Foto excluída com sucesso.', 'Fechar', { duration: 3000 });
      },
      error: (err) => {
        this.snackbar.open(mensagemErro(err, 'Não foi possível excluir a foto.'), 'Fechar', {
          duration: 4000,
        });
      },
    });
  }

  private carregar(animalId: number): void {
    this.carregando.set(true);
    this.fotoService
      .fotoListarPorAnimal({ animalId })
      .pipe(
        map((r) => r.dados ?? []),
        catchError(() => of([] as FotoRespostaDto[])),
      )
      .subscribe((fotos) => {
        this.carregando.set(false);
        this.fotos.set(fotos);
      });
  }
}
