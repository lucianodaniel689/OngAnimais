import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DatePipe } from '@angular/common';
import {
  AbstractControl,
  FormArray,
  FormBuilder,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { catchError, map, of } from 'rxjs';
import { MatCardModule } from '@angular/material/card';
import { MatTabsModule } from '@angular/material/tabs';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatRadioModule } from '@angular/material/radio';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatDialog } from '@angular/material/dialog';
import {
  ConfiguracaoOngRespostaDto,
  ConfiguracaoOngUpdateDto,
  ConfiguracaoService,
  FormaAjudarDto,
  PaletaCores,
  PassoAdocaoDto,
} from '@app/core/api';
import { ConfiguracaoOngService, formatarWhatsapp } from '@app/core/config/configuracao-ong.service';
import { ORDEM_PALETAS, PALETAS } from '@app/core/config/paletas';
import { mensagemErro } from '@app/core/http/error.interceptor';
import { urlMidia } from '@app/core/http/media-url.util';
import {
  ArquivoRejeitado,
  ConfirmDialogComponent,
  ConfirmDialogData,
  LoadingComponent,
  LogoOngComponent,
  MascaraTelefoneDirective,
  UploadImagensComponent,
  telefoneValidator,
} from '@app/shared/components';

/** Um passo de "Como adotar" no formulário. */
type PassoForm = FormGroup<{
  icone: FormControl<string>;
  titulo: FormControl<string>;
  descricao: FormControl<string>;
}>;

/** Um card de "Formas de ajudar" no formulário — `destino` vira `rota` (/...) ou `ancora` (#...). */
type FormaForm = FormGroup<{
  icone: FormControl<string>;
  titulo: FormControl<string>;
  descricao: FormControl<string>;
  rotuloAcao: FormControl<string>;
  destino: FormControl<string>;
  ativo: FormControl<boolean>;
}>;

/** Limites espelhados do `ConfiguracaoOngUpdateDto` do backend (só UX — a API valida de novo). */
const MAX_PASSOS = 6;
const MAX_FORMAS = 8;

/** Ícones oferecidos nos passos e cards (nome do Material Icons → rótulo no select). */
const ICONES: readonly { nome: string; rotulo: string }[] = [
  { nome: 'pets', rotulo: 'Patinha' },
  { nome: 'favorite', rotulo: 'Coração' },
  { nome: 'volunteer_activism', rotulo: 'Mão com coração' },
  { nome: 'handshake', rotulo: 'Aperto de mãos' },
  { nome: 'groups', rotulo: 'Pessoas' },
  { nome: 'campaign', rotulo: 'Megafone' },
  { nome: 'share', rotulo: 'Compartilhar' },
  { nome: 'search', rotulo: 'Lupa' },
  { nome: 'home', rotulo: 'Casa' },
  { nome: 'savings', rotulo: 'Cofrinho' },
  { nome: 'card_giftcard', rotulo: 'Presente' },
  { nome: 'restaurant', rotulo: 'Ração / alimento' },
  { nome: 'medical_services', rotulo: 'Saúde' },
  { nome: 'directions_car', rotulo: 'Transporte' },
  { nome: 'event', rotulo: 'Calendário / evento' },
  { nome: 'chat', rotulo: 'Conversa' },
  { nome: 'call', rotulo: 'Telefone' },
  { nome: 'description', rotulo: 'Documento / termo' },
  { nome: 'checklist', rotulo: 'Lista de etapas' },
  { nome: 'verified', rotulo: 'Selo de confirmação' },
];

/** Para onde o link de um card de "Formas de ajudar" leva. `#...` = âncora da home; `/...` = rota. */
const DESTINOS: readonly { valor: string; rotulo: string }[] = [
  { valor: '/adotar', rotulo: 'Página "Quero adotar" (lista de animais)' },
  { valor: '#contato', rotulo: 'Seção de contato da página inicial' },
  { valor: '#doacao', rotulo: 'Bloco de doação/PIX da página inicial' },
];

