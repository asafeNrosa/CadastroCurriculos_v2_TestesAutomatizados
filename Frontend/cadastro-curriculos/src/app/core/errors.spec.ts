import { HttpErrorResponse } from '@angular/common/http';
import { mensagemDeErro } from './errors';

const erro = (status: number, corpo?: unknown) => new HttpErrorResponse({ status, error: corpo });

describe('mensagemDeErro', () => {
  it('status 0 vira aviso de API fora do ar', () => {
    expect(mensagemDeErro(erro(0), 'padrão')).toContain('Não foi possível conectar à API');
  });

  it('413 vira aviso de arquivo grande demais', () => {
    expect(mensagemDeErro(erro(413), 'padrão')).toBe('O arquivo excede o tamanho máximo permitido.');
  });

  it('usa o title do ProblemDetails quando existe', () => {
    expect(mensagemDeErro(erro(409, { title: 'Já existe um candidato.' }), 'padrão')).toBe('Já existe um candidato.');
  });

  it('usa a mensagem padrão quando a API não manda title', () => {
    expect(mensagemDeErro(erro(500), 'Falhou.')).toBe('Falhou.');
  });

  it('401 de login mantém a mensagem da API', () => {
    expect(mensagemDeErro(erro(401, { title: 'E-mail ou senha inválidos.' }), 'padrão')).toBe('E-mail ou senha inválidos.');
  });

  it('401 genérico (token vencido) vira aviso de sessão expirada', () => {
    expect(mensagemDeErro(erro(401, { title: 'Unauthorized' }), 'padrão')).toBe('Sua sessão expirou. Entre novamente.');
    expect(mensagemDeErro(erro(401), 'padrão')).toBe('Sua sessão expirou. Entre novamente.');
  });
});
