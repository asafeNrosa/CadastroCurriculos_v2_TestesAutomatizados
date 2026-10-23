import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Credenciais, Sessao } from '../models/auth.model';

export const CHAVE_SESSAO = 'cadastro-curriculos.sessao';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = environment.apiUrl;

  private readonly _sessao = signal<Sessao | null>(this.lerSessaoSalva());

  readonly sessao = this._sessao.asReadonly();
  readonly logado = computed(() => this._sessao() !== null);
  readonly email = computed(() => this._sessao()?.email ?? null);

  get token(): string | null {
    return this._sessao()?.token ?? null;
  }

  entrar(credenciais: Credenciais): Observable<Sessao> {
    return this.http
      .post<Sessao>(`${this.apiUrl}/auth/login`, credenciais)
      .pipe(tap(sessao => this.guardar(sessao)));
  }

  registrar(credenciais: Credenciais): Observable<Sessao> {
    return this.http
      .post<Sessao>(`${this.apiUrl}/auth/registrar`, credenciais)
      .pipe(tap(sessao => this.guardar(sessao)));
  }

  sair(): void {
    this._sessao.set(null);
    try {
      localStorage.removeItem(CHAVE_SESSAO);
    } catch {
      // Armazenamento indisponível (modo privado, por exemplo): a sessão em memória já foi limpa.
    }
  }

  private guardar(sessao: Sessao): void {
    this._sessao.set(sessao);
    try {
      localStorage.setItem(CHAVE_SESSAO, JSON.stringify(sessao));
    } catch {
      // Sem armazenamento, a sessão vale apenas até recarregar a página.
    }
  }

  private lerSessaoSalva(): Sessao | null {
    try {
      const bruto = localStorage.getItem(CHAVE_SESSAO);
      if (!bruto) return null;

      const sessao = JSON.parse(bruto) as Sessao;
      const expirada = !sessao?.token || !(new Date(sessao.expiraEm).getTime() > Date.now());
      if (expirada) {
        localStorage.removeItem(CHAVE_SESSAO);
        return null;
      }
      return sessao;
    } catch {
      return null;
    }
  }
}