/**
 * Converte o WhatsApp digitado no formato gravado pela API (só dígitos, com DDI). Sem "+" e com
 * 10–11 dígitos = número brasileiro sem DDI → acrescenta 55. Vazio → null (item some do site).
 */
function normalizarWhatsapp(valor: string): string | null {
  const texto = valor.trim();
  if (!texto) {
    return null;
  }
  const digitos = texto.replace(/\D/g, '');
  return !texto.startsWith('+') && digitos.length <= 11 ? `55${digitos}` : digitos;
}

/** Texto aparado; vazio vira null (o backend faz o mesmo, aqui só evita mandar espaço). */
function textoOuNulo(valor: string): string | null {
  const texto = valor.trim();
  return texto ? texto : null;
}

/**
 * Configurações da ONG (exclusivo de Admin — `adminGuard` na rota; o backend também revalida o
 * perfil no banco). Três abas: "Identidade" (nome e contato), "Página inicial" e "Aparência"
 * (logo e paleta). O PUT da API substitui a configuração inteira, então os campos que esta tela
 * ainda não edita (espécies, regras de adoção, módulos...) são reenviados **como vieram** do GET.
 * Ao salvar, o site passa a usar os dados novos na hora (`ConfiguracaoOngService.aplicar`), sem
 * recarregar. O logo é exceção: grava na hora, pelo endpoint próprio, fora do botão Salvar; a
 * paleta escolhida aparece em prévia no site inteiro até salvar ou sair da tela.
 */
@Component({
  selector: 'app-configuracoes',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DatePipe,
    ReactiveFormsModule,
    MatCardModule,
    MatTabsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatCheckboxModule,
    MatRadioModule,
    MatButtonModule,
    MatIconModule,
    MatTooltipModule,
    MatProgressSpinnerModule,
    LoadingComponent,
    MascaraTelefoneDirective,
    UploadImagensComponent,
    LogoOngComponent,
  ],
  templateUrl: './configuracoes.component.html',
  styleUrl: './configuracoes.component.scss',
})
export class ConfiguracoesComponent {
  private fb = inject(FormBuilder);
  private api = inject(ConfiguracaoService);
  private configuracaoOng = inject(ConfiguracaoOngService);
  private snackbar = inject(MatSnackBar);
  private dialog = inject(MatDialog);

  readonly maxPassos = MAX_PASSOS;
  readonly maxFormas = MAX_FORMAS;

  /** Paletas do seletor da aba Aparência, na ordem de exibição. */
  readonly paletas = ORDEM_PALETAS.map((valor) => ({ valor, ...PALETAS[valor] }));

  readonly enviandoLogo = signal(false);
  readonly erroLogo = signal<string | null>(null);

  /** Logo salvo (o envio/remoção grava na hora, fora do botão Salvar). */
  readonly logoAtual = this.configuracaoOng.logo;

  /** Favicon gerado do logo, para a prévia "como fica na aba do navegador". */
  readonly faviconAtual = computed(() => urlMidia(this.configuracaoAtual()?.faviconUrl) ?? null);

  readonly carregando = signal(true);
  readonly salvando = signal(false);
  readonly erroCarregar = signal<string | null>(null);
  readonly erroSalvar = signal<string | null>(null);
  readonly abaAtiva = signal(0);

  /** Configuração como veio da API — base dos campos que esta tela não edita e do "Desfazer". */
  private readonly configuracaoAtual = signal<ConfiguracaoOngRespostaDto | null>(null);
  readonly dataAtualizacao = computed(() => this.configuracaoAtual()?.dataAtualizacao ?? null);

