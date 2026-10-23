import { HttpErrorResponse } from '@angular/common/http';
import { FormControl, FormGroup } from '@angular/forms';
import {
  caminhoDoCampo,
  criarExperienciaForm,
  criarFormularioCandidato,
  montarRequest,
  periodoValido,
  preencherFormulario,
  tratarErroDeSalvar
} from './formulario-candidato';
import { CandidatoCompleto } from './models/candidato.model';

const candidato: CandidatoCompleto = {
  id: 7,
  nomeCompleto: 'Maria da Silva',
  email: 'maria@exemplo.com',
  telefone: null,
  areaInteresse: 'Backend',
  resumoProfissional: null,
  dataCadastro: '2026-10-01T10:00:00Z',
  podeEditar: true,
  experiencias: [
    { id: 1, empresa: 'Acme', cargo: 'Dev', dataInicio: '2023-01-10', dataFim: null, descricao: 'APIs' }
  ]
};

describe('periodoValido', () => {
  const grupo = (inicio: string, fim: string) =>
    new FormGroup({ dataInicio: new FormControl(inicio), dataFim: new FormControl(fim) });

  it('aceita término igual ou posterior ao início', () => {
    expect(periodoValido(grupo('2024-01-01', '2024-01-01'))).toBeNull();
    expect(periodoValido(grupo('2024-01-01', '2024-06-30'))).toBeNull();
  });

  it('aceita emprego atual (sem término)', () => {
    expect(periodoValido(grupo('2024-01-01', ''))).toBeNull();
  });

  it('rejeita término anterior ao início', () => {
    expect(periodoValido(grupo('2024-05-01', '2024-01-01'))).toEqual({ periodoInvalido: true });
  });
});

describe('criarExperienciaForm', () => {
  it('começa inválido: empresa, cargo e início são obrigatórios', () => {
    const form = criarExperienciaForm();
    expect(form.invalid).toBe(true);
    expect(form.controls.empresa.errors).toEqual({ required: true });
    expect(form.controls.cargo.errors).toEqual({ required: true });
    expect(form.controls.dataInicio.errors).toEqual({ required: true });
  });

  it('fica válido com os campos obrigatórios e término em branco', () => {
    const form = criarExperienciaForm({ empresa: 'Acme', cargo: 'Dev', dataInicio: '2024-01-01' });
    expect(form.valid).toBe(true);
  });

  it('rejeita empresa só com espaços', () => {
    const form = criarExperienciaForm({ empresa: '   ', cargo: 'Dev', dataInicio: '2024-01-01' });
    expect(form.controls.empresa.errors).toEqual({ required: true });
  });
});

describe('montarRequest', () => {
  it('apara textos e converte campos vazios em null', () => {
    const form = criarFormularioCandidato();
    form.patchValue({ nomeCompleto: '  Maria  ', email: ' maria@exemplo.com ', telefone: '   ', areaInteresse: '' });

    expect(montarRequest(form)).toEqual({
      nomeCompleto: 'Maria',
      email: 'maria@exemplo.com',
      telefone: null,
      areaInteresse: null,
      resumoProfissional: null,
      experiencias: []
    });
  });

  it('inclui as experiências, com término vazio virando null', () => {
    const form = criarFormularioCandidato();
    form.controls.experiencias.push(
      criarExperienciaForm({ empresa: ' Acme ', cargo: 'Dev', dataInicio: '2024-01-01', dataFim: '', descricao: '  ' })
    );

    expect(montarRequest(form).experiencias).toEqual([
      { empresa: 'Acme', cargo: 'Dev', dataInicio: '2024-01-01', dataFim: null, descricao: null }
    ]);
  });
});

describe('preencherFormulario', () => {
  it('carrega os dados do candidato e as experiências', () => {
    const form = criarFormularioCandidato();
    preencherFormulario(form, candidato);

    expect(form.controls.nomeCompleto.value).toBe('Maria da Silva');
    expect(form.controls.telefone.value).toBe('');
    expect(form.controls.areaInteresse.value).toBe('Backend');
    expect(form.controls.experiencias.length).toBe(1);
    expect(form.controls.experiencias.at(0).controls.empresa.value).toBe('Acme');
  });

  it('substitui experiências que já estavam no formulário', () => {
    const form = criarFormularioCandidato();
    form.controls.experiencias.push(criarExperienciaForm());
    form.controls.experiencias.push(criarExperienciaForm());

    preencherFormulario(form, candidato);

    expect(form.controls.experiencias.length).toBe(1);
  });
});

describe('caminhoDoCampo', () => {
  it.each([
    ['Email', 'email'],
    ['NomeCompleto', 'nomeCompleto'],
    ['Experiencias[0].Empresa', 'experiencias.0.empresa'],
    ['Experiencias[12].DataFim', 'experiencias.12.dataFim']
  ])('%s vira %s', (chave, esperado) => {
    expect(caminhoDoCampo(chave)).toBe(esperado);
  });
});

describe('tratarErroDeSalvar', () => {
  const erro = (status: number, corpo?: unknown) => new HttpErrorResponse({ status, error: corpo });

  it('409 por e-mail repetido marca o campo e-mail', () => {
    const form = criarFormularioCandidato();
    const mensagem = tratarErroDeSalvar(form, erro(409, { title: 'Já existe um candidato cadastrado com este e-mail.' }), 'padrão');

    expect(mensagem).toBe('Já existe um candidato cadastrado com este e-mail.');
    expect(form.controls.email.errors).toEqual({ duplicado: true });
    expect(form.controls.email.touched).toBe(true);
  });

  it('409 de "já possui currículo" mostra a mensagem da API e não marca o e-mail', () => {
    const form = criarFormularioCandidato();
    const titulo = 'Você já possui um currículo cadastrado. Edite o existente.';

    expect(tratarErroDeSalvar(form, erro(409, { title: titulo }), 'padrão')).toBe(titulo);
    expect(form.controls.email.errors?.['duplicado']).toBeUndefined();
  });

  it('400 com erros por campo marca os campos, inclusive os das experiências', () => {
    const form = criarFormularioCandidato();
    form.controls.experiencias.push(criarExperienciaForm());

    const mensagem = tratarErroDeSalvar(form, erro(400, {
      errors: { Email: ['Informe um e-mail válido.'], 'Experiencias[0].Empresa': ['A empresa é obrigatória.'] }
    }), 'padrão');

    expect(mensagem).toBe('Corrija os campos destacados antes de salvar.');
    expect(form.controls.email.errors).toEqual({ servidor: 'Informe um e-mail válido.' });
    expect(form.controls.experiencias.at(0).controls.empresa.errors).toEqual({ servidor: 'A empresa é obrigatória.' });
  });

  it('outros erros usam a mensagem padrão', () => {
    expect(tratarErroDeSalvar(criarFormularioCandidato(), erro(500), 'Não foi possível salvar.')).toBe('Não foi possível salvar.');
  });
});
