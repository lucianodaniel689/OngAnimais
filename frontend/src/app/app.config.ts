import {
  ApplicationConfig,
  inject,
  provideAppInitializer,
  provideZoneChangeDetection,
} from '@angular/core';
import {
  TitleStrategy,
  provideRouter,
  withComponentInputBinding,
  withInMemoryScrolling,
} from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { MAT_FORM_FIELD_DEFAULT_OPTIONS } from '@angular/material/form-field';

import { environment } from '@env/environment';
import { BASE_PATH } from '@app/core/api';
import { routes } from './app.routes';
import { authInterceptor } from '@app/core/auth/auth.interceptor';
import { errorInterceptor } from '@app/core/http/error.interceptor';
import { ConfiguracaoOngService } from '@app/core/config/configuracao-ong.service';
import { TituloOngStrategy } from '@app/core/config/titulo-ong.strategy';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    // Sem isso, trocar de página (ex.: home → /adotar) mantém a rolagem onde
    // estava, em vez de voltar pro topo — parece bug pra quem usa o site.
    // `anchorScrolling` cobre os links internos tipo `#contato`/`#ajudar`.
    provideRouter(
      routes,
      withComponentInputBinding(),
      withInMemoryScrolling({ scrollPositionRestoration: 'enabled', anchorScrolling: 'enabled' }),
    ),
    provideHttpClient(withInterceptors([authInterceptor, errorInterceptor])),
    provideAnimationsAsync(),
    // Configurações da ONG (nome, contato, textos da home) antes da primeira tela — evita o
    // site abrir com o texto padrão e "piscar" para o da ONG. Tem tempo limite e nunca falha
    // (ver ConfiguracaoOngService.carregar).
    provideAppInitializer(() => inject(ConfiguracaoOngService).carregar()),
    { provide: TitleStrategy, useClass: TituloOngStrategy },
    // URL base dos services gerados pelo openapi-generator.
    // Em dev é '' (caminhos relativos passam pelo proxy); em prod, a URL da API.
    { provide: BASE_PATH, useValue: environment.apiUrl },
    // Por padrão o Material reserva altura fixa (1 linha) pro hint/erro do
    // form-field, posicionado em `position: absolute` — um `mat-hint` que
    // quebra em 2+ linhas (comum no celular) "flutua" fora do fluxo normal e
    // sobrepõe o que vem depois no layout. `dynamic` faz essa área crescer de
    // verdade com o conteúdo, em vez de só ficar bonito quando cabe numa linha.
    { provide: MAT_FORM_FIELD_DEFAULT_OPTIONS, useValue: { subscriptSizing: 'dynamic' } },
  ],
};