  readonly form = this.fb.nonNullable.group({
    identidade: this.fb.nonNullable.group({
      nomeOng: ['', [Validators.required, Validators.maxLength(150)]],
      whatsapp: ['', [telefoneValidator]],
      instagram: ['', [Validators.pattern(/^@?[A-Za-z0-9._]{1,30}$/)]],
      emailContato: ['', [Validators.email, Validators.maxLength(255)]],
      endereco: ['', [Validators.maxLength(255)]],
      horarioAtendimento: ['', [Validators.maxLength(150)]],
      mensagemWhatsapp: ['', [Validators.maxLength(500)]],
      textoContato: ['', [Validators.maxLength(1000)]],
    }),
    pagina: this.fb.nonNullable.group({
      heroTitulo: ['', [Validators.required, Validators.maxLength(150)]],
      heroSubtitulo: ['', [Validators.required, Validators.maxLength(500)]],
      quantidadeDestaque: [6, [Validators.required, Validators.min(1), Validators.max(20)]],
      passos: this.fb.array<PassoForm>([]),
      formas: this.fb.array<FormaForm>([]),
      doacaoPixChave: ['', [Validators.maxLength(150)]],
      doacaoTexto: ['', [Validators.maxLength(1000)]],
    }),
    aparencia: this.fb.nonNullable.group({
      paleta: [PaletaCores.Kraft as PaletaCores, [Validators.required]],
    }),
  });

  readonly identidade = this.form.controls.identidade.controls;
  readonly pagina = this.form.controls.pagina.controls;
  readonly aparencia = this.form.controls.aparencia.controls;

  get passos(): FormArray<PassoForm> {
    return this.pagina.passos;
  }

  get formas(): FormArray<FormaForm> {
    return this.pagina.formas;
  }

  /**
   * Ícones do select: a lista fixa + qualquer ícone que já esteja salvo e não esteja nela (ex.:
   * configurado pela API) — sem isso o select apareceria vazio para esse item.
   */
  readonly icones = signal<{ nome: string; rotulo: string }[]>([...ICONES]);

  /** Destinos do select de link — lista fixa + destinos já salvos fora dela (mesmo motivo). */
  readonly destinos = signal<{ valor: string; rotulo: string }[]>([...DESTINOS]);

  constructor() {
    this.carregar();

    // Escolher uma paleta já pinta o site inteiro (prévia, sem salvar). Ao sair da tela sem
    // salvar, as cores voltam para a paleta salva.
    this.aparencia.paleta.valueChanges
      .pipe(takeUntilDestroyed())
      .subscribe((paleta) => this.configuracaoOng.previsualizarPaleta(paleta));
    inject(DestroyRef).onDestroy(() => this.configuracaoOng.encerrarPrevisualizacao());
  }

  /** Aba com erro mostra um ícone de alerta no rótulo (só depois de tentar salvar/tocar). */
  abaComErro(aba: 'identidade' | 'pagina'): boolean {
    const grupo = this.form.controls[aba];
    return grupo.invalid && grupo.touched;
  }

  /** Mensagem do primeiro erro do campo — evita repetir um `@if` por validador no template. */
  erroDoCampo(controle: AbstractControl): string {
    const erros = controle.errors ?? {};
    if (erros['required']) {
      return 'Campo obrigatório.';
    }
    if (erros['maxlength']) {
      return `Máximo de ${erros['maxlength'].requiredLength} caracteres.`;
    }
    if (erros['min']) {
      return `O mínimo é ${erros['min'].min}.`;
    }
    if (erros['max']) {
      return `O máximo é ${erros['max'].max}.`;
    }
    if (erros['email']) {
      return 'Email inválido.';
    }
    if (erros['telefone']) {
      return 'Informe o número com DDD (ex.: (11) 99999-9999).';
    }
    if (erros['pattern']) {
      return 'Use só letras, números, ponto e sublinhado (ex.: @minhaong).';
    }
    return 'Valor inválido.';
  }

  adicionarPasso(): void {
    if (this.passos.length >= MAX_PASSOS) {
      return;
    }
    this.passos.push(this.criarPasso({ icone: 'pets', titulo: '', descricao: '' }));
    this.passos.markAsDirty();
  }

  adicionarForma(): void {
    if (this.formas.length >= MAX_FORMAS) {
      return;
    }
    this.formas.push(
      this.criarForma({
        icone: 'favorite',
        titulo: '',
        descricao: '',
        rotuloAcao: '',
        ancora: '#contato',
        ativo: true,
      }),
    );
    this.formas.markAsDirty();
  }

