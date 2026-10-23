import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../environments/environment';
import { Sessao } from '../models/auth.model';
import { AuthService, CHAVE_SESSAO } from './auth.service';

const daquiA = (minutos: number) => new Date(Date.now() + minutos * 60_000).toISOString();
const sessaoValida = (): Sessao => ({ token: 'abc.def.ghi', email: 'maria@exemplo.com', expiraEm: daquiA(60) });

function criarServico() {
  TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
  return { auth: TestBed.inject(AuthService), http: TestBed.inject(HttpTestingController) };
}

describe('AuthService', () => {
  beforeEach(() => localStorage.clear());

  it('começa deslogado quando não há sessão salva', () => {
    const { auth } = criarServico();
    expect(auth.logado()).toBe(false);
    expect(auth.token).toBeNull();
    expect(auth.email()).toBeNull();
  });

  it('entrar guarda a sessão em memória e no armazenamento do navegador', () => {
    const { auth, http } = criarServico();
    const sessao = sessaoValida();

    auth.entrar({ email: 'maria@exemplo.com', senha: 'senha-segura-123' }).subscribe();
    const requisicao = http.expectOne(`${environment.apiUrl}/auth/login`);
    expect(requisicao.request.method).toBe('POST');
    expect(requisicao.request.body).toEqual({ email: 'maria@exemplo.com', senha: 'senha-segura-123' });
    requisicao.flush(sessao);

    expect(auth.logado()).toBe(true);
    expect(auth.token).toBe(sessao.token);
    expect(auth.email()).toBe('maria@exemplo.com');
    expect(JSON.parse(localStorage.getItem(CHAVE_SESSAO)!)).toEqual(sessao);
  });

  it('registrar também deixa o usuário logado', () => {
    const { auth, http } = criarServico();

    auth.registrar({ email: 'maria@exemplo.com', senha: 'senha-segura-123' }).subscribe();
    http.expectOne(`${environment.apiUrl}/auth/registrar`).flush(sessaoValida());

    expect(auth.logado()).toBe(true);
  });

  it('falha no login não cria sessão', () => {
    const { auth, http } = criarServico();

    auth.entrar({ email: 'maria@exemplo.com', senha: 'errada' }).subscribe({ error: () => undefined });
    http.expectOne(`${environment.apiUrl}/auth/login`)
      .flush({ title: 'E-mail ou senha inválidos.' }, { status: 401, statusText: 'Unauthorized' });

    expect(auth.logado()).toBe(false);
    expect(localStorage.getItem(CHAVE_SESSAO)).toBeNull();
  });

  it('sair limpa a sessão em memória e no armazenamento', () => {
    localStorage.setItem(CHAVE_SESSAO, JSON.stringify(sessaoValida()));
    const { auth } = criarServico();
    expect(auth.logado()).toBe(true);

    auth.sair();

    expect(auth.logado()).toBe(false);
    expect(localStorage.getItem(CHAVE_SESSAO)).toBeNull();
  });

  it('restaura uma sessão salva que ainda não venceu', () => {
    localStorage.setItem(CHAVE_SESSAO, JSON.stringify(sessaoValida()));
    const { auth } = criarServico();

    expect(auth.logado()).toBe(true);
    expect(auth.email()).toBe('maria@exemplo.com');
  });

  it('descarta uma sessão salva que já venceu', () => {
    localStorage.setItem(CHAVE_SESSAO, JSON.stringify({ ...sessaoValida(), expiraEm: daquiA(-5) }));
    const { auth } = criarServico();

    expect(auth.logado()).toBe(false);
    expect(localStorage.getItem(CHAVE_SESSAO)).toBeNull();
  });

  it('ignora um valor salvo que não é JSON válido', () => {
    localStorage.setItem(CHAVE_SESSAO, 'isso não é json');
    const { auth } = criarServico();

    expect(auth.logado()).toBe(false);
  });
});
