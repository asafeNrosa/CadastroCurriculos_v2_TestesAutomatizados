# Cadastro de Currículos

Aplicação para a equipe de recrutamento cadastrar e consultar candidatos, desenvolvida como desafio técnico para o CIEE/PR.

O cadastro pode ser feito de duas formas, pelo **mesmo formulário e com as mesmas regras de validação**:

- **Manual:** a pessoa preenche os dados e salva.
- **Com PDF:** a pessoa envia um currículo (opcional, até 5 MB), o backend extrai o texto e tenta identificar **nome, e-mail, telefone, área de interesse e resumo profissional**. Os dados encontrados preenchem o formulário e podem ser corrigidos antes de salvar.

Depois de salvo, o candidato aparece na listagem, com acesso a uma tela de detalhes.

**Contas (opcionais):** o cadastro continua aberto a todos, sem login. Quem criar uma conta e cadastrar o currículo estando logado passa a ser o dono dele e pode **editar** (na própria tela de detalhes) e **excluir** o cadastro. Cada conta tem no máximo um currículo. Cadastros feitos sem login não pertencem a ninguém e não podem ser alterados.

**Experiências profissionais:** o currículo aceita de 0 a 10 experiências (empresa, cargo, início, término e descrição), adicionadas e removidas no formulário.

O relato do desenvolvimento, as decisões técnicas e o uso de IA estão em [DESENVOLVIMENTO.md](DESENVOLVIMENTO.md).

---

## Tecnologias e versões

| Camada | Tecnologia | Versão |
|---|---|---|
| Frontend | Angular (componentes standalone, signals, zoneless) | 21.2.x |
| Frontend | Bootstrap (customizado via Sass) | 5.3.8 |
| Backend | ASP.NET Core Web API | .NET 10 |
| Backend | Entity Framework Core (SqlServer, Design, Tools) | 10.0.12 |
| Backend | PdfPig (leitura de PDF) | 0.1.16 |
| Backend | Microsoft.AspNetCore.Authentication.JwtBearer (login com JWT) | 10.0.12 |
| Banco de dados | SQL Server Express | 2025 (17.0) |
| Testes (backend) | xUnit / xunit.runner.visualstudio / Microsoft.NET.Test.Sdk | 2.9.3 / 3.1.4 / 17.14.1 |
| Testes (backend) | Microsoft.AspNetCore.Mvc.Testing / EF Core Sqlite (integração, banco em memória) | 10.0.12 |
| Testes (frontend) | Vitest (via `ng test`) e Playwright (ponta a ponta) | do `package.json` |

Ambiente usado no desenvolvimento: Windows, Visual Studio 2026 (18.10), Node.js 24.13, npm 11.6, Angular CLI 21.2.

---

## Estrutura do repositório

```
├── .github/workflows/                    # Deploy da API e do frontend no Azure (GitHub Actions)
├── Backend/
│   └── Cadastro_Curriculos/
│       ├── Cadastro_Curriculos/          # API ASP.NET Core
│       │   ├── Controllers/              # CandidatosController, CurriculosController, AuthController
│       │   ├── Data/                     # AppDbContext
│       │   ├── Dtos/                     # Request e Response
│       │   ├── Infrastructure/           # GlobalExceptionHandler, ErrosDeBanco
│       │   ├── Migrations/               # Estrutura do banco (EF Core)
│       │   ├── Models/                   # Candidato, ExperienciaProfissional, Usuario
│       │   ├── Services/                 # LeitorPdf, CurriculoExtrator, GeradorDeToken
│       │   ├── appsettings.example.json  # Modelo de configuração
│       │   └── Cadastro_Curriculos.http  # Requisições para teste manual
│       └── Cadastro_Curriculos.Tests/    # Testes unitários e de integração (xUnit)
├── Frontend/
│   └── cadastro-curriculos/              # Aplicação Angular (testes unitários em src/, ponta a ponta em e2e/)
├── exemplos/
│   └── curriculo.pdf                     # Currículo fictício para testar a importação
├── DESENVOLVIMENTO.md
└── README.md
```

---

## Pré-requisitos

- [.NET SDK 10](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) 20.19 ou superior (desenvolvido com a 24.13)
- Angular CLI 21: `npm install -g @angular/cli`
- SQL Server (Express, Developer ou LocalDB)
- Ferramenta de migrations do EF Core: `dotnet tool install --global dotnet-ef`

---

## 1. Configurar a conexão com o SQL Server

A connection string **não fica no repositório**. O arquivo `appsettings.example.json` mostra o formato esperado:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.\\SQLEXPRESS;Database=CadastroCurriculos;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

Ajuste o `Server=` para a sua instância. Exemplos: `.\SQLEXPRESS` (Express), `localhost` (instância padrão), `(localdb)\MSSQLLocalDB` (LocalDB). Para autenticação SQL, troque `Trusted_Connection=True` por `User Id=...;Password=...`.

Escolha **uma** das formas de configurar:

**Opção A: User Secrets (recomendada)**, na pasta da API:

```bash
cd Backend/Cadastro_Curriculos/Cadastro_Curriculos
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=.\SQLEXPRESS;Database=CadastroCurriculos;Trusted_Connection=True;TrustServerCertificate=True"
```

