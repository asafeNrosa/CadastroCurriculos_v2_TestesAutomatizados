import { ComponentFixture, TestBed } from '@angular/core/testing';
import { LIMITE_EXPERIENCIAS, criarFormularioCandidato } from '../../core/formulario-candidato';
import { CandidatoCampos } from './candidato-campos';

describe('CandidatoCampos', () => {
  let fixture: ComponentFixture<CandidatoCampos>;
  let componente: CandidatoCampos;
  let form: ReturnType<typeof criarFormularioCandidato>;

  beforeEach(() => {
    form = criarFormularioCandidato();
    fixture = TestBed.createComponent(CandidatoCampos);
    fixture.componentRef.setInput('form', form);
    fixture.detectChanges();
    componente = fixture.componentInstance;
  });

  const html = () => fixture.nativeElement as HTMLElement;

  it('começa sem experiências e mostra a orientação', () => {
    expect(form.controls.experiencias.length).toBe(0);
    expect(html().textContent).toContain('Nenhuma experiência adicionada');
  });

  // O app é zoneless: a tela só se atualiza por eventos (cliques), como acontece para o usuário.
  const botaoAdicionar = () =>
    Array.from(html().querySelectorAll('button')).find(b => b.textContent?.includes('Adicionar experiência'))!;

  function clicarAdicionar(vezes = 1) {
    for (let i = 0; i < vezes; i++) {
      botaoAdicionar().click();
      fixture.detectChanges();
    }
  }

  it('clicar em Adicionar experiência inclui um grupo novo e a tela ganha os campos', () => {
    clicarAdicionar();

    expect(form.controls.experiencias.length).toBe(1);
    expect(html().querySelector('#experiencia-0-empresa')).not.toBeNull();
    expect(html().textContent).not.toContain('Nenhuma experiência adicionada');
  });

  it('clicar em Remover tira o grupo escolhido e mantém os outros', () => {
    clicarAdicionar(2);
    form.controls.experiencias.at(0).controls.empresa.setValue('Primeira');
    form.controls.experiencias.at(1).controls.empresa.setValue('Segunda');

    html().querySelector<HTMLButtonElement>('[aria-label="Remover experiência 1"]')!.click();
    fixture.detectChanges();

    expect(form.controls.experiencias.length).toBe(1);
    expect(form.controls.experiencias.at(0).controls.empresa.value).toBe('Segunda');
    expect(html().querySelector('#experiencia-1-empresa')).toBeNull();
  });

  it('respeita o limite de experiências e desabilita o botão', () => {
    clicarAdicionar(LIMITE_EXPERIENCIAS + 3);

    expect(form.controls.experiencias.length).toBe(LIMITE_EXPERIENCIAS);
    expect(botaoAdicionar().disabled).toBe(true);
  });

  it('erroDe só aparece depois que o campo foi tocado', () => {
    expect(componente.erroDe('nomeCompleto')).toBeNull();

    form.controls.nomeCompleto.markAsTouched();
    expect(componente.erroDe('nomeCompleto')).toBe('O nome completo é obrigatório.');

    form.controls.email.setValue('invalido');
    form.controls.email.markAsTouched();
    expect(componente.erroDe('email')).toBe('Informe um e-mail válido.');
  });

  it('erroDaExperiencia explica data de término anterior ao início', () => {
    componente.adicionarExperiencia();
    const grupo = form.controls.experiencias.at(0);
    grupo.patchValue({ dataInicio: '2024-05-01', dataFim: '2024-01-01' });
    grupo.controls.dataFim.markAsTouched();

    expect(componente.erroDaExperiencia(grupo, 'dataFim')).toBe('A data de término não pode ser anterior à data de início.');
  });

  it('erroDaExperiencia pede empresa, cargo e início quando tocados e vazios', () => {
    componente.adicionarExperiencia();
    const grupo = form.controls.experiencias.at(0);
    grupo.markAllAsTouched();

    expect(componente.erroDaExperiencia(grupo, 'empresa')).toBe('A empresa é obrigatória.');
    expect(componente.erroDaExperiencia(grupo, 'cargo')).toBe('O cargo é obrigatório.');
    expect(componente.erroDaExperiencia(grupo, 'dataInicio')).toBe('A data de início é obrigatória.');
  });

  it('avisa quando o usuário edita um campo (para tirar o destaque do PDF)', () => {
    const editados: string[] = [];
    componente.campoEditado.subscribe(campo => editados.push(campo));

    const input = html().querySelector<HTMLInputElement>('#nomeCompleto')!;
    input.value = 'Maria';
    input.dispatchEvent(new Event('input'));

    expect(editados).toEqual(['nomeCompleto']);
  });
});
