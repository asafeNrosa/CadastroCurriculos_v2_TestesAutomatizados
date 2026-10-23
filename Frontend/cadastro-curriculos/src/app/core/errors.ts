import { HttpErrorResponse } from '@angular/common/http';

export function mensagemDeErro(erro: HttpErrorResponse, padrao: string): string {
  if (erro.status === 0) return 'Não foi possível conectar à API. Verifique se o backend está em execução.';
  if (erro.status === 413) return 'O arquivo excede o tamanho máximo permitido.';
  // 401 sem mensagem própria da API (a de login traz "E-mail ou senha inválidos."): o token venceu.
  if (erro.status === 401 && (!erro.error?.title || erro.error.title === 'Unauthorized')) {
    return 'Sua sessão expirou. Entre novamente.';
  }
  return erro.error?.title ?? padrao;
}