**Opção B: arquivo local**, copiando o exemplo para `appsettings.Development.json` na mesma pasta e ajustando a string.

**Opção C: variável de ambiente** `ConnectionStrings__DefaultConnection`.

Se a connection string não for encontrada, a API não inicia e mostra uma mensagem indicando o problema.

**Chave do login (JWT).** A API assina os tokens com a configuração `Jwt:Chave` (mínimo de 32 caracteres), que também **não fica no repositório** e sem a qual a API não inicia. No desenvolvimento ela já está em `appsettings.Development.json` (valor só para uso local). Para trocar, ou em produção, use User Secrets (`dotnet user-secrets set "Jwt:Chave" "<texto longo e aleatório>"`) ou a variável de ambiente `Jwt__Chave`. No Azure, crie a configuração `Jwt__Chave` no App Service.

---

## 2. Criar a estrutura do banco

O banco `CadastroCurriculos` é criado automaticamente pelas migrations, se ainda não existir:

```bash
cd Backend/Cadastro_Curriculos/Cadastro_Curriculos
dotnet ef database update
```

No Visual Studio, o equivalente é rodar `Update-Database` no Package Manager Console, com o projeto `Cadastro_Curriculos` selecionado.

A estrutura criada tem três tabelas, todas com data de cadastro preenchida em UTC:

- `Usuarios`: contas de login (e-mail único e senha guardada como hash).
- `Candidatos`: currículos, com índice único no e-mail e `UsuarioId` opcional (único quando preenchido). Ao excluir a conta, o currículo permanece, sem dono.
- `ExperienciasProfissionais`: relação 1:N com `Candidatos`, com exclusão em cascata.

---

## 3. Executar a aplicação

São dois terminais: um para a API e outro para o frontend.

**Backend (API):**

```bash
cd Backend/Cadastro_Curriculos/Cadastro_Curriculos
dotnet run --launch-profile http
```

A API fica disponível em `http://localhost:5006`.

**Frontend:**

```bash
cd Frontend/cadastro-curriculos
npm install
ng serve
```

Acesse `http://localhost:4200`.

Se a API rodar em outra porta, ajuste `apiUrl` em `Frontend/cadastro-curriculos/src/environments/environment.development.ts`, que é o arquivo usado pelo `ng serve`. O `environment.ts` contém o endereço da API publicada e é usado no build de produção. Se o frontend rodar em outra origem, acrescente-a em `Cors:Origens` no `appsettings` da API.

Para testar a importação, use o currículo fictício em `exemplos/curriculo.pdf`.

---

## 4. Rodar os testes

**Backend** (unitários e de integração; os de integração sobem a API em memória com SQLite, sem precisar do SQL Server):

```bash
cd Backend/Cadastro_Curriculos
dotnet test
```

**Frontend, testes unitários** (Vitest):

```bash
cd Frontend/cadastro-curriculos
npm test -- --watch=false
```

**Frontend, ponta a ponta** (Playwright, com navegador real). Exige a **API rodando** em `http://localhost:5006` e o banco criado; o Playwright sobe o `ng serve` sozinho. Os cadastros de teste usam e-mails únicos. As capturas de tela ficam em `e2e-evidencias/`.

```bash
cd Frontend/cadastro-curriculos
npx playwright install chromium
npx playwright test
```

Os testes automatizados do backend cobrem:

- **CurriculoExtratorTests:** identificação de nome, e-mail, telefone, área de interesse e resumo, incluindo casos de confusão (título de seção antes do nome, linhas de contato, CPF parecido com telefone) e o corte do resumo no limite de 2000 caracteres.
- **CandidatoRequestValidacaoTests:** campos obrigatórios, tamanho máximo e formato do e-mail, conferindo as mensagens.
- **LeitorPdfTests:** validação do arquivo (ausente, vazio, acima de 5 MB, extensão errada, arquivo renomeado) e tratamento de PDF corrompido.
- **ExperienciaRequest, CandidatoRequest (experiências) e RegistrarRequest:** obrigatórios, tamanhos, término anterior ao início, limite de 10 experiências, e-mail e senha da conta.
- **Candidato (domínio), GeradorDeToken e ErrosDeBanco:** regra de quem é o dono, conteúdo e validade do token, e reconhecimento da violação de índice único.
- **Integração (`AuthApiTests` e `CandidatosApiTests`):** cadastro e login (201, 400, 401, 409), token adulterado e expirado, cadastro com e sem login, vínculo ao usuário, um currículo por conta, `PodeEditar` por consulta, edição (200, 400, 401, 403, 404, 409) substituindo as experiências, exclusão (204, 401, 403, 404) com cascata e novo cadastro depois de excluir.

Os testes do frontend cobrem validação, mensagens de erro, montagem do formulário e do FormArray, serviço de sessão, interceptor, tela de entrar/criar conta, componente de campos e tela de detalhes (editar e excluir). Os de ponta a ponta percorrem os fluxos CT-F01 a CT-F15: cadastro manual e com experiências, validações, conta e login, edição, exclusão e bloqueio de cadastros de outras pessoas.

