import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Candidato, CandidatoCompleto, CandidatoRequest, DadosExtraidos } from '../models/candidato.model';

@Injectable({ providedIn: 'root' })
export class CandidatoService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = environment.apiUrl;

  listar(): Observable<Candidato[]> {
    return this.http.get<Candidato[]>(`${this.apiUrl}/candidatos`);
  }

  obterPorId(id: number): Observable<CandidatoCompleto> {
    return this.http.get<CandidatoCompleto>(`${this.apiUrl}/candidatos/${id}`);
  }

  /** Currículo do usuário logado (404 quando ele ainda não cadastrou o seu). */
  obterMeu(): Observable<CandidatoCompleto> {
    return this.http.get<CandidatoCompleto>(`${this.apiUrl}/candidatos/meu`);
  }

  cadastrar(dados: CandidatoRequest): Observable<CandidatoCompleto> {
    return this.http.post<CandidatoCompleto>(`${this.apiUrl}/candidatos`, dados);
  }

  atualizar(id: number, dados: CandidatoRequest): Observable<CandidatoCompleto> {
    return this.http.put<CandidatoCompleto>(`${this.apiUrl}/candidatos/${id}`, dados);
  }

  excluir(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/candidatos/${id}`);
  }

  extrairDoPdf(arquivo: File): Observable<DadosExtraidos> {
    const formData = new FormData();
    formData.append('arquivo', arquivo);
    return this.http.post<DadosExtraidos>(`${this.apiUrl}/curriculos/extrair`, formData);
  }
}
