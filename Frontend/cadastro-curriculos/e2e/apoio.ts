import { APIRequestContext, Page, expect } from '@playwright/test';

export const API_URL = process.env['E2E_API_URL'] ?? 'http://localhost:5006/api';
export const SENHA = 'senha-segura-123';

// Mesma chave usada pelo AuthService para guardar a sessão no navegador.
const CHAVE_SESSAO = 'cadastro-curriculos.sessao';

let contador = 0;

/** E-mail que nunca se repete entre execuções, para os testes não colidirem no banco. */
export function emailUnico(prefixo: string): string {
  contador += 1;
  return `${prefixo}.${Date.now()}${contador}@exemplo.com`;
}

/** Salva uma captura em e2e-evidencias/, para ilustrar o documento da atividade. */
export async function evidencia(page: Page, nome: string): Promise<void> {
  await page.screenshot({ path: `e2e-evidencias/${nome}.png`, fullPage: true });
}

interface SessaoApi {
  token: string;
  email: string;
  expiraEm: string;
}

/** Cria uma conta direto pela API (atalho de preparação; o fluxo pela tela é testado à parte). */
export async function criarContaPelaApi(request: APIRequestContext, email: string): Promise<SessaoApi> {
  const resposta = await request.post(`${API_URL}/auth/registrar`, { data: { email, senha: SENHA } });
  expect(resposta.status()).toBe(201);
  return resposta.json();
}

/** Deixa o navegador "logado" como a sessão informada, antes de a página carregar. */
export async function logarComo(page: Page, sessao: SessaoApi): Promise<void> {
  await page.addInitScript(
    ([chave, valor]) => localStorage.setItem(chave, valor),
    [CHAVE_SESSAO, JSON.stringify(sessao)]
  );
}

/** Cadastra um currículo direto pela API e devolve o id (preparação de cenário). */
export async function cadastrarPelaApi(
  request: APIRequestContext,
  dados: { nomeCompleto: string; email: string; experiencias?: object[] },
  sessao?: SessaoApi
): Promise<number> {
  const resposta = await request.post(`${API_URL}/candidatos`, {
    data: { experiencias: [], ...dados },
    headers: sessao ? { Authorization: `Bearer ${sessao.token}` } : {}
  });
  expect(resposta.status()).toBe(201);
  return (await resposta.json()).id;
}

/** Preenche nome e e-mail no formulário de cadastro. */
export async function preencherDadosBasicos(page: Page, nome: string, email: string): Promise<void> {
  await page.getByLabel('Nome completo').fill(nome);
  await page.getByLabel('E-mail').fill(email);
}

/** Preenche a n-ésima experiência (0, 1, ...) do formulário. */
export async function preencherExperiencia(
  page: Page,
  indice: number,
  dados: { empresa: string; cargo: string; inicio: string; fim?: string; descricao?: string }
): Promise<void> {
  await page.locator(`#experiencia-${indice}-empresa`).fill(dados.empresa);
  await page.locator(`#experiencia-${indice}-cargo`).fill(dados.cargo);
  await page.locator(`#experiencia-${indice}-inicio`).fill(dados.inicio);
  if (dados.fim) await page.locator(`#experiencia-${indice}-fim`).fill(dados.fim);
  if (dados.descricao) await page.locator(`#experiencia-${indice}-descricao`).fill(dados.descricao);
}
