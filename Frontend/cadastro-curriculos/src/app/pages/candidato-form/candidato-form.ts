import { Component, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { Router, RouterLink } from '@angular/router';
import { ReactiveFormsModule } from '@angular/forms';
import { finalize } from 'rxjs';
import { CandidatoCampos } from '../../components/candidato-campos/candidato-campos';
import { Campo, criarFormularioCandidato, montarRequest, tratarErroDeSalvar } from '../../core/formulario-candidato';
import { DadosExtraidos } from '../../core/models/candidato.model';
import { AuthService } from '../../core/services/auth.service';
import { CandidatoService } from '../../core/services/candidato.service';
import { TAMANHO_MAXIMO_PDF } from '../../core/validacao';
import { mensagemDeErro } from '../../core/errors';

interface Mensagem {
  tipo: 'success' | 'danger' | 'warning' | 'info';
  texto: string;
}

@Component({
  selector: 'app-candidato-form',
  imports: [ReactiveFormsModule, RouterLink, CandidatoCampos],
  templateUrl: './candidato-form.html',
  styleUrl: './candidato-form.scss'
})
export class CandidatoForm {
  private readonly candidatoService = inject(CandidatoService);
  private readonly router = inject(Router);
  protected readonly auth = inject(AuthService);

  readonly form = criarFormularioCandidato();

  readonly lendoPdf = signal(false);
  readonly salvando = signal(false);
  readonly nomeArquivo = signal<string | null>(null);
  readonly mensagemPdf = signal<Mensagem | null>(null);
  readonly mensagemGeral = signal<Mensagem | null>(null);
  readonly camposDoPdf = signal(new Set<Campo>());
  readonly contaCriada = history.state?.contaCriada === true;

  aoSelecionarArquivo(evento: Event): void {
    const input = evento.target as HTMLInputElement;
    const arquivo = input.files?.[0];
    input.value = '';
    if (!arquivo) return;

    this.nomeArquivo.set(arquivo.name);
    this.mensagemPdf.set(null);

    if (!arquivo.name.toLowerCase().endsWith('.pdf')) {
      this.mensagemPdf.set({ tipo: 'danger', texto: 'Formato inválido. Envie um arquivo PDF.' });
      return;
    }
    if (arquivo.size > TAMANHO_MAXIMO_PDF) {
      this.mensagemPdf.set({ tipo: 'danger', texto: 'O arquivo excede o tamanho máximo de 5 MB.' });
      return;
    }

    this.lendoPdf.set(true);
    this.candidatoService.extrairDoPdf(arquivo)
      .pipe(finalize(() => this.lendoPdf.set(false)))
      .subscribe({
        next: dados => this.aplicarDadosExtraidos(dados),
        error: (erro: HttpErrorResponse) => this.mensagemPdf.set({
          tipo: 'danger',
          texto: `${mensagemDeErro(erro, 'Não foi possível ler o PDF.')} Você pode preencher os dados manualmente.`
        })
      });
  }

  salvar(): void {
    this.mensagemGeral.set(null);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.mensagemGeral.set({ tipo: 'danger', texto: 'Corrija os campos destacados antes de salvar.' });
      return;
    }

    this.salvando.set(true);
    this.candidatoService.cadastrar(montarRequest(this.form))
      .pipe(finalize(() => this.salvando.set(false)))
      .subscribe({
        next: candidato => this.router.navigate(['/candidatos', candidato.id], { state: { cadastroSalvo: true } }),
        error: (erro: HttpErrorResponse) => this.mensagemGeral.set({
          tipo: 'danger',
          texto: tratarErroDeSalvar(this.form, erro, 'Não foi possível salvar o cadastro.')
        })
      });
  }

  removerDestaque(campo: Campo): void {
    if (!this.camposDoPdf().has(campo)) return;
    const restantes = new Set(this.camposDoPdf());
    restantes.delete(campo);
    this.camposDoPdf.set(restantes);
  }

  private aplicarDadosExtraidos(dados: DadosExtraidos): void {
    const encontrados = new Set<Campo>();
    const faltando: string[] = [];
    const campos: [Campo, string | null, string][] = [
      ['nomeCompleto', dados.nomeCompleto, 'nome'],
      ['email', dados.email, 'e-mail'],
      ['telefone', dados.telefone, 'telefone'],
      ['areaInteresse', dados.areaInteresse, 'área de interesse'],
      ['resumoProfissional', dados.resumoProfissional, 'resumo profissional']
    ];

    for (const [campo, valor, rotulo] of campos) {
      if (valor) {
        const controle = this.form.controls[campo];
        controle.setValue(valor);
        controle.markAsTouched();
        encontrados.add(campo);
      } else {
        faltando.push(rotulo);
      }
    }

    this.camposDoPdf.set(encontrados);

    if (encontrados.size === 0) {
      this.mensagemPdf.set({ tipo: 'warning', texto: 'Não identificamos dados neste PDF. Preencha os dados manualmente.' });
    } else if (faltando.length > 0) {
      this.mensagemPdf.set({ tipo: 'warning', texto: `Dados importados do PDF. Não encontramos: ${faltando.join(', ')}. Preencha manualmente e confira os campos destacados.` });
    } else {
      this.mensagemPdf.set({ tipo: 'success', texto: 'Todos os dados foram importados do PDF. Confira os campos destacados antes de salvar.' });
    }
  }
}
