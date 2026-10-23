import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';
import { CandidatoService } from './candidato.service';

/** Leva o usuário logado ao seu currículo, ou ao formulário de cadastro se ele ainda não tiver um. */
@Injectable({ providedIn: 'root' })
export class MeuCurriculo {
  private readonly candidatos = inject(CandidatoService);
  private readonly router = inject(Router);

  abrir(): void {
    this.candidatos.obterMeu().subscribe({
      next: candidato => this.router.navigate(['/candidatos', candidato.id]),
      error: (erro: HttpErrorResponse) =>
        this.router.navigate(erro.status === 404 ? ['/candidatos/novo'] : ['/'])
    });
  }
}