  /** Remove um item da lista (passos precisam de pelo menos 1 — o botão fica desabilitado). */
  remover(lista: FormArray, indice: number): void {
    lista.removeAt(indice);
    lista.markAsDirty();
  }

  /** Move um item uma posição para cima (-1) ou para baixo (+1) — a ordem é a da página. */
  mover(lista: FormArray, indice: number, direcao: -1 | 1): void {
    const destino = indice + direcao;
    if (destino < 0 || destino >= lista.length) {
      return;
    }
    const controle = lista.at(indice);
    lista.removeAt(indice, { emitEvent: false });
    lista.insert(destino, controle);
    lista.markAsDirty();
  }

  /**
   * Envia o logo (grava na hora — não depende do botão Salvar). O backend gera também o favicon
   * e o ícone de atalho do celular. Não mexe no formulário: alterações não salvas continuam lá.
   */
  enviarLogo(arquivos: File[]): void {
    const arquivo = arquivos[0];
    if (!arquivo) {
      return;
    }
    this.enviandoLogo.set(true);
    this.erroLogo.set(null);
    this.api.configuracaoAtualizarLogo({ arquivo }).subscribe({
      next: (r) => {
        this.enviandoLogo.set(false);
        this.aoMudarLogo(r.dados);
        this.snackbar.open('Logo atualizado! O ícone da aba do navegador também.', 'Fechar', { duration: 5000 });
      },
      error: (err) => {
        this.enviandoLogo.set(false);
        this.erroLogo.set(mensagemErro(err, 'Não foi possível enviar o logo.'));
      },
    });
  }

  /** Arquivo recusado já no navegador (tipo/tamanho) — mesma mensagem dos outros uploads. */
  aoRejeitarLogo(rejeitados: ArquivoRejeitado[]): void {
    this.erroLogo.set(rejeitados[0]?.motivo ?? 'Arquivo não permitido.');
  }

  /** Remove o logo (com confirmação) — o site volta ao símbolo e ao ícone padrão. */
  removerLogo(): void {
    const ref = this.dialog.open(ConfirmDialogComponent, {
      data: {
        titulo: 'Remover logo',
        mensagem: 'O site volta a usar o símbolo padrão no cabeçalho e o ícone padrão na aba do navegador.',
        textoConfirmar: 'Remover',
        perigoso: true,
      } satisfies ConfirmDialogData,
      width: 'min(400px, 92vw)',
    });
    ref.afterClosed().subscribe((confirmado) => {
      if (!confirmado) {
        return;
      }
      this.enviandoLogo.set(true);
      this.erroLogo.set(null);
      this.api.configuracaoRemoverLogo().subscribe({
        next: (r) => {
          this.enviandoLogo.set(false);
          this.aoMudarLogo(r.dados);
          this.snackbar.open('Logo removido.', 'Fechar', { duration: 4000 });
        },
        error: (err) => {
          this.enviandoLogo.set(false);
          this.erroLogo.set(mensagemErro(err, 'Não foi possível remover o logo.'));
        },
      });
    });
  }

  /**
   * Logo salvo no servidor: atualiza a base (para o "Desfazer" e o próximo PUT) e o site, sem
   * repreencher o formulário (não apaga o que a pessoa editou e ainda não salvou). O site recebe a
   * configuração **salva** — a prévia de paleta, se houver, continua por cima até salvar/sair.
   */
  private aoMudarLogo(configuracao: ConfiguracaoOngRespostaDto | undefined): void {
    if (configuracao) {
      this.configuracaoAtual.set(configuracao);
      this.configuracaoOng.aplicar(configuracao);
    }
  }

  /** Volta o formulário para o que está salvo. */
  desfazer(): void {
    const configuracao = this.configuracaoAtual();
    if (configuracao) {
      this.preencher(configuracao);
      this.erroSalvar.set(null);
    }
  }

  salvar(): void {
    const base = this.configuracaoAtual();
    if (!base) {
      return;
    }
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      // Leva a pessoa para a aba onde está o erro (as outras abas com erro também ganham o alerta).
      this.abaAtiva.set(
        this.form.controls.identidade.invalid ? 0 : this.form.controls.pagina.invalid ? 1 : 2,
      );
      return;
    }

