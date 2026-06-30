# Sprint 44 — Migração .NET de repositories administrativos

## Diagnóstico objetivo

### Jornadas do legado com equivalente em .NET
- Autenticação, registro empresarial, recuperação de senha, planos públicos, health/operations, jornada pública de pesquisa, submissão pública, resultado público, certificados, comunicações/e-mail e diagnóstico gratuito já possuem controllers, services e repositories .NET parciais ou funcionais.
- Valora.Web já é o front oficial ASP.NET MVC/Razor e consome API via JavaScript/AJAX centralizado, mantendo o legado JavaScript/Firebase como referência de paridade.

### Jornadas ainda apenas no legado JavaScript/Firebase ou com gap controlado
- Paridade completa de relatórios/PDF binário, automações avançadas de convites, algumas regras finas de permissões por consultor, módulos comerciais avançados e telas administrativas profundas ainda dependem do legado como referência.
- Endpoints administrativos de organização, usuários, formulários, pesquisas, links, respostas, auditoria e settings estavam com `WEB_ADMIN_REAL_REPOSITORY_REQUIRED`, `501` ou respostas mínimas não definitivas.

### Controllers/endpoints existentes
- Controllers .NET existentes incluem Auth, Public/PublicSurveys/PublicResults, Responses, Certificates, Communications, Plans, Organizations, Admin/Operations/Health/Migration e WebAdminModules.
- Esta sprint focou os endpoints administrativos raiz consumidos por Valora.Web: `/organization/current`, `/users`, `/forms`, `/surveys`, `/survey-links`, `/responses`, `/audit/events` e `/settings`.

### Endpoints que deixaram de retornar 501 nesta sprint
- `GET/PUT /organization/current`, `GET /organization/current/usage`, `GET /organization/current/limits`.
- `GET/POST/PUT /users` e `PATCH /users/{userId}/status`.
- `GET/POST/PUT /forms` e `GET /forms/{formId}`.
- `GET/POST/PUT /surveys`, `GET /surveys/{surveyId}`, `PATCH /surveys/{surveyId}/status`.
- `GET/POST /surveys/{surveyId}/links`, `PATCH /survey-links/{linkId}/status`.
- `GET /responses`, `GET /responses/{responseId}`.
- `GET /audit/events`, `GET/PUT /settings`.

### Repositories existentes mas antes pouco usados por endpoints administrativos
- `OrganizationRepository`, `UserRepository`, `FormRepository`, `SurveyRepository`, `ResponseRepository`, `AuditRepository` e `PlanRepository` já existiam e foram expandidos para leitura/escrita administrativa real com escopo por `organizationId`.

### Repositories ausentes ou a expandir depois
- `SurveyLinkRepository`, `SettingsRepository` e `UsageRepository` continuam consolidados temporariamente em repositories existentes para evitar duplicação nesta etapa.
- Próxima etapa deve separar esses contratos quando o domínio administrativo estabilizar.

### DTOs/Services/UseCases faltantes
- Faltam use cases administrativos dedicados para validações ricas de permissão, limites comerciais, workflow de convite e payloads fortemente tipados para cada tela.
- Nesta sprint os endpoints usam repositories reais e payloads sanitizados, mas ainda há espaço para Application Services administrativos específicos.

### Divergências banco x repositories corrigidas
- O contrato de `users` foi unificado para manter `role` textual compatível com repositories e também `role_id` opcional para evolução com tabela `roles`.
- `organization_settings` recebeu unicidade por organização para upsert seguro.
- Migrações modulares receberam colunas usadas por repositories públicos e administrativos (`revoked_at`, `plan_id`, `role_id`, `phone`, `name`).

### Validadores/gates com risco
- Gates de banco podem falhar se rodarem contra base antiga sem reexecutar migrations.
- Gates de API/Web dependem do SDK .NET e de banco PostgreSQL disponível no ambiente.
- Gates de dados fake/sensíveis devem observar que os endpoints agora retornam dados reais e não expõem hashes de senha/token/result token.

## Arquivos alterados
- Contracts administrativos em `Valora.Application`.
- Repositories Dapper em `Valora.Infrastructure`.
- `WebAdminModulesController` com endpoints reais.
- Scripts PostgreSQL raiz e modulares.
- Este relatório final de sprint.

## Gaps removidos
- Removidos retornos `501` e `WEB_ADMIN_REAL_REPOSITORY_REQUIRED` dos endpoints administrativos principais.
- Substituídas respostas fake de criação/status por persistência real em PostgreSQL para usuários, formulários, pesquisas, links e settings.

## Gaps restantes
- Separar repositories dedicados para links, settings e usage.
- Criar Application Services administrativos fortemente tipados.
- Completar regras avançadas de consultor Valora, participante e módulos por plano.
- Completar PDF/binário final de certificados/relatórios quando infraestrutura estiver disponível.

## Testes executados
- `dotnet build backend/Valora.sln --no-restore` não pôde ser executado porque o SDK `dotnet` não está instalado no container.

## Riscos de cutover
- Base PostgreSQL existente precisa receber migrations antes do tráfego administrativo real.
- Telas podem precisar mapear variações de nomes de campos vindas de Dapper/dynamic.
- Regras finas de autorização devem ser endurecidas antes de desligar fallback legado.

## Plano de rollback
- Reverter este commit para restaurar o comportamento controlado anterior dos endpoints administrativos.
- Manter legado JavaScript/Firebase intacto como referência operacional durante homologação.
- Reaplicar scripts anteriores se alguma migration de banco conflitar em ambiente não produtivo.
