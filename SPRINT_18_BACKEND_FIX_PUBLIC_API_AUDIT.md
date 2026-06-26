# Sprint 18 — Auditoria Backend, API Pública e PostgreSQL valorapesquisa

## Escopo auditado
Foram auditados os arquivos e diretórios obrigatórios: `package.json`, `config.js`, `config/config.production.js`, `runtime-capabilities.js`, `index.html`, clientes/repositories frontend, `app.js`, `pdf.js`, `backend/`, `database/postgresql/`, `migration/`, `communication-gateway/`, `scripts/`, `tools/windows/`, `Dockerfile`, `docker-compose.yml` e `docker-compose.postgres.yml`.

## Respostas objetivas
1. Controllers existentes: `AdminController`, `AdminDatabaseController`, `ArchitectureController`, `AuthController`, `CertificatesController`, `CommunicationsController`, `CoreController`, `HealthController`, `MigrationController`, `OrganizationsController`, `PlansController`, `PublicController`, `PublicResultsController`, `PublicSurveysController`, `SurveysController`.
2. Controllers vazios: nenhum após a correção; `PublicSurveysController` possui validação e submissão públicas.
3. Controllers compactados em uma linha antes da sprint: `AuthController`, `PlansController`, `OrganizationsController` e trechos de `HealthController`; foram reformatados em múltiplas linhas.
4. Controllers sem rotas oficiais: endpoints oficiais estão presentes para health, arquitetura, planos, auth, jornada pública, certificados, comunicações, migração e migrate admin.
5. `Program.cs` está legível e delega configuração a extension methods.
6. Injeções estão separadas em `AddApiServices`, `AddApplicationServices` e `AddInfrastructureServices`.
7. SQL direto em controller: apenas health check técnico `SELECT 1`, aceito como verificação operacional; controllers de negócio não contêm SQL.
8. Regra de negócio em controller: a API pública mantém fallback demo local, mas cálculo oficial está centralizado no `ValoraInsightCalculator`.
9. Services separados: existem `AuthService`, `AuditService`, `PlanEntitlementService`, `CertificateService`, `EmailJobService`, `EmailService`, calculadora e devolutiva.
10. Repositories separados: existem repositories Dapper separados para organização, usuário, plano, formulário, pesquisa, resposta, resultado, certificado, comunicação, auditoria e migração.
11. Interfaces separadas: existem interfaces próprias em `Valora.Application/Contracts`.
12. DTOs separados: existem DTOs de auth, planos e jornada pública.
13. `AuthService` usa `AuditService` corretamente após a correção.
14. Referência inválida a `valorapesquisa.LogAsync`: removida.
15. `MigrationRunner` usa somente `valorapesquisa.schema_migrations`.
16. `CREATE SCHEMA IF NOT EXISTS valora`: não encontrado nos scripts canônicos.
17. Schemas legados `valora`, `billing`, `communication`, `audit` ou `migration`: validador fortalecido falha para SQL/repositories/database/migration com esses schemas.
18. API pública oficial existe.
19. `PublicSurveysController` implementa `validate` e `responses`.
20. `PublicResultsController` implementa `result`.
21. Banco canônico usa `valorapesquisa.nome_da_tabela`.
22. Docker possui compose para API e PostgreSQL.
23. Windows fora do Docker possui scripts em `tools/windows/backend` e script consolidado de validação.
24. `DATA_PROVIDER=api` é mantido para ambiente local/controlado.
25. `DATA_PROVIDER=hybrid` mantém Firebase primário e comparação sem cutover automático.
26. Ainda há itens planejados/not-implemented em gateway de comunicação e geração completa de certificado PDF/PNG backend; não bloqueiam a API pública oficial.

## Mapeamento dos termos obrigatórios
A busca por `TODO`, `NotImplemented`, `not implemented`, `demo only`, `mock`, `stub`, `throw new Error`, `501`, `CREATE SCHEMA`, schemas, `valorapesquisa.LogAsync`, `callPublicFunction`, `firebaseCallable`, `renderTakeSurvey`, `submitSurvey` e `renderResult` foi executada com `rg` no escopo solicitado. Os principais achados foram: scripts de validação com `throw new Error`, gateway de comunicação retornando `501/not-implemented` para rotas antigas não oficiais, migrations canônicas com `CREATE SCHEMA IF NOT EXISTS valorapesquisa`, e referências de jornada pública no frontend. Não há mais `valorapesquisa.LogAsync` no backend.

## Problemas corrigidos nesta sprint
- `AuthService` deixou de chamar schema como objeto C# e passou a registrar auditoria via `AuditService`.
- Controllers compactados foram reformatados para manutenção.
- Validador de schema PostgreSQL foi atualizado para os nomes canônicos `001_create_schema_valorapesquisa.sql` a `012_seed_demo_valora_insight.sql`.
- Validador de schema único passou a verificar `database/postgresql`, `backend`, `migration` e `scripts`, ignorando falsos positivos de namespace C# `Valora.*`.

## Riscos restantes
- `dotnet` não está instalado no ambiente de execução desta auditoria, então build/test backend precisam ser executados em ambiente com SDK .NET.
- Certificado backend completo em PDF/PNG segue como evolução; endpoint PNG retorna JSON seguro.
- Envio transacional real depende de configuração SMTP/provider em backend/gateway, sem secrets no frontend.
