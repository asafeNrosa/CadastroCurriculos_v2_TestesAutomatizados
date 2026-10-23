import { Component, OnInit, inject, input, numberAttribute, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { CandidatoCampos } from '../../components/candidato-campos/candidato-campos';
import { mensagemDeErro } from '../../core/errors';
import { criarFormularioCandidato, montarRequest, preencherFormulario, tratarErroDeSalvar } from '../../core/formulario-candidato';
import { CandidatoCompleto } from '../../core/models/candidato.model';
import { CandidatoService } from '../../core/services/candidato.service';

interface Mensagem {
  tipo: 'success' | 'danger';
  texto: string;
}

@Component({
  selector: 'app-candidato-detalhe',
  imports: [RouterLink, DatePipe, ReactiveFormsModule, CandidatoCampos],
  templateUrl: './candidato-detalhe.html',
  styleUrl: './candidato-detalhe.scss'
})
export class CandidatoDetalhe implements OnInit {
  private readonly candidatoService = inject(CandidatoService);
  private readonly router = inject(Router);

  // Recebe o :id da rota (withComponentInputBinding) já convertido para número
  readonly id = input.required({ transform: numberAttribute });

  readonly candidato = signal<CandidatoCompleto | null>(null);
  readonly carregando = signal(true);
  readonly erro = signal<string | null>(null);
  readonly mensagem = signal<Mensagem | null>(
    history.state?.cadastroSalvo === true ? { tipo: 'success', texto: 'Cadastro salvo com sucesso.' } : null
  );

  // Edição na própria página: o mesmo formulário de cadastro, preenchido com os dados atuais.
  readonly form = criarFormularioCandidato();
  readonly editando = signal(false);
  readonly salvando = signal(false);
  readonly mensagemEdicao = signal<string | null>(null);

  readonly confirmandoExclusao = signal(false);
  readonly excluindo = signal(false);

  ngOnInit(): void {
    if (!Number.isInteger(this.id())) {
      this.erro.set('Candidato não encontrado.');
      this.carregando.set(false);
      return;
    }

    this.candidatoService.obterPorId(this.id())
      .pipe(finalize(() => this.carregando.set(false)))
      .subscribe({
        next: candidato => this.candidato.set(candidato),
        error: (erro: HttpErrorResponse) =>
          this.erro.set(mensagemDeErro(erro, 'Não foi possível carregar o candidato.'))
      });
  }

  iniciarEdicao(): void {
    const candidato = this.candidato();
    if (!candidato?.podeEditar) return;

    preencherFormulario(this.form, candidato);
    this.mensagem.set(null);
    this.mensagemEdicao.set(null);
    this.confirmandoExclusao.set(false);
    this.editando.set(true);
  }

  cancelarEdicao(): void {
    this.editando.set(false);
    this.mensagemEdicao.set(null);
  }

  salvarEdicao(): void {
    this.mensagemEdicao.set(null);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.mensagemEdicao.set('Corrija os campos destacados antes de salvar.');
      return;
    }

    this.salvando.set(true);
    this.candidatoService.atualizar(this.id(), montarRequest(this.form))
      .pipe(finalize(() => this.salvando.set(false)))
      .subscribe({
        next: candidato => {
          this.candidato.set(candidato);
          this.editando.set(false);
          this.mensagem.set({ tipo: 'success', texto: 'Cadastro atualizado com sucesso.' });
        },
        error: (erro: HttpErrorResponse) =>
          this.mensagemEdicao.set(tratarErroDeSalvar(this.form, erro, 'Não foi possível atualizar o cadastro.'))
      });
  }

  pedirExclusao(): void {
    this.mensagem.set(null);
    this.confirmandoExclusao.set(true);
  }

  cancelarExclusao(): void {
    this.confirmandoExclusao.set(false);
  }

  confirmarExclusao(): void {
    this.excluindo.set(true);
    this.candidatoService.excluir(this.id())
      .pipe(finalize(() => this.excluindo.set(false)))
      .subscribe({
        next: () => this.router.navigate(['/candidatos'], { state: { cadastroExcluido: true } }),
        error: (erro: HttpErrorResponse) => {
          this.confirmandoExclusao.set(false);
          this.mensagem.set({ tipo: 'danger', texto: mensagemDeErro(erro, 'Não foi possível excluir o cadastro.') });
        }
      });
  }
}
