import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

/**
 * Página "não encontrada" (404). Exibida para qualquer URL desconhecida **e** para rotas
 * protegidas acessadas sem permissão (`authGuard`/`adminGuard` renderizam esta página
 * mantendo a URL digitada) — assim quem não tem acesso não consegue distinguir uma área
 * restrita de uma página que não existe.
 */
@Component({
  selector: 'app-nao-encontrado',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, MatButtonModule, MatIconModule],
  template: `
    <section class="nao-encontrado">
      <mat-icon class="nao-encontrado__icone">travel_explore</mat-icon>
      <h1>Página não encontrada</h1>
      <p>O endereço que você acessou não existe ou foi removido.</p>
      <div class="nao-encontrado__acoes">
        <a mat-flat-button color="primary" routerLink="/">Ir para o início</a>
        <a mat-stroked-button color="primary" routerLink="/adotar">Ver animais para adoção</a>
      </div>
    </section>
  `,
  styles: `
    .nao-encontrado {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: var(--esp-3);
      padding: var(--esp-7) var(--esp-4);
      text-align: center;
    }

    .nao-encontrado__icone {
      width: 56px;
      height: 56px;
      font-size: 56px;
      color: var(--cor-texto-suave);
    }

    .nao-encontrado h1 {
      margin: 0;
    }

    .nao-encontrado p {
      margin: 0;
      color: var(--cor-texto-suave);
    }

    .nao-encontrado__acoes {
      display: flex;
      flex-wrap: wrap;
      justify-content: center;
      gap: var(--esp-3);
      margin-top: var(--esp-3);
    }
  `,
})
export class NaoEncontradoComponent {}
