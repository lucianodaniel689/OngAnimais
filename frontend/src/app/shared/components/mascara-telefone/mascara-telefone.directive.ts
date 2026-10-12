import { Directive, ElementRef, inject } from '@angular/core';
import { AbstractControl, NgControl, ValidationErrors, ValidatorFn } from '@angular/forms';

/** Máximo de dígitos de um telefone (padrão E.164, com DDI). */
const MAX_DIGITOS = 15;

/** Telefone brasileiro sem DDI: DDD (2) + número (8 fixo ou 9 celular). */
const MAX_DIGITOS_NACIONAL = 11;

/** Formata dígitos de um número brasileiro sem DDI, progressivamente: "(11) 9999-9999" / "(11) 99999-9999". */
function formatarNacional(digitos: string): string {
  const d = digitos.slice(0, MAX_DIGITOS_NACIONAL);
  if (d.length === 0) {
    return '';
  }
  if (d.length <= 2) {
    return `(${d}`;
  }
  const ddd = d.slice(0, 2);
  if (d.length <= 6) {
    return `(${ddd}) ${d.slice(2)}`;
  }
  // Até 10 dígitos o número tem 8 (fixo: 4-4); com 11, é celular (5-4).
  const corte = d.length === MAX_DIGITOS_NACIONAL ? 7 : 6;
  return `(${ddd}) ${d.slice(2, corte)}-${d.slice(corte)}`;
}

/**
 * Aplica a máscara de telefone a um texto qualquer (digitado ou colado):
 * - sem "+": número brasileiro → "(11) 99999-9999" (ou "(11) 9999-9999" para fixo);
 * - "+55...": "+55 (11) 99999-9999";
 * - "+" de outro país: "+" seguido só dos dígitos (sem máscara — cada país tem a sua).
 */
export function formatarTelefone(texto: string): string {
  const digitos = texto.replace(/\D/g, '');
  if (!texto.trimStart().startsWith('+')) {
    return formatarNacional(digitos);
  }
  if (digitos.startsWith('55') && digitos.length > 2) {
    return `+55 ${formatarNacional(digitos.slice(2))}`;
  }
  return `+${digitos.slice(0, MAX_DIGITOS)}`;
}

/**
 * Validador de telefone completo: 10 a 15 dígitos (DDD + número, ou com DDI). Campo vazio passa —
 * use junto com `Validators.required` quando for obrigatório. Erro: `{ telefone: true }`.
 */
export const telefoneValidator: ValidatorFn = (controle: AbstractControl): ValidationErrors | null => {
  const valor = (controle.value as string | null)?.trim();
  if (!valor) {
    return null;
  }
  const quantidade = valor.replace(/\D/g, '').length;
  return quantidade >= 10 && quantidade <= MAX_DIGITOS ? null : { telefone: true };
};

/**
 * Máscara de telefone enquanto a pessoa digita: `<input matInput formControlName="telefone" appTelefone />`.
 * Formata a cada tecla (ver `formatarTelefone`), mantém o cursor no lugar certo (contando dígitos,
 * não caracteres) e grava o valor **formatado** no form control. Apagar um caractere da máscara
 * ("(", ")", " ", "-") apaga o dígito vizinho — sem isso a máscara recolocaria o caractere e o
 * Backspace "travaria". Coloca `inputmode="tel"` (teclado numérico no celular).
 *
 * Quem precisa só dos dígitos (ex.: link `wa.me`) tira a máscara ao enviar (`replace(/\D/g, '')`).
 * Valores atribuídos por código (`setValue`/`patchValue`) não são reformatados — formate antes
 * com `formatarTelefone` se vierem crus.
 */
@Directive({
  selector: 'input[appTelefone]',
  standalone: true,
  host: {
    inputmode: 'tel',
    '(input)': 'aoDigitar($event)',
  },
})
export class MascaraTelefoneDirective {
  private elemento = inject<ElementRef<HTMLInputElement>>(ElementRef).nativeElement;
  private controle = inject(NgControl, { self: true, optional: true });

  /** Valor formatado da última digitação — para saber se um Backspace só apagou máscara. */
  private anterior = '';

  aoDigitar(evento: Event): void {
    const tipo = (evento as InputEvent).inputType ?? '';
    let texto = this.elemento.value;
    let digitosAntesDoCursor = contarDigitos(texto.slice(0, this.elemento.selectionStart ?? texto.length));

    // Apagou só um caractere da máscara (os dígitos não mudaram): apaga o dígito vizinho.
    const soApagouMascara =
      tipo.startsWith('delete') && contarDigitos(texto) === contarDigitos(this.anterior);
    if (soApagouMascara) {
      const indice = tipo === 'deleteContentForward' ? digitosAntesDoCursor : digitosAntesDoCursor - 1;
      if (indice >= 0) {
        texto = removerDigito(texto, indice);
        digitosAntesDoCursor = Math.min(digitosAntesDoCursor, indice);
      }
    }

    const formatado = formatarTelefone(texto);
    this.anterior = formatado;
    if (formatado === this.elemento.value) {
      return;
    }

    this.elemento.value = formatado;
    // O value accessor do Angular pode ter gravado o texto cru antes desta diretiva: regrava o formatado.
    this.controle?.control?.setValue(formatado, { emitModelToViewChange: false });

    if (document.activeElement === this.elemento) {
      const posicao = posicaoAposDigitos(formatado, digitosAntesDoCursor);
      this.elemento.setSelectionRange(posicao, posicao);
    }
  }
}

function contarDigitos(texto: string): number {
  return texto.replace(/\D/g, '').length;
}

/** Remove o n-ésimo dígito (base 0) do texto, mantendo o resto. */
function removerDigito(texto: string, indice: number): string {
  let vistos = 0;
  for (let i = 0; i < texto.length; i++) {
    if (/\d/.test(texto[i])) {
      if (vistos === indice) {
        return texto.slice(0, i) + texto.slice(i + 1);
      }
      vistos++;
    }
  }
  return texto;
}

/** Posição no texto formatado logo depois do n-ésimo dígito (onde o cursor deve ficar). */
function posicaoAposDigitos(formatado: string, quantidade: number): number {
  if (quantidade === 0) {
    return formatado.startsWith('+') ? 1 : 0;
  }
  let vistos = 0;
  for (let i = 0; i < formatado.length; i++) {
    if (/\d/.test(formatado[i]) && ++vistos === quantidade) {
      return i + 1;
    }
  }
  return formatado.length;
}
