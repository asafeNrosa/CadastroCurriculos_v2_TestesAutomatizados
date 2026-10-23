import { expect, test } from '@playwright/test';
import { cadastrarPelaApi, emailUnico, evidencia, preencherDadosBasicos, preencherExperiencia } from './apoio';

test.describe('Cadastro de currículo (sem login)', () => {
  test('CT-F01 · cadastra um currículo válido sem entrar na conta', async ({ page }) => {
    const email = emailUnico('anonimo');

    await page.goto('/candidatos/novo');
    await preencherDadosBasicos(page, 'Carla Anônima', email);
    await page.getByRole('button', { name: 'Salvar', exact: true }).click();

    await expect(page).toHaveURL(/\/candidatos\/\d+$/);
    await expect(page.getByText('Cadastro salvo com sucesso.')).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Carla Anônima' })).toBeVisible();
    // Cadastro anônimo não tem dono: ninguém vê Editar nem Excluir.
    await expect(page.getByRole('button', { name: 'Editar' })).toHaveCount(0);
    await expect(page.getByRole('button', { name: 'Excluir' })).toHaveCount(0);
    await evidencia(page, 'CT-F01-cadastro-sem-login');
  });

  test('CT-F02 · valida campos obrigatórios e e-mail inválido antes de enviar', async ({ page }) => {
    await page.goto('/candidatos/novo');
    await page.getByRole('button', { name: 'Salvar', exact: true }).click();

    await expect(page.getByText('O nome completo é obrigatório.')).toBeVisible();
    await expect(page.getByText('O e-mail é obrigatório.')).toBeVisible();
    await expect(page.getByText('Corrija os campos destacados antes de salvar.')).toBeVisible();
    await expect(page).toHaveURL(/\/candidatos\/novo$/);

    await page.getByLabel('E-mail').fill('email-invalido');
    await expect(page.getByText('Informe um e-mail válido.')).toBeVisible();
    await evidencia(page, 'CT-F02-validacao-campos');
  });

  test('CT-F03 · recusa um e-mail que já está cadastrado', async ({ page, request }) => {
    const email = emailUnico('duplicado');
    await cadastrarPelaApi(request, { nomeCompleto: 'Primeira Pessoa', email });

    await page.goto('/candidatos/novo');
    await preencherDadosBasicos(page, 'Segunda Pessoa', email);
    await page.getByRole('button', { name: 'Salvar', exact: true }).click();

    await expect(page.getByText('Já existe um candidato cadastrado com este e-mail.').first()).toBeVisible();
    await expect(page).toHaveURL(/\/candidatos\/novo$/);
    await evidencia(page, 'CT-F03-email-duplicado');
  });

  test('CT-F04 · adiciona e remove experiências profissionais no formulário', async ({ page }) => {
    await page.goto('/candidatos/novo');
    await preencherDadosBasicos(page, 'Diego Experiente', emailUnico('experiencias'));

    await page.getByRole('button', { name: 'Adicionar experiência' }).click();
    await page.getByRole('button', { name: 'Adicionar experiência' }).click();
    await preencherExperiencia(page, 0, { empresa: 'Empresa Removida', cargo: 'Analista', inicio: '2020-01-10', fim: '2021-01-10' });
    await preencherExperiencia(page, 1, {
      empresa: 'Acme Tecnologia', cargo: 'Desenvolvedor Backend', inicio: '2022-03-01', descricao: 'APIs em ASP.NET Core.'
    });

    // Remove a primeira: a segunda passa a ocupar a posição 0.
    await page.getByRole('button', { name: 'Remover experiência 1' }).click();
    await expect(page.locator('#experiencia-0-empresa')).toHaveValue('Acme Tecnologia');
    await expect(page.locator('#experiencia-1-empresa')).toHaveCount(0);

    await page.getByRole('button', { name: 'Salvar', exact: true }).click();

    await expect(page).toHaveURL(/\/candidatos\/\d+$/);
    await expect(page.getByText('Desenvolvedor Backend · Acme Tecnologia')).toBeVisible();
    await expect(page.getByText('03/2022 – Atual')).toBeVisible();
    await expect(page.getByText('Empresa Removida')).toHaveCount(0);
    await evidencia(page, 'CT-F04-experiencias');
  });

  test('CT-F05 · bloqueia experiência com término anterior ao início', async ({ page }) => {
    await page.goto('/candidatos/novo');
    await preencherDadosBasicos(page, 'Elisa Datas', emailUnico('datas'));

    await page.getByRole('button', { name: 'Adicionar experiência' }).click();
    await preencherExperiencia(page, 0, { empresa: 'Acme', cargo: 'Dev', inicio: '2024-05-01', fim: '2024-01-01' });
    await page.getByRole('button', { name: 'Salvar', exact: true }).click();

    await expect(page.getByText('A data de término não pode ser anterior à data de início.')).toBeVisible();
    await expect(page).toHaveURL(/\/candidatos\/novo$/);
  });
});
