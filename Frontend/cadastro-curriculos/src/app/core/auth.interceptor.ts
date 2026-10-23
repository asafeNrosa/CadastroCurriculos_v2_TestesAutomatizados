import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { environment } from '../../environments/environment';
import { AuthService } from './services/auth.service';

/**
 * Anexa o token (Authorization: Bearer) às chamadas para a nossa API.
 * Se a API responder 401 para quem estava logado, o token venceu ou é inválido: encerra a sessão local.
 */
export const authInterceptor: HttpInterceptorFn = (requisicao, next) => {
  const auth = inject(AuthService);
  const token = auth.token;
  const paraNossaApi = requisicao.url.startsWith(environment.apiUrl);

  const enviada = token && paraNossaApi
    ? requisicao.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : requisicao;

  return next(enviada).pipe(
    catchError((erro: unknown) => {
      const sessaoVenceu = erro instanceof HttpErrorResponse
        && erro.status === 401
        && !!token
        && !requisicao.url.includes('/auth/');
      if (sessaoVenceu) auth.sair();
      return throwError(() => erro);
    })
  );
};
