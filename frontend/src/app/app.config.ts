import { ApplicationConfig, provideZoneChangeDetection } from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideNativeDateAdapter } from '@angular/material/core';

import { environment } from '@env/environment';
import { BASE_PATH } from '@app/core/api';
import { routes } from './app.routes';
import { authInterceptor } from '@app/core/auth/auth.interceptor';
import { errorInterceptor } from '@app/core/http/error.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes, withComponentInputBinding()),
    provideHttpClient(withInterceptors([authInterceptor, errorInterceptor])),
    provideAnimationsAsync(),
    provideNativeDateAdapter(),
    // URL base dos services gerados pelo openapi-generator.
    // Em dev é '' (caminhos relativos passam pelo proxy); em prod, a URL da API.
    { provide: BASE_PATH, useValue: environment.apiUrl },
  ],
};
