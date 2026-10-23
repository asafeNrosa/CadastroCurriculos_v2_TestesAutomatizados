# Relato do desenvolvimento

Este documento descreve como organizei o trabalho, as decisões técnicas, o uso de inteligência artificial, o que precisei corrigir, como verifiquei a solução, o tempo dedicado e as limitações e melhorias que faria com mais tempo.

---

## 1. Organização e execução

Priorizei uma entrega funcional e completa, como o próprio desafio sugere. O plano foi dividido em duas frentes:

1. **Backend completo e testado primeiro:** modelagem do banco, API de candidatos, extração de dados do PDF e testes automatizados.
2. **Depois, frontend, integração e documentação:** wireframe, telas em Angular, ajustes de integração, README e este relato.
3. **Depois:** com a entrega principal quase pronta, implementei a extração de área de interesse e resumo profissional a partir do PDF.
4. **Por último, a publicação no Azure:** banco, API e frontend publicados, com deploy automático pelo GitHub Actions.

Cada etapa terminava com testes manuais e um commit, para que o histórico mostrasse a evolução do trabalho. Durante o desenvolvimento, mantive anotações das etapas, que depois comparei com o que foi executado para montar este documento.

Ferramentas: Visual Studio 2026 (backend), VS Code (frontend), SQL Server Management Studio, Git e GitHub, arquivo `.http` do Visual Studio e `curl` para testar a API, portal do Azure e GitHub Actions para a publicação.

---

## 2. Principais decisões técnicas

### Banco de dados

- **Uma única tabela `Candidatos`.** Minha primeira ideia foi criar uma tabela para cada campo do formulário. Revisei a decisão: os campos são atributos de um único candidato, com um valor cada, e a tabela única já está normalizada. Separar exigiria vários INSERTs e JOINs sem nenhum ganho.
- **Índice único no e-mail**, para impedir cadastros duplicados. A regra fica no banco porque ele é o único ponto por onde todas as gravações passam, inclusive as simultâneas.
- **Data de cadastro preenchida pelo próprio banco**, em UTC (`SYSUTCDATETIME()`).
- **Telefone como texto**, para preservar a formatação.
- **O PDF não é armazenado:** ele só é lido para preencher o formulário.
- **Estrutura criada com migrations do EF Core** (Code First), versionadas junto com o código.

### Backend (ASP.NET Core)

- **DTOs separados da entidade.** O `CandidatoRequest` não aceita `Id` nem `DataCadastro`, e o `CandidatoResponse` define exatamente o que a API devolve.
- **Validação com Data Annotations** e mensagens em português, aplicadas automaticamente pelo `[ApiController]`. O e-mail é validado por uma expressão regular, porque o `[EmailAddress]` do .NET aceita endereços como `joao@empresa`.
- **Erros no formato ProblemDetails**, com códigos HTTP consistentes: 400, 404, 409, 413, 422, 500 e 503.
- **Tratamento de erros em dois níveis:** try/catch apenas onde o erro é esperado e tratável (e-mail duplicado, filtrando as violações 2601 e 2627 do SQL Server, e falha na leitura do PDF), e um tratador global (`IExceptionHandler`) para os erros inesperados. Detalhes técnicos vão só para o log, nunca para o usuário.
- **E-mail duplicado tratado pela violação do índice**, sem consulta prévia, para evitar condição de corrida entre requisições simultâneas.
- **Leitura e extração separadas.** O `LeitorPdf` cuida do arquivo, e o `CurriculoExtrator` cuida do texto. Assim o extrator é testado apenas com strings, sem precisar de PDFs.
- **Validação do arquivo em camadas:** presença, tamanho até 5 MB, extensão e assinatura `%PDF-` nos primeiros bytes. A assinatura impede que um arquivo renomeado seja aceito.
- **Extração de nome, e-mail e telefone:** e-mail e telefone por expressões regulares. O nome é identificado por heurística: a primeira linha, entre as dez primeiras, só com letras e com pelo menos duas palavras, ignorando títulos como "Currículo" e títulos de seção como "Dados Pessoais".
- **Extração de área de interesse e resumo por títulos de seção:** o extrator reconhece títulos conhecidos, comparando o texto sem acentos e em minúsculas. A área vem do texto depois de títulos como "Objetivo" ou "Cargo pretendido" (na mesma linha, após os dois-pontos, ou na linha seguinte). O resumo reúne as linhas depois de títulos como "Resumo profissional", "Perfil" ou "Sobre mim", até o próximo título conhecido. Os dois respeitam os limites do banco (100 e 2000 caracteres), cortando entre palavras.
- **Heurística em vez de IA.** Comparei quatro caminhos: heurística, IA via API, uma solução híbrida e IA rodando localmente. Escolhi a heurística porque não exige chave de API nem configuração para quem avalia, não envia dados pessoais de candidatos a terceiros e pode ser testada de forma determinística. A IA ficou registrada como melhoria.
- **O endpoint de extração não salva nada.** Os dois caminhos de cadastro terminam no mesmo `POST /api/candidatos`, com as mesmas validações.
- **Configuração sem credenciais no repositório:** connection string no User Secrets, com autenticação do Windows, e um `appsettings.example.json` como modelo.
- **Redirecionamento HTTPS apenas fora do ambiente de desenvolvimento**, porque o navegador não aceita redirecionamentos na verificação de CORS (preflight), o que quebraria o upload pelo frontend.

