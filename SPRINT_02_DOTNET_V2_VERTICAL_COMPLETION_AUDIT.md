# Sprint 02 — Auditoria da conclusão vertical .NET v2

## 1. Resumo da sprint
A Sprint 02 substituiu os placeholders principais do `backend-v2` por um fluxo vertical real com PostgreSQL/Dapper: Organização → Usuário → Login → Formulário → Pesquisa → Link público → Resposta pública → Resultado → Auditoria.

## 2. O que estava placeholder e virou real
- Formulários agora listam, detalham e salvam com perguntas/opções.
- Pesquisas agora listam, detalham, salvam e alteram status validando formulário.
- Links públicos agora geram token seguro e persistem somente hash.
- Validação pública valida organização, pesquisa publicada, link ativo, token, revogação e expiração.
- Resposta pública grava resposta, answers e score.
- Resultado público valida hash do result token.
- Auditoria lista eventos reais por escopo.

## 3. Endpoints implementados
- `/organizations`, `/users`, `/forms`, `/surveys`, `/surveys/{surveyId}/links`, `/survey-links/{linkId}/status`, `/public/surveys/validate`, `/public/surveys/{surveyId}/responses`, `/public/results/{responseId}`, `/responses` e `/audit/events`.

## 4. Repositories criados
- `OrganizationRepository`, `UserRepository`, `FormRepository`, `SurveyRepository`, `SurveyLinkRepository`, `ResponseRepository` e `AuditService` Dapper.

## 5. Services criados/ajustados
- Reuso do `SurveyResultCalculator` e `QuestionScoreCalculator`.
- `AuditService` passou a registrar e listar eventos com sanitização básica de metadata.
- `Sha256TokenHasher` segue como serviço de token/hash seguro.

## 6. DTOs criados
- DTOs seguros em `FoundationDtos.cs`: organizações, usuários, formulários, perguntas, opções, pesquisas, links, público, respostas, resultados e auditoria.

## 7. Tabelas/índices ajustados
- SQL idempotente recriado com schema `valorapesquisa`, tabelas do fluxo vertical e índices mínimos solicitados: usuários, organizações, forms, surveys, links, responses e audit logs.

## 8. Telas ajustadas
- Views MVC/Razor receberam marcadores funcionais e `site.js` passou a consumir a API via jQuery/AJAX, usando `sessionStorage` para JWT, mensagens amigáveis e estados vazios.

## 9. Testes criados
- Testes de cálculo por tipo/nível/peso, segurança textual contra hashes, erro sem stack trace, SQL com tabelas/índices e repositories com filtros por organização/hash.

## 10. Comandos executados
- `find .. -name AGENTS.md -print`
- `git status --short`
- `sed` nas leituras obrigatórias do backend-v2 e referências legadas.
- `rg` para verificar remoção de placeholders e exposição textual de hashes.
- `dotnet build backend-v2/ValoraPesquisa.sln`

## 11. Comandos não executados e motivo
- `dotnet test backend-v2/ValoraPesquisa.sln` não pôde ser executado porque o container não possui o binário `dotnet` instalado.

## 12. Gaps restantes
- Build/test reais dependem do .NET SDK no ambiente.
- Não houve teste integrado contra PostgreSQL real neste container.
- Algumas telas são mínimas e priorizam consumo/API/estado vazio, sem UX avançada.

## 13. Riscos
- Pode haver ajustes finos de compilação após rodar em ambiente com .NET SDK.
- Dapper com records pode exigir validação em runtime conforme convenções de mapeamento Npgsql/Dapper.
- Fluxos comerciais avançados, SMTP, WhatsApp, certificados ricos e migração completa permanecem fora de escopo.

## 14. Próximo passo recomendado
Instalar .NET SDK 8 no CI/agente, executar `dotnet build backend-v2/ValoraPesquisa.sln` e `dotnet test backend-v2/ValoraPesquisa.sln`, corrigir eventuais erros de compilação/runtime e então criar testes integrados com PostgreSQL descartável.