Para testes manuais da API, o arquivo `Cadastro_Curriculos.http` pode ser executado pelo Visual Studio ou pelo VS Code (extensão REST Client). O upload pode ser testado com curl:

```bash
curl -i -F "arquivo=@exemplos/curriculo.pdf" http://localhost:5006/api/curriculos/extrair
```

No PowerShell, use `curl.exe` em vez de `curl`.

---

## Publicação (Azure)

A aplicação também está publicada no Azure:

| Parte | Serviço |
|---|---|
| Frontend | Azure Static Web Apps |
| API | Azure App Service (Windows, .NET 10) |
| Banco | Azure SQL Database |

- **Chave do login:** a configuração `Jwt__Chave` precisa existir no App Service, senão a API não inicia.
- **Deploy contínuo:** a cada push na branch `main`, o GitHub Actions publica o frontend e, quando há mudanças no backend, roda todos os testes antes de publicar a API. Um teste quebrado impede o deploy.
- **Configuração sem credenciais no repositório:** em produção, a connection string fica nas configurações do App Service, e a origem do frontend é liberada no CORS pela configuração `Cors__Origens__0`.
- **Rotas do Angular:** o arquivo `public/staticwebapp.config.json` redireciona as rotas para o `index.html`, para que links diretos e o recarregamento das páginas funcionem.

---

## Endpoints da API

| Método | Rota | Descrição | Respostas |
|---|---|---|---|
| GET | `/api/candidatos` | Lista os candidatos, mais recentes primeiro | 200 |
| GET | `/api/candidatos/{id}` | Detalhes de um candidato, com experiências e `podeEditar` | 200, 404 |
| GET | `/api/candidatos/meu` | Currículo do usuário logado | 200, 401, 404 |
| POST | `/api/candidatos` | Cadastra um candidato (com ou sem login; logado, passa a ser o dono) | 201, 400, 409 |
| PUT | `/api/candidatos/{id}` | Atualiza o cadastro (somente o dono) | 200, 400, 401, 403, 404, 409 |
| DELETE | `/api/candidatos/{id}` | Exclui o cadastro e suas experiências (somente o dono) | 204, 401, 403, 404 |
| POST | `/api/auth/registrar` | Cria a conta e já devolve o token | 201, 400, 409 |
| POST | `/api/auth/login` | Entra e devolve o token (enviado depois como `Authorization: Bearer`) | 200, 400, 401 |
| POST | `/api/curriculos/extrair` | Lê o PDF (campo `arquivo`, multipart) e devolve os dados encontrados, sem salvar | 200, 400, 413, 422 |

Todos os erros seguem o formato ProblemDetails (RFC 9457). Erros inesperados retornam 500, e falhas de conexão com o banco retornam 503.

---

## Validações

| Campo | Regra |
|---|---|
| Nome completo | Obrigatório (não aceita só espaços), até 150 caracteres |
| E-mail | Obrigatório, formato válido, até 254 caracteres, único no cadastro |
| Telefone | Opcional, até 20 caracteres |
| Área ou cargo de interesse | Opcional, até 100 caracteres |
| Resumo profissional | Opcional, até 2000 caracteres |
| Arquivo | Opcional; PDF de até 5 MB, conferido pela extensão e pela assinatura `%PDF-` |
| Experiências | Opcionais, até 10. Empresa (até 150) e cargo (até 100) obrigatórios; início obrigatório; término vazio significa emprego atual e não pode ser anterior ao início; descrição até 1000 |
| Conta | E-mail válido; senha de 8 a 100 caracteres |

As mesmas regras são aplicadas no frontend, para resposta imediata, e no backend, que é a garantia final.

---

## Limitações conhecidas

- O nome é identificado por heurística: se um cargo aparecer na primeira linha do currículo, pode ser confundido com o nome.
- Havendo mais de um e-mail ou telefone, só o primeiro é capturado.
- Telefones fora do padrão brasileiro não são reconhecidos.
- Área de interesse e resumo dependem de títulos de seção conhecidos, como "Objetivo", "Cargo pretendido", "Resumo profissional", "Perfil" ou "Sobre mim". Seções com outros nomes não são reconhecidas.
- O resumo vai até o próximo título conhecido; se a seção seguinte tiver um título fora da lista, parte dela pode ser incluída no resumo.
- Currículos em duas colunas podem ter o texto misturado na leitura do PDF.
- PDFs escaneados (imagem) não são lidos, porque não há OCR.
- A lista e os detalhes são públicos: qualquer pessoa com o link vê os cadastros. O login só controla quem pode **alterar** um currículo.
- Cadastros feitos sem login não têm dono e não podem ser editados nem excluídos.
- O token JWT fica no `localStorage` do navegador (simples, mas exposto a XSS) e dura 2 horas; não há renovação, refresh token, recuperação de senha nem confirmação de e-mail.
- Não há limite de tentativas de login (rate limiting).

Em todos os casos, o formulário permite completar ou corrigir os dados antes de salvar. Detalhes e melhorias planejadas estão no [DESENVOLVIMENTO.md](DESENVOLVIMENTO.md).