### Frontend (Angular)

- **Wireframe próprio como base**, com ajustes antes de codar: rótulos acima dos campos (o placeholder desapareceria quando o PDF preenchesse o formulário), botões Salvar e Cancelar, área de mensagens, dados da tela de detalhes como texto e um cabeçalho de navegação.
- **Bootstrap customizado via Sass**, com uma paleta de cores vivas e contraste adequado para leitura. Campos preenchidos pelo PDF ficam destacados em ciano, para o usuário saber o que conferir.
- **Um único formulário (Reactive Forms) para os dois caminhos.** O PDF apenas preenche os campos encontrados (nome, e-mail, telefone, área e resumo), que podem ser corrigidos antes de salvar.
- **Mesmas regras de validação do backend:** a mesma expressão regular de e-mail e um validador de obrigatório que, como o `[Required]` do .NET, recusa textos só com espaços.
- **O PDF nunca bloqueia o cadastro manual.** Arquivo inválido, falha de leitura ou API indisponível geram apenas uma mensagem.
- **Erros da API ligados aos campos:** o 409 aparece no campo e-mail, e os erros de validação do backend (400) aparecem em cada campo correspondente.
- **Estado das telas em signals**, necessário no Angular 21, que funciona sem Zone.js por padrão.
- **Todas as chamadas HTTP num único serviço** (`CandidatoService`) e o tratamento de mensagens de erro numa função compartilhada.

### Publicação no Azure

- **Arquitetura:** frontend no Azure Static Web Apps (plano Free), API no Azure App Service (Windows, .NET 10) e banco no Azure SQL Database (edição Básica).
- **Recursos reaproveitados:** o servidor SQL e o plano do App Service de um projeto anterior, o que evitou custo adicional na conta Azure for Students.
- **Banco copiado com o assistente do SSMS**, levando a estrutura e a tabela de histórico das migrations.
- **Nenhuma credencial no repositório:** a connection string fica nas "Cadeias de conexão" do App Service, que o ASP.NET Core lê como `ConnectionStrings:DefaultConnection` sem nenhuma mudança no código. A origem do frontend é liberada no CORS pela configuração `Cors__Origens__0`.
- **Deploy contínuo:** a API é publicada pelo GitHub Actions a cada push que altera o backend. O workflow roda todos os testes antes, e um teste quebrado impede o deploy. O frontend é publicado pelo workflow do Static Web Apps.
- **Ajustes no frontend:** `environment.ts` de produção apontando para a API publicada; `staticwebapp.config.json` redirecionando as rotas do Angular para o `index.html`, para que links diretos e o recarregamento das páginas funcionem; versão mínima do Node declarada no `package.json` para a compilação no Azure.

---

## 3. Ferramentas de IA

Utilizei apenas o **Claude (Anthropic), modelo Claude Opus 5.5**, pela interface do claude.ai. Nenhuma outra ferramenta de IA foi usada.

---

## 4. Como a IA participou

**Todas as ações e decisões sobre o código foram tomadas por mim**, seguindo boas práticas de desenvolvimento e com o auxílio do Claude. A IA funcionou como uma **assistente**, e não como uma desenvolvedora que entrega o projeto pronto: eu a usei para tirar dúvidas, entender conceitos, comparar alternativas e receber ajuda na implementação. Em cada etapa, avaliei as propostas, escolhi o caminho, executei, revisei o código e conduzi os testes.