    this.salvando.set(true);
    this.erroSalvar.set(null);

    this.api
      .configuracaoAtualizar({ configuracaoOngUpdateDto: this.montarDto(base) })
      .subscribe({
        next: (r) => {
          this.salvando.set(false);
          if (r.dados) {
            this.configuracaoAtual.set(r.dados);
            this.preencher(r.dados);
            this.configuracaoOng.aplicar(r.dados);
          }
          this.snackbar.open('Configurações salvas! O site já mostra os dados novos.', 'Fechar', {
            duration: 5000,
          });
        },
        error: (err) => {
          this.salvando.set(false);
          this.erroSalvar.set(mensagemErro(err, 'Não foi possível salvar as configurações.'));
        },
      });
  }

  private carregar(): void {
    this.api
      .configuracaoObter()
      .pipe(
        map((r) => r.dados ?? null),
        catchError((err) => {
          this.erroCarregar.set(mensagemErro(err, 'Não foi possível carregar as configurações.'));
          return of(null);
        }),
      )
      .subscribe((configuracao) => {
        this.carregando.set(false);
        if (configuracao) {
          this.configuracaoAtual.set(configuracao);
          this.preencher(configuracao);
        }
      });
  }

  /** Preenche o formulário a partir da API e o deixa "limpo" (sem alterações pendentes). */
  private preencher(c: ConfiguracaoOngRespostaDto): void {
    this.incluirOpcoesSalvas(c);

    this.form.controls.identidade.setValue({
      nomeOng: c.nomeOng ?? '',
      whatsapp: formatarWhatsapp(c.whatsapp) ?? '',
      instagram: c.instagram ? `@${c.instagram}` : '',
      emailContato: c.emailContato ?? '',
      endereco: c.endereco ?? '',
      horarioAtendimento: c.horarioAtendimento ?? '',
      mensagemWhatsapp: c.mensagemWhatsapp ?? '',
      textoContato: c.textoContato ?? '',
    });

    this.passos.clear({ emitEvent: false });
    (c.passosAdocao ?? []).forEach((p) => this.passos.push(this.criarPasso(p), { emitEvent: false }));
    this.formas.clear({ emitEvent: false });
    (c.formasAjudar ?? []).forEach((f) => this.formas.push(this.criarForma(f), { emitEvent: false }));

    this.form.controls.pagina.patchValue({
      heroTitulo: c.heroTitulo ?? '',
      heroSubtitulo: c.heroSubtitulo ?? '',
      quantidadeDestaque: c.quantidadeDestaque ?? 6,
      doacaoPixChave: c.doacaoPixChave ?? '',
      doacaoTexto: c.doacaoTexto ?? '',
    });

    this.form.controls.aparencia.setValue({ paleta: c.paleta ?? PaletaCores.Kraft });

    this.form.markAsPristine();
    this.form.markAsUntouched();
  }

  /** Ícones/destinos já salvos que não estão nas listas fixas entram como opção do select. */
  private incluirOpcoesSalvas(c: ConfiguracaoOngRespostaDto): void {
    const iconesSalvos = [
      ...(c.passosAdocao ?? []).map((p) => p.icone),
      ...(c.formasAjudar ?? []).map((f) => f.icone),
    ];
    const iconesExtras = [...new Set(iconesSalvos)]
      .filter((nome) => !ICONES.some((i) => i.nome === nome))
      .map((nome) => ({ nome, rotulo: nome }));
    this.icones.set([...ICONES, ...iconesExtras]);

    const destinosSalvos = (c.formasAjudar ?? [])
      .map((f) => f.rota ?? f.ancora)
      .filter((d): d is string => !!d);
    const destinosExtras = [...new Set(destinosSalvos)]
      .filter((valor) => !DESTINOS.some((d) => d.valor === valor))
      .map((valor) => ({ valor, rotulo: `Outro: ${valor}` }));
    this.destinos.set([...DESTINOS, ...destinosExtras]);
  }

  private criarPasso(p: PassoAdocaoDto): PassoForm {
    return this.fb.nonNullable.group({
      icone: [p.icone, [Validators.required]],
      titulo: [p.titulo, [Validators.required, Validators.maxLength(60)]],
      descricao: [p.descricao, [Validators.required, Validators.maxLength(300)]],
    });
  }

  private criarForma(f: FormaAjudarDto): FormaForm {
    return this.fb.nonNullable.group({
      icone: [f.icone, [Validators.required]],
      titulo: [f.titulo, [Validators.required, Validators.maxLength(60)]],
      descricao: [f.descricao, [Validators.required, Validators.maxLength(300)]],
      rotuloAcao: [f.rotuloAcao, [Validators.required, Validators.maxLength(60)]],
      destino: [f.rota ?? f.ancora ?? '', [Validators.required]],
      ativo: [f.ativo !== false],
    });
  }

  /** Monta o PUT: campos desta tela a partir do form + o resto exatamente como veio da API. */
  private montarDto(base: ConfiguracaoOngRespostaDto): ConfiguracaoOngUpdateDto {
    const id = this.form.controls.identidade.getRawValue();
    const pg = this.form.controls.pagina.getRawValue();

    return {
      // --- Editados nesta tela ---
      nomeOng: id.nomeOng.trim(),
      whatsapp: normalizarWhatsapp(id.whatsapp),
      instagram: textoOuNulo(id.instagram),
      emailContato: textoOuNulo(id.emailContato),
      endereco: textoOuNulo(id.endereco),
      horarioAtendimento: textoOuNulo(id.horarioAtendimento),
      mensagemWhatsapp: textoOuNulo(id.mensagemWhatsapp),
      textoContato: textoOuNulo(id.textoContato),
      heroTitulo: pg.heroTitulo.trim(),
      heroSubtitulo: pg.heroSubtitulo.trim(),
      quantidadeDestaque: pg.quantidadeDestaque,
      passosAdocao: pg.passos.map((p) => ({
        icone: p.icone,
        titulo: p.titulo.trim(),
        descricao: p.descricao.trim(),
      })),
      formasAjudar: pg.formas.map((f) => ({
        icone: f.icone,
        titulo: f.titulo.trim(),
        descricao: f.descricao.trim(),
        rotuloAcao: f.rotuloAcao.trim(),
        rota: f.destino.startsWith('/') ? f.destino : null,
        ancora: f.destino.startsWith('#') ? f.destino : null,
        ativo: f.ativo,
      })),
      doacaoPixChave: textoOuNulo(pg.doacaoPixChave),
      doacaoTexto: textoOuNulo(pg.doacaoTexto),
      paleta: this.aparencia.paleta.value,

      // --- Ainda sem tela: reenviados como vieram (o PUT substitui a configuração inteira) ---
      // (O logo não vai no PUT: tem endpoint próprio de envio/remoção.)
      imagemPadraoUrl: base.imagemPadraoUrl ?? null,
      especiesHabilitadas: base.especiesHabilitadas ?? [],
      especiesComPorte: base.especiesComPorte ?? [],
      exibirCastrado: base.exibirCastrado ?? true,
      exibirVermifugado: base.exibirVermifugado ?? true,
      aceitarCandidaturas: base.aceitarCandidaturas ?? true,
      avisoCandidaturasPausadas: base.avisoCandidaturasPausadas ?? null,
      multiplasCandidaturas: base.multiplasCandidaturas ?? true,
      recusarPendentesAoConcluir: base.recusarPendentesAoConcluir ?? true,
      statusAnimalDevolvido: base.statusAnimalDevolvido!,
      telefoneObrigatorio: base.telefoneObrigatorio ?? true,
      textoConsentimento: base.textoConsentimento ?? null,
      exibirWhatsappAnimal: base.exibirWhatsappAnimal ?? true,
      moduloVacinas: base.moduloVacinas ?? true,
      diasAlertaVacina: base.diasAlertaVacina ?? 30,
    };
  }
}
