import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, map, tap } from 'rxjs';
import {
  PerfilUsuario,
  UsuarioService,
  UsuarioLoginDto,
  UsuarioRegistroDto,
  UsuarioRespostaDto,
} from '@app/core/api';

const CHAVE_TOKEN = 'onganimais.token';

/** Claims relevantes do JWT emitido pelo backend. */
interface PayloadToken {
  sub: string;
  email: string;
  given_name: string;
  /** Perfil de acesso ('Admin' | 'Comum') — claim de nome estável emitida pelo backend. */
  perfil?: string;
  exp: number;
}

/** Usuário autenticado, derivado do token. */
export interface UsuarioAutenticado {
  id: number;
  email: string;
  nome: string;
  /** Tokens antigos (anteriores à Fase 14) não têm a claim — tratado como Comum. */
  perfil: PerfilUsuario;
}

/**
 * Gerencia autenticação e estado do token. As chamadas HTTP de login/registro
 * usam o `UsuarioService` gerado pelo openapi-generator; aqui ficam a
 * persistência do token e os signals de estado.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private usuarioApi = inject(UsuarioService);

  /**
   * True quando o navegador tinha um token que já expirou (funcionário cuja sessão venceu).
   * O `authGuard` usa isso para mandar essa pessoa ao login em vez da página 404 que
   * visitantes sem sessão veem. Declarado antes de `_token`, que o preenche ao inicializar.
   */
  private _sessaoExpirada = false;

  /** Estado reativo do token. */
  private readonly _token = signal<string | null>(this.lerTokenValido());

  /** Usuário autenticado derivado do token (ou null). */
  readonly usuario = computed<UsuarioAutenticado | null>(() => {
    const token = this._token();
    if (!token) {
      return null;
    }
    const payload = this.decodificar(token);
    if (!payload) {
      return null;
    }
    return {
      id: Number(payload.sub),
      email: payload.email,
      nome: payload.given_name,
      perfil: payload.perfil === PerfilUsuario.Admin ? PerfilUsuario.Admin : PerfilUsuario.Comum,
    };
  });

  /** True quando há token válido (não expirado). */
  readonly autenticado = computed(() => this.usuario() !== null);

  /** True quando o funcionário logado tem perfil Admin (gestão de funcionários). */
  readonly ehAdmin = computed(() => this.usuario()?.perfil === PerfilUsuario.Admin);

  registrar(dto: UsuarioRegistroDto): Observable<UsuarioRespostaDto> {
    return this.usuarioApi
      .usuarioRegistrar({ usuarioRegistroDto: dto })
      .pipe(map((r) => r.dados!));
  }

  login(dto: UsuarioLoginDto): Observable<UsuarioAutenticado> {
    return this.usuarioApi.usuarioLogin({ usuarioLoginDto: dto }).pipe(
      tap((r) => {
        const token = (r.dados as { token?: string } | null)?.token;
        if (!token) {
          throw new Error('Resposta de login sem token.');
        }
        this.guardarToken(token);
      }),
      map(() => this.usuario()!),
    );
  }

  logout(): void {
    localStorage.removeItem(CHAVE_TOKEN);
    this._token.set(null);
    this._sessaoExpirada = false;
  }

  /** Encerra a sessão por expiração (401 da API) — o próximo acesso ao admin leva ao login. */
  expirarSessao(): void {
    this.logout();
    this._sessaoExpirada = true;
  }

  get token(): string | null {
    return this._token();
  }

  /** Ver `_sessaoExpirada`. */
  get sessaoExpirada(): boolean {
    return this._sessaoExpirada;
  }

  // --- internos -------------------------------------------------------------

  private guardarToken(token: string): void {
    localStorage.setItem(CHAVE_TOKEN, token);
    this._token.set(token);
    this._sessaoExpirada = false;
  }

  /** Lê o token do storage só se ainda for válido; senão limpa. */
  private lerTokenValido(): string | null {
    const token = localStorage.getItem(CHAVE_TOKEN);
    if (!token) {
      return null;
    }
    const payload = this.decodificar(token);
    if (!payload || payload.exp * 1000 <= Date.now()) {
      localStorage.removeItem(CHAVE_TOKEN);
      this._sessaoExpirada = !!payload;
      return null;
    }
    return token;
  }

  private decodificar(token: string): PayloadToken | null {
    try {
      const base64 = token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/');
      return JSON.parse(atob(base64)) as PayloadToken;
    } catch {
      return null;
    }
  }
}
