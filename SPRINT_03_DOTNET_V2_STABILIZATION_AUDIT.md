# Sprint 03 — Auditoria final da estabilização .NET v2

## 1. Resumo
A Sprint 03 estabilizou a fundação `backend-v2` sem criar planos, módulos comerciais, financeiro, SMTP, WhatsApp, certificados ricos, LGPD completa ou importação total Firebase. O foco foi organizar a base .NET v2 para evolução: controllers/repositories/DTOs separados, services/use cases mínimos, Web MVC funcional, Docker Compose, scripts, README, seed local, testes e validador automatizado.

## 2. Diagnóstico inicial
O diagnóstico foi registrado em `SPRINT_03_DOTNET_V2_STABILIZATION_DIAGNOSTIC.md`, incluindo a existência prévia deste arquivo de auditoria, limitações do ambiente sem SDK, concentração anterior de arquivos, riscos Dapper/records e plano objetivo de correção.

## 3. Build executado ou motivo de não execução
`dotnet build backend-v2/ValoraPesquisa.sln` foi tentado em 2026-07-01, mas o container retornou `dotnet: command not found`. O build real deve ser executado em ambiente com .NET SDK 8. Scripts e README documentam o caminho de execução.

## 4. Testes executados ou motivo de não execução
`dotnet test backend-v2/ValoraPesquisa.sln` não pôde rodar pelo mesmo motivo ambiental: ausência do SDK. O validador automatizado Node foi executado com sucesso via `npm run backend-v2:validate`.

## 5. Controllers separados
Os controllers da API foram separados por responsabilidade em `ApiBase.cs`, `OrganizationsController.cs`, `UsersController.cs`, `FormsController.cs`, `SurveysController.cs`, `SurveyLinksController.cs`, `PublicSurveysController.cs`, `PublicResultsController.cs`, `ResponsesController.cs` e `AuditController.cs`. `FoundationControllers.cs` permanece apenas como marcador de compatibilidade da sprint.

## 6. Repositories separados
Os repositories Dapper foram separados em `Scope.cs`, `AuditService.cs`, `OrganizationRepository.cs`, `UserRepository.cs`, `FormRepository.cs`, `SurveyRepository.cs`, `SurveyLinkRepository.cs` e `ResponseRepository.cs`. `DapperRepositories.cs` permanece apenas como marcador.

## 7. DTOs organizados
Os DTOs foram organizados em subpastas sob `Application/DTOs` por domínio funcional: organizações, usuários, formulários, pesquisas, público, respostas, resultados e auditoria, preservando o namespace `ValoraPesquisa.Application.DTOs`.

## 8. Services criados
Foram criados use cases mínimos para criação de organização, criação de usuário, gravação de formulário, gravação de pesquisa, criação de link público, validação pública, submissão pública de resposta, consulta de resultado público e consulta de auditoria.

## 9. Web MVC melhorado
A Web MVC foi mantida em Razor/Bootstrap 5/jQuery/AJAX com `sessionStorage` para JWT nesta etapa. As telas passaram a oferecer login com mensagem/loading, dashboard, CRUD mínimo administrativo, ações de status, links públicos, respostas/auditoria seguras, renderização de pesquisa pública por tipo de pergunta e resultado público sem exibir tokens/hashes.

## 10. Docker Compose criado
Foi criado `backend-v2/docker-compose.yml` com PostgreSQL, `ValoraPesquisa.Api` e `ValoraPesquisa.Web`, além de Dockerfiles para API e Web. O compose usa banco `valorapesquisa` e aplica o SQL versionado de inicialização.

## 11. Scripts Windows/Linux criados
Foram criados scripts Windows e Linux para subir PostgreSQL, aplicar banco, rodar API, rodar Web e validar build/test/validator.

## 12. README criado
Foi criado `backend-v2/README.md` com visão geral, pré-requisitos, PostgreSQL, banco, API, Web, URLs locais, credenciais demo, testes, Docker Compose, validação manual e troubleshooting.

## 13. Seed ajustado
`scriptbd_completo.sql` contém seed idempotente com organização Valora, usuário admin Valora, organização empresa demo, usuário empresa admin demo, formulário demo, pesquisa demo publicada, perguntas e opções demo. As senhas não são gravadas em texto puro; hashes de desenvolvimento são documentados.

## 14. Testes unitários criados
A suíte em `FoundationTests.cs` cobre cálculo `scale`, `single_choice`, `multiple_choice`, `short_text`, peso, níveis Inicial/Intermediário/Avançado, token hash, BCrypt, sanitização/segurança textual, erro sem stack trace, SQL com tabelas/índices e estrutura separada.

## 15. Testes integrados criados
Foi criado `PostgresIntegrationTests.cs` como base de fluxo integrado contra PostgreSQL/API reais. Ele permanece marcado com `Skip` no ambiente padrão porque exige serviços externos. O próximo passo é ativá-lo em CI com PostgreSQL descartável.

## 16. Validador criado
Foi criado `backend-v2/tools/validate-backend-v2-foundation.js` e adicionado o script raiz `backend-v2:validate`. A execução atual passou com sucesso.

## 17. Gaps restantes
- Build e testes .NET reais ainda precisam ser executados em ambiente com SDK 8.
- Teste integrado precisa ser ativado em CI com PostgreSQL/API reais.
- Services podem absorver ainda mais lógica dos controllers nas próximas sprints.
- UX MVC pode evoluir para filtros/paginação/validações mais ricas, mantendo MVC/Razor.

## 18. Riscos
- Possíveis ajustes de compilação só aparecerão quando `dotnet build` rodar com SDK 8.
- Dapper + records pode exigir aliases adicionais em runtime, dependendo do mapeamento retornado pelo Npgsql.
- Hashes BCrypt do seed são de desenvolvimento e devem ser regenerados/substituídos se necessário para ambiente operacional.

## 19. Próximo passo recomendado
Instalar .NET SDK 8 no CI/agente, executar `dotnet build backend-v2/ValoraPesquisa.sln` e `dotnet test backend-v2/ValoraPesquisa.sln`, ativar PostgreSQL descartável para testes integrados e então iniciar a Sprint 04 — planos, módulos, limites comerciais, bloqueio por assinatura, dashboard real e permissões avançadas.
