import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { environment } from '../../../environments/environment';
import { CandidatoCompleto } from '../../core/models/candidato.model';
import { CandidatoDetalhe } from './candidato-detalhe';

const URL_CANDIDATO = `${environment.apiUrl}/candidatos/5`;

const candidato = (sobrescrever: Partial<CandidatoCompleto> = {}): CandidatoCompleto => ({
  id: 5,
  nomeCompleto: 'Maria da Silva',
  email: 'maria@exemplo.com',
  telefone: null,
  areaInteresse: 'Backend',
  resumoProfissional: null,
  dataCadastro: '2026-10-01T10:00:00Z',
  podeEditar: false,
  experiencias: [
    { id: 1, empresa: 'Acme', cargo: 'Dev', dataInicio: '2023-01-10', dataFim: '2024-06-30', descricao: 'APIs' },
    { id: 2, empresa: 'Beta', cargo: 'Dev Pleno', dataInicio: '2024-07-01', dataFim: null, descricao: null }
  ],
  ...sobrescrever
});

describe('CandidatoDetalhe', () => {
  let fixture: ComponentFixture<CandidatoDetalhe>;
  let http: HttpTestingController;

  function abrir(dados: CandidatoCompleto) {
    TestBed.configureTestingModule({
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()]
    });
    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(CandidatoDetalhe);
    fixture.componentRef.setInput('id', 5);
    fixture.detectChanges();
    http.expectOne(URL_CANDIDATO).flush(dados);
    fixture.detectChanges();
  }

  const html = () => fixture.nativeElement as HTMLElement;
  const botao = (texto: string) =>
    Array.from(html().querySelectorAll('button')).find(b => b.textContent?.trim() === texto);

  it('mostra os dados e as experiências (mais recente primeiro, como a API entrega)', () => {
    abrir(candidato());

    expect(html().querySelector('h1')?.textContent).toContain('Maria da Silva');
    const texto = html().textContent ?? '';
    expect(texto).toContain('Dev · Acme');
    expect(texto).toContain('01/2023 – 06/2024');
    expect(texto).toContain('07/2024 – Atual');
  });

  it('mostra aviso quando não há experiências', () => {
    abrir(candidato({ experiencias: [] }));

    expect(html().textContent).toContain('Nenhuma experiência informada.');
  });

  it('NÃO mostra Editar e Excluir quando o usuário não é o dono', () => {
    abrir(candidato({ podeEditar: false }));

    expect(botao('Editar')).toBeUndefined();
    expect(botao('Excluir')).toBeUndefined();
  });

  it('mostra Editar e Excluir quando o usuário é o dono', () => {
    abrir(candidato({ podeEditar: true }));

    expect(botao('Editar')).toBeDefined();
    expect(botao('Excluir')).toBeDefined();
  });

  it('Editar abre o formulário na mesma página, preenchido com os dados atuais', () => {
    abrir(candidato({ podeEditar: true }));

    botao('Editar')!.click();
    fixture.detectChanges();

    expect(fixture.componentInstance.editando()).toBe(true);
    expect(html().querySelector<HTMLInputElement>('#nomeCompleto')?.value).toBe('Maria da Silva');
    expect(html().querySelector<HTMLInputElement>('#experiencia-1-empresa')?.value).toBe('Beta');
  });

  it('salvar a edição envia PUT com os dados alterados e atualiza a tela', () => {
    abrir(candidato({ podeEditar: true }));
    botao('Editar')!.click();
    fixture.detectChanges();

    fixture.componentInstance.form.controls.nomeCompleto.setValue('Maria Souza');
    fixture.componentInstance.salvarEdicao();

    const requisicao = http.expectOne(URL_CANDIDATO);
    expect(requisicao.request.method).toBe('PUT');
    expect(requisicao.request.body.nomeCompleto).toBe('Maria Souza');
    expect(requisicao.request.body.experiencias.length).toBe(2);
    requisicao.flush(candidato({ podeEditar: true, nomeCompleto: 'Maria Souza' }));
    fixture.detectChanges();

    expect(fixture.componentInstance.editando()).toBe(false);
    expect(html().querySelector('h1')?.textContent).toContain('Maria Souza');
    expect(html().textContent).toContain('Cadastro atualizado com sucesso.');
  });

  it('não envia a edição quando o formulário está inválido', () => {
    abrir(candidato({ podeEditar: true }));
    botao('Editar')!.click();
    fixture.detectChanges();

    fixture.componentInstance.form.controls.nomeCompleto.setValue('   ');
    fixture.componentInstance.salvarEdicao();

    http.expectNone(URL_CANDIDATO);
    expect(fixture.componentInstance.mensagemEdicao()).toBe('Corrija os campos destacados antes de salvar.');
  });

  it('cancelar a edição volta à visualização sem enviar nada', () => {
    abrir(candidato({ podeEditar: true }));
    botao('Editar')!.click();
    fixture.detectChanges();

    fixture.componentInstance.cancelarEdicao();
    fixture.detectChanges();

    http.expectNone(URL_CANDIDATO);
    expect(fixture.componentInstance.editando()).toBe(false);
  });

  it('Excluir pede confirmação antes de apagar', () => {
    abrir(candidato({ podeEditar: true }));

    botao('Excluir')!.click();
    fixture.detectChanges();

    http.expectNone(URL_CANDIDATO);
    expect(html().textContent).toContain('Excluir este cadastro?');
  });

  it('confirmar a exclusão envia DELETE e volta para a lista', () => {
    abrir(candidato({ podeEditar: true }));
    const navegar = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

    botao('Excluir')!.click();
    fixture.detectChanges();
    botao('Confirmar exclusão')!.click();

    const requisicao = http.expectOne(URL_CANDIDATO);
    expect(requisicao.request.method).toBe('DELETE');
    requisicao.flush(null, { status: 204, statusText: 'No Content' });

    expect(navegar).toHaveBeenCalledWith(['/candidatos'], { state: { cadastroExcluido: true } });
  });

  it('falha na exclusão mostra a mensagem e mantém o cadastro na tela', () => {
    abrir(candidato({ podeEditar: true }));
    botao('Excluir')!.click();
    fixture.detectChanges();
    botao('Confirmar exclusão')!.click();

    http.expectOne(URL_CANDIDATO).flush(
      { title: 'Você só pode excluir o seu próprio cadastro.' },
      { status: 403, statusText: 'Forbidden' }
    );
    fixture.detectChanges();

    expect(html().textContent).toContain('Você só pode excluir o seu próprio cadastro.');
    expect(html().querySelector('h1')?.textContent).toContain('Maria da Silva');
  });
});
