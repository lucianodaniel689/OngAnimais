import { ChangeDetectionStrategy, Component, ElementRef, input, output, signal, viewChild } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';

/** Arquivo rejeitado na validação client-side, com o motivo para exibir ao usuário. */
export interface ArquivoRejeitado {
  arquivo: File;
  motivo: string;
}

const EXTENSOES_PADRAO = ['.jpg', '.jpeg', '.png', '.webp', '.gif'];

/**
 * Dropzone reutilizável para upload de imagens (drag & drop + seleção via clique).
 * Valida extensão e tamanho no cliente (mesmas regras do backend) antes de emitir os
 * arquivos aceitos — não sabe nada sobre para onde/como o upload será enviado.
 *
 * Uso:
 * ```html
 * <app-upload-imagens
 *   [multiplo]="true"
 *   [tamanhoMaximoMb]="15"
 *   (arquivosSelecionados)="aoSelecionar($event)"
 *   (arquivosRejeitados)="aoRejeitar($event)"
 * />
 * ```
 */
@Component({
  selector: 'app-upload-imagens',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatIconModule],
  templateUrl: './upload-imagens.component.html',
  styleUrl: './upload-imagens.component.scss',
})
export class UploadImagensComponent {
  /** Permite selecionar mais de um arquivo por vez. */
  readonly multiplo = input<boolean>(true);
  /**
   * Tamanho máximo por arquivo, em MB — o da foto **original** (câmera de celular: 3–8MB). Igual
   * ao `Armazenamento:TamanhoMaximoMb` do backend, que reduz a foto ao salvar.
   */
  readonly tamanhoMaximoMb = input<number>(15);
  /** Extensões aceitas (com o ponto, ex.: ['.jpg', '.png']). */
  readonly extensoesAceitas = input<string[]>(EXTENSOES_PADRAO);
  /** Texto de apoio exibido na dropzone. */
  readonly rotulo = input<string>('Arraste imagens aqui ou clique para selecionar');

  /** Arquivos que passaram na validação client-side. */
  readonly arquivosSelecionados = output<File[]>();
  /** Arquivos rejeitados, com o motivo (tipo ou tamanho inválido). */
  readonly arquivosRejeitados = output<ArquivoRejeitado[]>();

  readonly arrastando = signal(false);

  private readonly inputArquivo = viewChild.required<ElementRef<HTMLInputElement>>('inputArquivo');

  get aceitaAtributo(): string {
    return this.extensoesAceitas().join(',');
  }

  abrirSeletor(): void {
    this.inputArquivo().nativeElement.click();
  }

  aoSelecionarViaInput(evento: Event): void {
    const input = evento.target as HTMLInputElement;
    this.processarArquivos(input.files);
    input.value = '';
  }

  aoSoltarArquivos(evento: DragEvent): void {
    evento.preventDefault();
    this.arrastando.set(false);
    this.processarArquivos(evento.dataTransfer?.files ?? null);
  }

  aoEntrarNaZona(evento: DragEvent): void {
    evento.preventDefault();
    this.arrastando.set(true);
  }

  aoSairDaZona(evento: DragEvent): void {
    evento.preventDefault();
    this.arrastando.set(false);
  }

  aoArrastarSobre(evento: DragEvent): void {
    evento.preventDefault();
  }

  private processarArquivos(lista: FileList | null): void {
    if (!lista || lista.length === 0) {
      return;
    }

    const arquivos = this.multiplo() ? Array.from(lista) : [lista[0]];
    const aceitos: File[] = [];
    const rejeitados: ArquivoRejeitado[] = [];
    const tamanhoMaximoBytes = this.tamanhoMaximoMb() * 1024 * 1024;

    for (const arquivo of arquivos) {
      const extensao = this.extensaoDe(arquivo.name);
      if (!this.extensoesAceitas().includes(extensao)) {
        rejeitados.push({
          arquivo,
          motivo: `Tipo não permitido. Use: ${this.extensoesAceitas().join(', ')}.`,
        });
        continue;
      }
      if (!arquivo.type.startsWith('image/')) {
        rejeitados.push({ arquivo, motivo: 'O arquivo não é uma imagem válida.' });
        continue;
      }
      if (arquivo.size > tamanhoMaximoBytes) {
        rejeitados.push({
          arquivo,
          motivo: `Excede o tamanho máximo de ${this.tamanhoMaximoMb()}MB.`,
        });
        continue;
      }
      aceitos.push(arquivo);
    }

    if (aceitos.length > 0) {
      this.arquivosSelecionados.emit(aceitos);
    }
    if (rejeitados.length > 0) {
      this.arquivosRejeitados.emit(rejeitados);
    }
  }

  private extensaoDe(nomeArquivo: string): string {
    const indice = nomeArquivo.lastIndexOf('.');
    return indice >= 0 ? nomeArquivo.slice(indice).toLowerCase() : '';
  }
}
