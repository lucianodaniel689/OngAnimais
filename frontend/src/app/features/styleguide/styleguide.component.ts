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
  CarrosselComponent,
  CarrosselSlideDirective,
  ConfirmDialogComponent,
  ConfirmDialogData,
  LoadingComponent,
  StatTileComponent,
  StatusAdocaoBadgeComponent,
  StatusAnimalBadgeComponent,
  UploadImagensComponent,
} from '@app/shared/components';

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
    StatusAnimalBadgeComponent,
    StatusAdocaoBadgeComponent,
    AnimalCardComponent,
    CarrosselComponent,
    CarrosselSlideDirective,
    UploadImagensComponent,
    StatTileComponent,
    LoadingComponent,
  ],
  templateUrl: './styleguide.component.html',
  styleUrl: './styleguide.component.scss',
})
export class StyleguideComponent {
  private snackbar = inject(MatSnackBar);
  private dialog = inject(MatDialog);

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