Exemplos de pedidos e de como as respostas foram aproveitadas:

| Pedido | Como usei a resposta |
|---|---|
| Montar um plano de construção para executar em poucos dias | Adotei o plano por blocos, começando pelo backend. |
| Modelar o banco com uma tabela para cada campo do formulário | A IA argumentou contra; revisei a ideia e optei pela tabela única. |
| Como configurar a connection string de um banco que ainda não existe | Entendi que o EF cria o banco e montei a string a partir da minha instância do SQL Server. |
| Usar try/catch sempre que possível | Discutimos e adotei try/catch pontual com um tratador global. |
| Diagnóstico de erros colados do terminal (git, curl, redirecionamento 307) | Identifiquei as causas e apliquei as correções. |
| Revisão do meu wireframe, com cores vivas e de fácil leitura | Aceitei os ajustes de usabilidade e a paleta proposta. |
| Qual a complexidade de extrair área de interesse e resumo do PDF | Primeiro priorizei a entrega completa. Depois, comparei as opções em detalhe (comportamento com diferentes currículos, critérios do desafio, tempo) e escolhi a heurística por títulos de seção. |
| Como publicar a aplicação no Azure | Segui o caminho que já tinha usado em outro projeto, reaproveitando o servidor SQL e o plano do App Service, e optei pelo deploy da API via GitHub Actions, com os testes rodando antes. |
| Diagnóstico do erro de conexão do frontend publicado | Pela aba Rede do navegador, identifiquei a falta do cabeçalho de CORS e o nome da configuração que não batia com o código. |
| Ajuste e correção de código (entidade, DbContext, DTOs, controllers, serviços, testes, telas) | Revisei, adaptei ao meu projeto e compilei cada parte, pedindo a explicação de cada trecho. |

---

## 5. O que precisei corrigir, adaptar ou descartar

**Sugestões da IA que precisei corrigir ou adaptar:**

- As instruções iniciais eram por linha de comando; adaptei para o Visual Studio, que era a ferramenta que eu estava usando.
- O namespace do código sugerido não era o do meu projeto; ajustei para `Cadastro_Curriculos`.
- Um erro do Git ("O pipe foi finalizado") foi diagnosticado inicialmente como falha de conexão com o banco. A causa real estava no Git do Visual Studio, e descartei as sugestões de conexão.
- O conteúdo sugerido para o arquivo `.http` apagava a variável com o endereço da API; corrigi.

**Problemas do ambiente que resolvi:**

- O repositório estava sem `.gitignore`, e as pastas `bin` e `obj` tinham sido commitadas. Criei o `.gitignore` do .NET e removi as pastas com `git rm --cached`.
- O `curl` do PowerShell é um apelido de outro comando; passei a usar `curl.exe`.
- O Bootstrap foi instalado por engano fora da pasta do projeto Angular. Identifiquei pela data dos arquivos e corrigi antes do commit.
- Colei o HTML da tela inicial no arquivo de estilos; os erros do editor mostraram o problema.
- A data de cadastro aparecia 3 horas adiantada, porque o valor UTC chegava ao frontend sem indicação de fuso. Corrigi no backend com `DateTime.SpecifyKind`.
- Um push foi rejeitado porque eu tinha editado um arquivo direto no GitHub. Resolvi com `git pull --rebase`, sem push forçado.

**Problemas resolvidos na publicação:**

- O endereço da API não era o nome que escolhi: o App Service acrescenta um código único e a região ao domínio padrão. Passei a usar o domínio indicado no portal.
- O firewall do Azure SQL bloqueou o meu IP na cópia do banco; liberei o acesso pelo portal.
- A primeira execução do workflow gerado pelo Azure falhou, porque tentava compilar a raiz do repositório. Ajustei para apontar para o projeto da API e para rodar os testes antes do deploy.
- O banco foi copiado depois da limpeza dos dados de teste e chegou vazio ao Azure. O primeiro cadastro foi feito pelo próprio site publicado, servindo de teste final do fluxo completo.
- O frontend publicado não conseguia acessar a API. Pela aba Rede do navegador, vi que a API respondia 200, mas sem o cabeçalho `Access-Control-Allow-Origin`: a variável de ambiente estava como `Cors__Origins__0`, e o código lê `Cors:Origens`. Corrigi o nome.
- O build de produção do Angular avisou que o pacote inicial passava do orçamento de 500 kB, por causa do Bootstrap completo (227 kB de CSS). Como o tráfego real, comprimido, é de cerca de 106 kB, tratei o aviso como aceitável.
- Corrigi a grafia de um nome cadastrado direto no Azure SQL, com um `UPDATE` dentro de uma transação, já que a aplicação ainda não tem tela de edição.

