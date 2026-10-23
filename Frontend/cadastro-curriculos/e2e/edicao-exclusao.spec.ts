import { expect, test } from '@playwright/test';
import { API_URL, cadastrarPelaApi, criarContaPelaApi, emailUnico, evidencia, logarComo } from './apoio';

test.describe('Editar e excluir o próprio cadastro', () => {
  test('CT-F10 · quem cadastrou logado vê Editar e Excluir; o visitante não vê', async ({ page, request, browser }) => {
    const dono = await criarContaPelaApi(request, emailUnico('dono'));
    const id = await cadastrarPelaApi(request, { nomeCompleto: 'Fábio Dono', email: emailUnico('fabio') }, dono);

    await logarComo(page, dono);
    await page.goto(`/candidatos/${id}`);
    await expect(page.getByRole('button', { name: 'Editar', exact: true })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Excluir', exact: true })).toBeVisible();
    await evidencia(page, 'CT-F10-dono-ve-botoes');

    // Visitante: contexto novo, sem sessão.
    const visitante = await browser.newContext();
    const paginaVisitante = await visitante.newPage();
    await paginaVisitante.goto(`/candidatos/${id}`);
    await expect(paginaVisitante.getByRole('heading', { name: 'Fábio Dono' })).toBeVisible();
    await expect(paginaVisitante.getByRole('button', { name: 'Editar' })).toHaveCount(0);
    await expect(paginaVisitante.getByRole('button', { name: 'Excluir' })).toHaveCount(0);
    await visitante.close();
  });

  test('CT-F11 · edita o cadastro na própria página de detalhes', async ({ page, request }) => {
    const dono = await criarContaPelaApi(request, emailUnico('editor'));
    const id = await cadastrarPelaApi(request, { nomeCompleto: 'Gabriela Antes', email: emailUnico('gabriela') }, dono);

    await logarComo(page, dono);
    await page.goto(`/candidatos/${id}`);
    await page.getByRole('button', { name: 'Editar', exact: true }).click();

    // O formulário abre na mesma página, já preenchido.
    await expect(page).toHaveURL(new RegExp(`/candidatos/${id}$`));
    await expect(page.getByLabel('Nome completo')).toHaveValue('Gabriela Antes');

    await page.getByLabel('Nome completo').fill('Gabriela Depois');
    await page.getByLabel('Área ou cargo de interesse').fill('Backend .NET');
    await page.getByRole('button', { name: 'Adicionar experiência' }).click();
    await page.locator('#experiencia-0-empresa').fill('Beta Sistemas');
    await page.locator('#experiencia-0-cargo').fill('Desenvolvedora');
    await page.locator('#experiencia-0-inicio').fill('2023-08-01');
    await page.getByRole('button', { name: 'Salvar alterações' }).click();

    await expect(page.getByText('Cadastro atualizado com sucesso.')).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Gabriela Depois' })).toBeVisible();
    await expect(page.getByText('Backend .NET')).toBeVisible();
    await expect(page.getByText('Desenvolvedora · Beta Sistemas')).toBeVisible();
    await expect(page).toHaveURL(new RegExp(`/candidatos/${id}$`));
    await evidencia(page, 'CT-F11-edicao-na-mesma-pagina');

    // Persistiu: recarregar a página mostra os dados novos.
    await page.reload();
    await expect(page.getByRole('heading', { name: 'Gabriela Depois' })).toBeVisible();
  });

  test('CT-F12 · cancelar a edição não altera o cadastro', async ({ page, request }) => {
    const dono = await criarContaPelaApi(request, emailUnico('cancela'));
    const id = await cadastrarPelaApi(request, { nomeCompleto: 'Heitor Original', email: emailUnico('heitor') }, dono);

    await logarComo(page, dono);
    await page.goto(`/candidatos/${id}`);
    await page.getByRole('button', { name: 'Editar', exact: true }).click();
    await page.getByLabel('Nome completo').fill('Nome Que Não Vale');
    await page.getByRole('button', { name: 'Cancelar' }).click();

    await expect(page.getByRole('heading', { name: 'Heitor Original' })).toBeVisible();
    await expect(page.getByText('Nome Que Não Vale')).toHaveCount(0);
  });

  test('CT-F13 · exclui o cadastro depois de confirmar', async ({ page, request }) => {
    const dono = await criarContaPelaApi(request, emailUnico('excluidor'));
    const email = emailUnico('isabela');
    const id = await cadastrarPelaApi(request, { nomeCompleto: 'Isabela Excluída', email }, dono);

    await logarComo(page, dono);
    await page.goto(`/candidatos/${id}`);
    await page.getByRole('button', { name: 'Excluir', exact: true }).click();

    await expect(page.getByText('Excluir este cadastro?')).toBeVisible();
    await evidencia(page, 'CT-F13-confirmar-exclusao');
    await page.getByRole('button', { name: 'Confirmar exclusão' }).click();

    await expect(page).toHaveURL(/\/candidatos$/);
    await expect(page.getByText('Cadastro excluído com sucesso.')).toBeVisible();
    await expect(page.getByText(email)).toHaveCount(0);

    const consulta = await request.get(`${API_URL}/candidatos/${id}`);
    expect(consulta.status()).toBe(404);
  });

  test('CT-F14 · o botão Meu currículo leva ao cadastro do usuário logado', async ({ page, request }) => {
    const dono = await criarContaPelaApi(request, emailUnico('meu'));
    const id = await cadastrarPelaApi(request, { nomeCompleto: 'Joana Minha', email: emailUnico('joana') }, dono);

    await logarComo(page, dono);
    await page.goto('/');
    await page.getByRole('button', { name: 'Meu currículo' }).click();

    await expect(page).toHaveURL(new RegExp(`/candidatos/${id}$`));
    await expect(page.getByRole('heading', { name: 'Joana Minha' })).toBeVisible();
  });
});

test.describe('Segurança: cada um só mexe no que é seu', () => {
  test('CT-F15 · outro usuário logado não vê os botões e a API recusa a alteração', async ({ page, request }) => {
    const dono = await criarContaPelaApi(request, emailUnico('dono2'));
    const outro = await criarContaPelaApi(request, emailUnico('intruso'));
    const id = await cadastrarPelaApi(request, { nomeCompleto: 'Kátia Alheia', email: emailUnico('katia') }, dono);

    await logarComo(page, outro);
    await page.goto(`/candidatos/${id}`);
    await expect(page.getByRole('heading', { name: 'Kátia Alheia' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Editar' })).toHaveCount(0);
    await expect(page.getByRole('button', { name: 'Excluir' })).toHaveCount(0);
    await evidencia(page, 'CT-F15-outro-usuario-sem-botoes');

    // Mesmo contornando a tela, a API recusa: 401 sem token, 403 com o token de outra pessoa.
    const corpo = { nomeCompleto: 'Invasor', email: emailUnico('invasor') };
    const semToken = await request.put(`${API_URL}/candidatos/${id}`, { data: corpo });
    expect(semToken.status()).toBe(401);

    const comTokenAlheio = { Authorization: `Bearer ${outro.token}` };
    const edicao = await request.put(`${API_URL}/candidatos/${id}`, { data: corpo, headers: comTokenAlheio });
    expect(edicao.status()).toBe(403);
    const exclusao = await request.delete(`${API_URL}/candidatos/${id}`, { headers: comTokenAlheio });
    expect(exclusao.status()).toBe(403);
  });
});
