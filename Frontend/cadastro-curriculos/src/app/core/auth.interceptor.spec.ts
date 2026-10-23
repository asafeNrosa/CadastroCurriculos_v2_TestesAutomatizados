import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../environments/environment';
import { authInterceptor } from './auth.interceptor';
import { AuthService, CHAVE_SESSAO } from './services/auth.service';

function preparar(logado: boolean) {
  localStorage.clear();
  if (logado) {
    localStorage.setItem(CHAVE_SESSAO, JSON.stringify({
      token: 'token-de-teste', email: 'maria@exemplo.com', expiraEm: new Date(Date.now() + 3_600_000).toISOString()
    }));
  }
  TestBed.configureTestingModule({
    providers: [provideHttpClient(withInterceptors([authInterceptor])), provideHttpClientTesting()]
  });
  return {
    http: TestBed.inject(HttpClient),
    controle: TestBed.inject(HttpTestingController),
    auth: TestBed.inject(AuthService)
  };
}

describe('authInterceptor', () => {
  it('envia o token nas chamadas para a nossa API quando está logado', () => {
    const { http, controle } = preparar(true);

    http.get(`${environment.apiUrl}/candidatos`).subscribe();

    const requisicao = controle.expectOne(`${environment.apiUrl}/candidatos`);
    expect(requisicao.request.headers.get('Authorization')).toBe('Bearer token-de-teste');
    requisicao.flush([]);
  });

  it('não envia Authorization quando está deslogado', () => {
    const { http, controle } = preparar(false);

    http.get(`${environment.apiUrl}/candidatos`).subscribe();

    const requisicao = controle.expectOne(`${environment.apiUrl}/candidatos`);
    expect(requisicao.request.headers.has('Authorization')).toBe(false);
    requisicao.flush([]);
  });

  it('não vaza o token para outros endereços', () => {
    const { http, controle } = preparar(true);

    http.get('https://outro-site.exemplo.com/dados').subscribe();

    const requisicao = controle.expectOne('https://outro-site.exemplo.com/dados');
    expect(requisicao.request.headers.has('Authorization')).toBe(false);
    requisicao.flush({});
  });

  it('encerra a sessão quando a API responde 401 para quem estava logado', () => {
    const { http, controle, auth } = preparar(true);

    http.put(`${environment.apiUrl}/candidatos/1`, {}).subscribe({ error: () => undefined });
    controle.expectOne(`${environment.apiUrl}/candidatos/1`)
      .flush({ title: 'Unauthorized' }, { status: 401, statusText: 'Unauthorized' });

    expect(auth.logado()).toBe(false);
  });

  it('mantém a sessão quando o 401 vem do próprio login (senha errada)', () => {
    const { http, controle, auth } = preparar(true);

    http.post(`${environment.apiUrl}/auth/login`, {}).subscribe({ error: () => undefined });
    controle.expectOne(`${environment.apiUrl}/auth/login`)
      .flush({ title: 'E-mail ou senha inválidos.' }, { status: 401, statusText: 'Unauthorized' });

    expect(auth.logado()).toBe(true);
  });

  it('um 403 não derruba a sessão', () => {
    const { http, controle, auth } = preparar(true);

    http.delete(`${environment.apiUrl}/candidatos/1`).subscribe({ error: () => undefined });
    controle.expectOne(`${environment.apiUrl}/candidatos/1`)
      .flush({ title: 'Você só pode excluir o seu próprio cadastro.' }, { status: 403, statusText: 'Forbidden' });

    expect(auth.logado()).toBe(true);
  });
});