**Descartado:** a tabela por campo, o try/catch em todos os métodos e, por enquanto, o uso de IA para a extração.

---

## 6. Como verifiquei a solução

- **Revisão do que foi gerado:** conferi a migration antes de aplicá-la e a estrutura criada no SQL Server Management Studio (tabela, índice único e valor padrão).
- **Testes automatizados (xUnit)**, todos aprovados:
  - `CurriculoExtratorTests`: extração com texto completo, texto vazio, ausência de telefone, título antes do nome, linhas de contato, e-mail em maiúsculas, cinco formatos de telefone, CPF que não pode ser confundido com telefone, área na mesma linha e na linha seguinte (com título em maiúsculas e acentuado), resumo em várias linhas parando no próximo título, currículo sem títulos de seção, resumo acima de 2000 caracteres e título de seção antes do nome.
  - `CandidatoRequestValidacaoTests`: campos obrigatórios, tamanho máximo e formatos inválidos de e-mail, conferindo o texto das mensagens.
  - `LeitorPdfTests`: arquivo ausente, vazio, acima de 5 MB, com extensão errada, renomeado para `.pdf`, válido e corrompido.
- **Testes manuais da API** com o arquivo `.http` e o `curl`: cadastro (201), e-mail duplicado (409), dados inválidos (400), detalhes (200), id inexistente (404), banco desligado (503) e uploads com PDF válido, arquivo falso, requisição sem arquivo e PDF escaneado.
- **Testes manuais do frontend**, com backend e frontend rodando juntos:
  - Formulário: salvar vazio, e-mail inválido, nome só com espaços, cadastro válido, e-mail duplicado, importação do currículo fictício (os cinco campos preenchidos), currículo sem as seções de área e resumo (aviso dos campos não encontrados), arquivo falso, imagem e API desligada.
  - Lista e detalhes: hora de cadastro correta, navegação para os detalhes, aviso de cadastro salvo, id inexistente, id inválido, API desligada e resumo com várias linhas.

- **Testes em produção (Azure):** lista da API pelo navegador, extração com `curl` no endereço publicado, cadastro completo pelo site a partir do PDF e hora de cadastro correta na lista.
- **Integração contínua:** a cada push no backend, o GitHub Actions roda todos os testes automatizados antes do deploy.

Na primeira versão, os testes automatizados ficaram concentrados no backend e o frontend foi verificado manualmente. A segunda etapa (seção 9) acrescentou testes de integração da API, testes unitários do frontend e testes ponta a ponta com Playwright.

---

## 7. Tempo dedicado

Aproximadamente **16 horas**: cerca de **8 horas no backend**, **4 horas no frontend** e **4 horas** na extração de área e resumo e na publicação no Azure.

---

## 8. Dificuldades, limitações e melhorias

### Dificuldades

As maiores dificuldades não estiveram nas tomadas de decisão, mas em **executar corretamente** cada decisão tomada. O melhor exemplo é a leitura do PDF para preencher o formulário: apesar de o PdfPig ser uma biblioteca simples, eu nunca tinha feito esse tipo de extração. Precisei entender como o texto sai do PDF, por que as quebras de linha importam para encontrar o nome e como validar o arquivo de forma segura.

### Limitações conhecidas

- O nome é identificado por heurística: se um cargo aparecer na primeira linha do currículo, pode ser confundido com o nome.
- Havendo mais de um e-mail ou telefone, só o primeiro é capturado.
- Telefones fora do padrão brasileiro não são reconhecidos.
- Área de interesse e resumo dependem de títulos de seção conhecidos; seções com outros nomes não são reconhecidas.
- O resumo vai até o próximo título conhecido; se a seção seguinte tiver um título fora da lista, parte dela pode entrar no resumo.
- Currículos em duas colunas podem ter o texto misturado na leitura do PDF.
- PDFs escaneados (imagem) não são lidos, porque não há OCR.
- A lista e os detalhes continuam públicos; o login só controla quem pode alterar um currículo.
- Cadastros feitos sem login não têm dono e não podem ser editados nem excluídos.
- O token JWT fica no `localStorage` (exposto a XSS), dura 2 horas e não há renovação, recuperação de senha, confirmação de e-mail nem limite de tentativas de login.

