import { expect, test } from '@playwright/test';
import { SENHA, criarContaPelaApi, emailUnico, evidencia } from './apoio';

test.describe('Conta e login', () => {
  test('CT-F06 · cria uma conta pela tela e fica logado', async ({ page }) => {
    const email = emailUnico('novaconta');

    await page.goto('/entrar');
    await page.getByRole('tab', { name: 'Criar conta' }).click();
    await page.getByLabel('E-mail').fill(email);
    await page.getByLabel('Senha', { exact: true }).fill(SENHA);
    await page.getByLabel('Confirmar senha').fill(SENHA);
    await page.getByRole('button', { name: 'Criar conta', exact: true }).click();

    // Depois de criar a conta, o usuário segue direto para cadastrar o currículo.
    await expect(page).toHaveURL(/\/candidatos\/novo$/);
    await expect(page.getByText('Conta criada com sucesso.')).toBeVisible();
    await expect(page.getByTestId('usuario-logado')).toHaveText(email);
    await expect(page.getByRole('button', { name: 'Sair' })).toBeVisible();
    await evidencia(page, 'CT-F06-conta-criada');
  });

  test('CT-F07 · recusa senhas diferentes e senha curta ao criar a conta', async ({ page }) => {
    await page.goto('/entrar');
    await page.getByRole('tab', { name: 'Criar conta' }).click();
    await page.getByLabel('E-mail').fill(emailUnico('senhas'));
    await page.getByLabel('Senha', { exact: true }).fill('curta');
    await page.getByLabel('Confirmar senha').fill('outra');
    await page.getByRole('button', { name: 'Criar conta', exact: true }).click();

    await expect(page.getByText('A senha deve ter no mínimo 8 caracteres.')).toBeVisible();
    await expect(page.getByText('As senhas não conferem.')).toBeVisible();
    await expect(page).toHaveURL(/\/entrar$/);
  });

  test('CT-F08 · mostra erro com senha errada e entra com a senha certa', async ({ page, request }) => {
    const email = emailUnico('login');
    await criarContaPelaApi(request, email);

    await page.goto('/entrar');
    await page.getByLabel('E-mail').fill(email);
    await page.getByLabel('Senha').fill('senha-errada-999');
    await page.getByRole('button', { name: 'Entrar', exact: true }).click();
    await expect(page.getByText('E-mail ou senha inválidos.')).toBeVisible();
    await expect(page.getByTestId('usuario-logado')).toHaveCount(0);
    await evidencia(page, 'CT-F08-login-invalido');

    await page.getByLabel('Senha').fill(SENHA);
    await page.getByRole('button', { name: 'Entrar', exact: true }).click();

    // Conta nova, ainda sem currículo: vai para o formulário de cadastro.
    await expect(page).toHaveURL(/\/candidatos\/novo$/);
    await expect(page.getByTestId('usuario-logado')).toHaveText(email);
  });

  test('CT-F09 · sair encerra a sessão', async ({ page, request }) => {
    const email = emailUnico('sair');
    await criarContaPelaApi(request, email);

    await page.goto('/entrar');
    await page.getByLabel('E-mail').fill(email);
    await page.getByLabel('Senha').fill(SENHA);
    await page.getByRole('button', { name: 'Entrar', exact: true }).click();
    await expect(page.getByRole('button', { name: 'Sair' })).toBeVisible();

    await page.getByRole('button', { name: 'Sair' }).click();

    await expect(page.getByTestId('usuario-logado')).toHaveCount(0);
    await expect(page.getByRole('link', { name: 'Entrar' })).toBeVisible();
    // A sessão não volta ao recarregar a página.
    await page.reload();
    await expect(page.getByRole('link', { name: 'Entrar' })).toBeVisible();
  });
});
