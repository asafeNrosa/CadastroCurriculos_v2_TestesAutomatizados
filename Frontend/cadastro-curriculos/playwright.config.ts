import { defineConfig, devices } from '@playwright/test';

/**
 * Testes funcionais (E2E) do sistema, pela interface web.
 *
 * Pré-requisito: a API precisa estar rodando em http://localhost:5006 (dotnet run) com o banco acessível.
 * O front sobe sozinho (npm start) se ainda não estiver no ar.
 *
 * Variáveis opcionais:
 *   E2E_BASE_URL  endereço do front   (padrão http://localhost:4200)
 *   E2E_API_URL   endereço da API     (padrão http://localhost:5006/api)
 *   PW_CHROMIUM_PATH  caminho de um Chromium já instalado (útil quando o download do Playwright não é possível)
 */
export default defineConfig({
  testDir: './e2e',
  timeout: 30_000,
  expect: { timeout: 7_000 },
  // Em sequência: os testes usam o mesmo banco e fica mais fácil acompanhar na gravação.
  fullyParallel: false,
  workers: 1,
  retries: 0,
  reporter: [['list'], ['html', { open: 'never' }]],
  use: {
    baseURL: process.env['E2E_BASE_URL'] ?? 'http://localhost:4200',
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
    launchOptions: { 
      executablePath: process.env['PW_CHROMIUM_PATH'] || undefined,
      slowMo: 600
     }
  },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
  webServer: {
    command: 'npm start',
    url: process.env['E2E_BASE_URL'] ?? 'http://localhost:4200',
    reuseExistingServer: true,
    timeout: 120_000
  }
});
