import { HttpErrorResponse } from '@angular/common/http';
import { AbstractControl, FormArray, FormControl, FormGroup, ValidatorFn, Validators } from '@angular/forms';
import { CandidatoCompleto, CandidatoRequest, ExperienciaRequest } from './models/candidato.model';
import { mensagemDeErro } from './errors';
import { EMAIL_REGEX, obrigatorio } from './validacao';

export const LIMITE_EXPERIENCIAS = 10;

export type Campo = 'nomeCompleto' | 'email' | 'telefone' | 'areaInteresse' | 'resumoProfissional';
export type CampoExperiencia = 'empresa' | 'cargo' | 'dataInicio' | 'dataFim' | 'descricao';

/** A data de término (quando existe) não pode ser anterior à de início. Datas ISO comparam como texto. */
export const periodoValido: ValidatorFn = grupo => {
  const inicio = grupo.get('dataInicio')?.value as string;
  const fim = grupo.get('dataFim')?.value as string;
  return inicio && fim && fim < inicio ? { periodoInvalido: true } : null;
};

function texto(valor = '', validadores: ValidatorFn[] = []): FormControl<string> {
  return new FormControl<string>(valor, { nonNullable: true, validators: validadores });
}

export function criarExperienciaForm(valores?: Partial<ExperienciaRequest>) {
  return new FormGroup(
    {
      empresa: texto(valores?.empresa ?? '', [obrigatorio, Validators.maxLength(150)]),
      cargo: texto(valores?.cargo ?? '', [obrigatorio, Validators.maxLength(100)]),
      dataInicio: texto(valores?.dataInicio ?? '', [Validators.required]),
      dataFim: texto(valores?.dataFim ?? ''),
      descricao: texto(valores?.descricao ?? '', [Validators.maxLength(1000)])
    },
    { validators: periodoValido }
  );
}

export type ExperienciaFormGroup = ReturnType<typeof criarExperienciaForm>;

export function criarFormularioCandidato() {
  return new FormGroup({
    nomeCompleto: texto('', [obrigatorio, Validators.maxLength(150)]),
    email: texto('', [obrigatorio, Validators.maxLength(254), Validators.pattern(EMAIL_REGEX)]),
    telefone: texto('', [Validators.maxLength(20)]),
    areaInteresse: texto('', [Validators.maxLength(100)]),
    resumoProfissional: texto('', [Validators.maxLength(2000)]),
    experiencias: new FormArray<ExperienciaFormGroup>([])
  });
}

export type CandidatoFormGroup = ReturnType<typeof criarFormularioCandidato>;

/** Converte o formulário no corpo enviado à API (textos aparados; vazio vira null). */
export function montarRequest(form: CandidatoFormGroup): CandidatoRequest {
  const v = form.getRawValue();
  return {
    nomeCompleto: v.nomeCompleto.trim(),
    email: v.email.trim(),
    telefone: v.telefone.trim() || null,
    areaInteresse: v.areaInteresse.trim() || null,
    resumoProfissional: v.resumoProfissional.trim() || null,
    experiencias: v.experiencias.map(e => ({
      empresa: e.empresa.trim(),
      cargo: e.cargo.trim(),
      dataInicio: e.dataInicio,
      dataFim: e.dataFim || null,
      descricao: e.descricao.trim() || null
    }))
  };
}

/** Carrega um candidato no formulário (usado ao entrar no modo de edição). */
export function preencherFormulario(form: CandidatoFormGroup, candidato: CandidatoCompleto): void {
  form.reset({
    nomeCompleto: candidato.nomeCompleto,
    email: candidato.email,
    telefone: candidato.telefone ?? '',
    areaInteresse: candidato.areaInteresse ?? '',
    resumoProfissional: candidato.resumoProfissional ?? ''
  });
  form.controls.experiencias.clear();
  for (const e of candidato.experiencias) {
    form.controls.experiencias.push(criarExperienciaForm(e));
  }
}

/**
 * Chaves de erro da API vêm como "Email" ou "Experiencias[0].Empresa".
 * Viram o caminho do formulário: "email" ou "experiencias.0.empresa".
 */
export function caminhoDoCampo(chave: string): string {
  return chave
    .replace(/\[(\d+)\]/g, '.$1')
    .split('.')
    .filter(Boolean)
    .map(parte => (/^\d+$/.test(parte) ? parte : parte.charAt(0).toLowerCase() + parte.slice(1)))
    .join('.');
}

export function aplicarErrosDoServidor(form: AbstractControl, errosPorCampo: Record<string, string[]>): void {
  for (const [chave, mensagens] of Object.entries(errosPorCampo)) {
    const controle = form.get(caminhoDoCampo(chave));
    controle?.setErrors({ servidor: mensagens[0] });
    controle?.markAsTouched();
  }
}

/** Trata a falha de um cadastro/edição: marca os campos com erro e devolve a mensagem geral. */
export function tratarErroDeSalvar(form: CandidatoFormGroup, erro: HttpErrorResponse, padrao: string): string {
  const titulo = String(erro.error?.title ?? '').toLowerCase();

  // 409 por e-mail repetido. O 409 de "você já possui um currículo" cai na mensagem da própria API.
  if (erro.status === 409 && titulo.includes('e-mail')) {
    const email = form.controls.email;
    email.setErrors({ duplicado: true });
    email.markAsTouched();
    return 'Já existe um candidato cadastrado com este e-mail.';
  }

  const errosPorCampo = erro.error?.errors as Record<string, string[]> | undefined;
  if (erro.status === 400 && errosPorCampo) {
    aplicarErrosDoServidor(form, errosPorCampo);
    return 'Corrija os campos destacados antes de salvar.';
  }

  return mensagemDeErro(erro, padrao);
}
