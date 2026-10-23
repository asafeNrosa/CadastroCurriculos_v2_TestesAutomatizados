import { Component, input, output } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import {
  Campo,
  CampoExperiencia,
  CandidatoFormGroup,
  ExperienciaFormGroup,
  LIMITE_EXPERIENCIAS,
  criarExperienciaForm
} from '../../core/formulario-candidato';

/**
 * Campos do currículo (dados pessoais + experiências profissionais).
 * Componente de apresentação: o formulário é criado e salvo por quem o usa
 * (página de cadastro e edição na página de detalhes).
 */
@Component({
  selector: 'app-candidato-campos',
  imports: [ReactiveFormsModule],
  templateUrl: './candidato-campos.html'
})
export class CandidatoCampos {
  readonly form = input.required<CandidatoFormGroup>();

  /** Campos preenchidos a partir do PDF (ganham destaque até o usuário editá-los). */
  readonly destaques = input<ReadonlySet<Campo>>(new Set<Campo>());

  /** Avisa quando o usuário edita um campo, para quem usa o componente tirar o destaque. */
  readonly campoEditado = output<Campo>();

  readonly limite = LIMITE_EXPERIENCIAS;

  get experiencias() {
    return this.form().controls.experiencias;
  }

  adicionarExperiencia(): void {
    if (this.experiencias.length >= this.limite) return;
    this.experiencias.push(criarExperienciaForm());
  }

  removerExperiencia(indice: number): void {
    this.experiencias.removeAt(indice);
  }

  erroDe(campo: Campo): string | null {
    const controle = this.form().controls[campo];
    if (!controle.touched || !controle.errors) return null;

    const erros = controle.errors;
    if (erros['required']) return campo === 'nomeCompleto' ? 'O nome completo é obrigatório.' : 'O e-mail é obrigatório.';
    if (erros['pattern']) return 'Informe um e-mail válido.';
    if (erros['maxlength']) return `Use no máximo ${erros['maxlength'].requiredLength} caracteres.`;
    if (erros['duplicado']) return 'Já existe um candidato cadastrado com este e-mail.';
    if (erros['servidor']) return erros['servidor'];
    return null;
  }

  erroDaExperiencia(grupo: ExperienciaFormGroup, campo: CampoExperiencia): string | null {
    const controle = grupo.controls[campo];

    if (campo === 'dataFim' && grupo.errors?.['periodoInvalido'] && (controle.touched || grupo.controls.dataInicio.touched)) {
      return 'A data de término não pode ser anterior à data de início.';
    }

    if (!controle.touched || !controle.errors) return null;

    const erros = controle.errors;
    if (erros['required']) {
      const obrigatorios: Record<string, string> = {
        empresa: 'A empresa é obrigatória.',
        cargo: 'O cargo é obrigatório.',
        dataInicio: 'A data de início é obrigatória.'
      };
      return obrigatorios[campo] ?? null;
    }
    if (erros['maxlength']) return `Use no máximo ${erros['maxlength'].requiredLength} caracteres.`;
    if (erros['servidor']) return erros['servidor'];
    return null;
  }
}
