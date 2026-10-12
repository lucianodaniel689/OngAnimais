import { ChangeDetectionStrategy, Component } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialog } from '@angular/material/dialog';
import { inject } from '@angular/core';
import {
  AnimalRespostaDto,
  EspecieAnimal,
  PorteAnimal,
  SexoAnimal,
  StatusAdocao,
  StatusAnimal,
} from '@app/core/api';
import { ESPECIES } from '@app/core/models';
import {
  AnimalCardComponent,
  ArquivoRejeitado,
  AvatarIniciaisComponent,
  FotoResponsivaDirective,
  CarrosselComponent,
  CarrosselSlideDirective,
  ConfirmDialogComponent,
  ConfirmDialogData,
  LoadingComponent,
  LogoOngComponent,
  MascaraTelefoneDirective,
  SeloComponent,
  StatTileComponent,
  StatusAdocaoBadgeComponent,
  StatusAnimalBadgeComponent,
  UploadImagensComponent,
  telefoneValidator,
} from '@app/shared/components';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatRadioModule } from '@angular/material/radio';
import { ORDEM_PALETAS, PALETAS } from '@app/core/config/paletas';

interface AmostraCor {
  nome: string;
  variavel: string;
}

/** Página de Styleguide — catálogo vivo dos componentes (Angular Material). */
@Component({
  selector: 'app-styleguide',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    MatButtonModule,
    MatIconModule,
    MatCardModule,
    MatChipsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatDatepickerModule,
    MatSnackBarModule,
    MatTableModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    StatusAnimalBadgeComponent,
    StatusAdocaoBadgeComponent,
    AnimalCardComponent,
    CarrosselComponent,
    CarrosselSlideDirective,
    UploadImagensComponent,
    StatTileComponent,
    LoadingComponent,
    SeloComponent,
    AvatarIniciaisComponent,
    FotoResponsivaDirective,
    MascaraTelefoneDirective,
    ReactiveFormsModule,
    MatRadioModule,
    LogoOngComponent,
  ],
  templateUrl: './styleguide.component.html',
  styleUrl: './styleguide.component.scss',
})
export class StyleguideComponent {
  private snackbar = inject(MatSnackBar);
  private dialog = inject(MatDialog);

  /** Paletas que a ONG pode escolher (catálogo — a em uso aparece nas amostras de cores). */
  readonly paletas = ORDEM_PALETAS.map((valor) => ({ valor, ...PALETAS[valor] }));

  /** Exemplo da máscara de telefone (`appTelefone` + `telefoneValidator`). */
  readonly telefoneExemplo = new FormControl('', { nonNullable: true, validators: [telefoneValidator] });

  abrirSnackbar(): void {
    this.snackbar.open('Animal cadastrado com sucesso! 🐾', 'Fechar', { duration: 4000 });
  }

  aoSelecionarImagensExemplo(arquivos: File[]): void {
    const nomes = arquivos.map((a) => a.name).join(', ');
    this.snackbar.open(`Arquivo(s) aceito(s): ${nomes}`, 'Fechar', { duration: 4000 });
  }

  aoRejeitarImagensExemplo(rejeitados: ArquivoRejeitado[]): void {
    const mensagens = rejeitados.map((r) => `"${r.arquivo.name}": ${r.motivo}`).join(' ');
    this.snackbar.open(mensagens, 'Fechar', { duration: 5000 });
  }

  abrirConfirmacao(): void {
    const ref = this.dialog.open(ConfirmDialogComponent, {
      data: {
        titulo: 'Excluir animal',
        mensagem: 'Tem certeza que deseja excluir "Thor"? Essa ação não pode ser desfeita.',
        textoConfirmar: 'Excluir',
        perigoso: true,
      } satisfies ConfirmDialogData,
      width: 'min(400px, 92vw)',
    });
    ref.afterClosed().subscribe((confirmado) => {
      this.snackbar.open(confirmado ? 'Confirmado.' : 'Cancelado.', 'Fechar', { duration: 3000 });
    });
  }

  abrirConfirmacaoComVinculos(): void {
    const ref = this.dialog.open(ConfirmDialogComponent, {
      data: {
        titulo: 'Excluir animal e vínculos',
        mensagem:
          '"Thor" possui registros vinculados. Ao confirmar, tudo isto será excluído junto com o animal:',
        itens: [
          { icone: 'volunteer_activism', texto: '2 adoções (candidaturas) — o histórico será perdido' },
          { icone: 'vaccines', texto: '1 vacina registrada' },
          { icone: 'photo_library', texto: '3 fotos (os arquivos também são apagados)' },
        ],
        rodape: 'Essa ação não pode ser desfeita.',
        textoConfirmar: 'Excluir tudo',
        perigoso: true,
      } satisfies ConfirmDialogData,
      width: 'min(460px, 92vw)',
    });
    ref.afterClosed().subscribe((confirmado) => {
      this.snackbar.open(confirmado ? 'Confirmado.' : 'Cancelado.', 'Fechar', { duration: 3000 });
    });
  }

  readonly cores: AmostraCor[] = [
    { nome: 'Primária', variavel: '--cor-primaria' },
    { nome: 'Primária escura', variavel: '--cor-primaria-escura' },
    { nome: 'Primária clara', variavel: '--cor-primaria-clara' },
    { nome: 'Secundária', variavel: '--cor-secundaria' },
    { nome: 'Secundária escura', variavel: '--cor-secundaria-escura' },
    { nome: 'Secundária clara', variavel: '--cor-secundaria-clara' },
    { nome: 'Sucesso', variavel: '--cor-sucesso' },
    { nome: 'Alerta', variavel: '--cor-alerta' },
    { nome: 'Perigo', variavel: '--cor-perigo' },
    { nome: 'Info', variavel: '--cor-info' },
    { nome: 'Texto', variavel: '--cor-texto' },
    { nome: 'Texto suave', variavel: '--cor-texto-suave' },
    { nome: 'Borda (barbante)', variavel: '--cor-borda' },
    { nome: 'Superfície', variavel: '--cor-superficie' },
  ];

  readonly statusAnimal: StatusAnimal[] = [
    StatusAnimal.Disponivel,
    StatusAnimal.Reservado,
    StatusAnimal.EmTratamento,
    StatusAnimal.Adotado,
  ];
  readonly statusAdocao: StatusAdocao[] = [
    StatusAdocao.Pendente,
    StatusAdocao.Aprovada,
    StatusAdocao.Concluida,
    StatusAdocao.Recusada,
    StatusAdocao.Cancelada,
  ];
  readonly especies: EspecieAnimal[] = ESPECIES;

  readonly animalExemplo: AnimalRespostaDto = {
    id: 1,
    nome: 'Thor',
    especie: EspecieAnimal.Cachorro,
    sexo: SexoAnimal.Macho,
    idadeAproximada: '2 anos',
    porte: PorteAnimal.Medio,
    descricao: 'Brincalhão, vacinado e sociável com crianças e outros animais.',
    status: StatusAnimal.Disponivel,
    dataCadastro: new Date().toISOString(),
    ativo: true,
  };

  readonly colunasTabela = ['nome', 'especie', 'status'];
  readonly animaisTabela: AnimalRespostaDto[] = [
    this.animalExemplo,
    {
      id: 2,
      nome: 'Mia',
      especie: EspecieAnimal.Gato,
      sexo: SexoAnimal.Femea,
      idadeAproximada: '1 ano',
      porte: PorteAnimal.Pequeno,
      status: StatusAnimal.Reservado,
      dataCadastro: new Date().toISOString(),
      ativo: true,
    },
  ];
}