Em todos os casos, o formulário permite completar ou corrigir os dados antes de salvar.

### Melhorias que faria com mais tempo

- **Extração híbrida com IA:** manter a heurística como padrão e, quando houver uma chave configurada, enviar o texto do currículo a um modelo de linguagem que devolva os campos estruturados, voltando à heurística em caso de falha. Isso melhoraria currículos sem títulos ou em duas colunas, mas exige tratar custo, disponibilidade do serviço e, principalmente, o envio de dados pessoais a terceiros, de acordo com a LGPD.
- **Perfil de administrador**, com acesso à lista e aos detalhes restrito a ele e exclusão de qualquer cadastro. (Contas de candidato, edição e exclusão do próprio cadastro já foram implementadas.)
- **Recuperação de senha, confirmação de e-mail e renovação do token**; guardar o token em cookie `HttpOnly`.
- **Impressão do cadastro** a partir da tela de detalhes.
- **Tabelas próprias para telefones e e-mails**, permitindo mais de um de cada por candidato (relação 1:N), como já foi feito com as experiências. O índice único passaria para a tabela de e-mails.
- **OCR** para ler currículos escaneados.
- **Paginação e busca** na listagem.
- **Teste de integração contra o SQL Server real** (hoje usam SQLite em memória) e **mais testes de componente** no frontend.
- **CSS menor**, importando apenas os componentes do Bootstrap que são usados.
- **Ajustes no CI/CD:** filtro de pastas também no workflow do frontend e atualização das actions do GitHub para as versões compatíveis com o Node.js 24.

---

## 9. Segunda etapa: contas, edição, exclusão e experiências

Depois da entrega inicial, ampliei o sistema com cinco mudanças:

1. **Experiências profissionais (1:N).** Em vez de colunas `Experiencia1`, `Experiencia2`..., criei a tabela `ExperienciasProfissionais` ligada a `Candidatos` por chave estrangeira com exclusão em cascata. No DTO, uma `List<ExperienciaRequest>`; no Angular, um `FormArray` em que cada linha é um `FormGroup`. Limite de 10 por cadastro, validado nos dois lados.
2. **Contas e login com JWT.** Tabela `Usuarios` (senha com `PasswordHasher`, nunca em texto puro), `POST /api/auth/registrar` e `/login`. O cadastro de currículo **continua anônimo**: o token é opcional e, se vier, o currículo passa a pertencer ao usuário (`Candidato.UsuarioId`, único). A chave de assinatura não fica no repositório.
3. **PUT e DELETE** só para o dono: 401 sem token, 403 para outro usuário, 404 se não existe. O PUT substitui a lista de experiências inteira. A API devolve `podeEditar`, então o frontend não precisa decidir sozinho se mostra os botões; a regra de verdade continua no backend.
4. **Editar e excluir na própria tela de detalhes**, sem página nova: o mesmo componente de campos serve ao cadastro e à edição, e a exclusão pede confirmação na própria tela.
5. **E-mail duplicado continua decidido pelo banco** (violação do índice único), agora também na edição e na criação de conta.

**Decisões e cuidados**
- Mensagem igual para e-mail inexistente e senha errada no login, para não revelar quais e-mails têm conta.
- Falha de 401 com sessão ativa derruba a sessão no frontend ("Sua sessão expirou"); o token só é enviado para a URL da API.
- Um usuário tem no máximo um currículo (índice único filtrado em `UsuarioId`); excluir a conta não apaga o currículo (SetNull).

**Testes acrescentados**
- Backend: unitários (DTOs, regra de dono, gerador de token, detecção de violação de unicidade) e de integração com `WebApplicationFactory` e SQLite em memória, cobrindo os códigos 200, 201, 204, 400, 401, 403, 404 e 409 e a cascata.
- Frontend: testes unitários com Vitest e 15 casos ponta a ponta com Playwright (CT-F01 a CT-F15), com capturas de tela como evidência.

**Como usei a IA nesta etapa:** para propor o desenho (1:N, token opcional, `podeEditar`), gerar o código base e os testes. Eu revisei as regras de acesso e as mensagens, e conferi o resultado rodando a aplicação e os testes.
