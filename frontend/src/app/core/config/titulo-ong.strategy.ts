import { Injectable, inject } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { RouterStateSnapshot, TitleStrategy } from '@angular/router';
import { ConfiguracaoOngService } from './configuracao-ong.service';

/**
 * Título da aba = "{nome da ONG} — {título da rota}". As rotas declaram só a parte da página
 * (`title: 'Quero adotar'`) e o nome vem das configurações — trocar o nome no painel muda
 * todas as abas, sem nome fixo espalhado pelo `app.routes.ts`.
 */
@Injectable()
export class TituloOngStrategy extends TitleStrategy {
  private title = inject(Title);
  private configuracao = inject(ConfiguracaoOngService);

  override updateTitle(snapshot: RouterStateSnapshot): void {
    const pagina = this.buildTitle(snapshot);
    const nome = this.configuracao.nomeOng();
    this.title.setTitle(pagina ? `${nome} — ${pagina}` : nome);
  }
}
