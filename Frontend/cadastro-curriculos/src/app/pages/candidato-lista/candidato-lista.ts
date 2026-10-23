import { Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { mensagemDeErro } from '../../core/errors';
import { Candidato } from '../../core/models/candidato.model';
import { CandidatoService } from '../../core/services/candidato.service';

@Component({
  selector: 'app-candidato-lista',
  imports: [RouterLink, DatePipe],
  templateUrl: './candidato-lista.html',
  styleUrl: './candidato-lista.scss'
})
export class CandidatoLista {
  private readonly candidatoService = inject(CandidatoService);

  readonly candidatos = signal<Candidato[]>([]);
  readonly carregando = signal(true);
  readonly erro = signal<string | null>(null);
  readonly cadastroExcluido = history.state?.cadastroExcluido === true;

  constructor() {
    this.carregar();
  }

  carregar(): void {
    this.carregando.set(true);
    this.erro.set(null);

    this.candidatoService.listar()
      .pipe(finalize(() => this.carregando.set(false)))
      .subscribe({
        next: lista => this.candidatos.set(lista),
        error: (erro: HttpErrorResponse) =>
          this.erro.set(mensagemDeErro(erro, 'Não foi possível carregar os candidatos.'))
      });
  }
}