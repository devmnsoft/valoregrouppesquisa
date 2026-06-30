# Sprint XX — Novo Projeto .NET Migração Audit

## 1. Decisão arquitetural
Foi mantida a solution `backend/Valora.sln`, porque já existe separação compatível com Clean Architecture (`Domain`, `Application`, `Infrastructure`, `Api`, `Web`, `Tests`) e o legado JavaScript/Firebase permanece preservado na raiz. Criar `backend-v2` nesta etapa aumentaria duplicidade sem ganho técnico imediato.

## 2. Estrutura usada
- `backend/Valora.Domain`: entidades e objetos de domínio.
- `backend/Valora.Application`: DTOs, contratos, serviços e use cases.
- `backend/Valora.Infrastructure`: repositórios Dapper existentes.
- `backend/Valora.Api`: Web API.
- `backend/Valora.Web`: MVC/Razor oficial.
- `backend/Valora.Tests`: testes automatizados.
- `database/postgresql`: scripts PostgreSQL e `scriptbd_completo.sql`.

## 3. Estrutura criada/ajustada
- `backend/Valora.Domain/Common/AuditableEntity.cs` com campos auditáveis obrigatórios.
- `backend/Valora.Domain/Entities/MigrationDomainEntities.cs` com entidades mínimas pendentes da migração.
- `backend/Valora.Application/Results/SurveyResultCalculator.cs` com cálculo dinâmico de resultado.
- `backend/Valora.Application/Migration/LegacyImportContracts.cs` com contratos de importação legado.
- `backend/Valora.Tests/SurveyResultCalculatorTests.cs` com cobertura do cálculo dinâmico.
- `NOVO_PROJETO_DOTNET_MIGRACAO_MAPA_FUNCIONAL.md` com mapa funcional obrigatório.

## 4. Módulos migrados/mapeados
Foram mapeados clientes, financeiro, planos, módulos, usuários, funcionários, formulários, pesquisas, convites por e-mail, respostas, relatórios, certificados, planos de ação, ValoraBot, suporte, LGPD, integrações, exportações, benchmark, white label, backup e logs.

## 5. Regras do legado reaproveitadas
- Perfis e permissões de `role-definitions.js`.
- Módulos e bloqueios comerciais de `module-definitions.js`.
- Jornada pública, LGPD, link seguro e cálculo de `functions/index.js`.
- Regras documentadas em `PERFIS_E_PERMISSOES.md`, `MODULOS_E_PLANOS.md` e `README.md`.

## 6. Pendências
- Concluir repositories Dapper para todas as entidades novas.
- Expandir endpoints administrativos faltantes com persistência real.
- Implementar autenticação completa com BCrypt e refresh token persistido por hash.
- Completar importação transacional de Firestore/localStorage.
- Validar tudo em ambiente com .NET SDK instalado.

## 7. Banco criado/ajustado
O repositório já contém `database/postgresql/scriptbd_completo.sql` e scripts modulares. A etapa documentou a necessidade de expandir tabelas novas para todas as entidades auditáveis, com UUID, FKs, índices por `organization_id`, e-mail, slug, status e hashes de token.

## 8. Endpoints criados/confirmados
A solução já possui controllers para Auth, Organizations, Plans, Surveys, PublicSurveys, Responses, Certificates, Communications, Operations, Migration, Health e Admin. Nesta etapa foram criados contratos de aplicação para sustentar importação legado sem acoplar Web/Api ao Firebase.

## 9. Telas criadas/confirmadas
`Valora.Web` já contém controllers e views MVC/Razor para home, conta, dashboard, organizações, usuários, formulários, pesquisas, respostas, resultados, certificados, comunicações, auditoria, operações, planos, migração e jornada pública. Nenhum novo frontend Node/React/Vue/Angular foi criado.

## 10. Services criados
- `SurveyResultCalculator`.
- `QuestionScoreCalculator`.
- `DimensionScoreCalculator`.
- `ResultBandResolver`.
- `LegacyMappingService` inicial.

## 11. Repositories criados/confirmados
Foram confirmados repositórios Dapper existentes para Organization, User, Plan, Form, Survey, Response, Result, Certificate, Communication, Audit e Migration. A etapa adicionou interfaces de migração para padronizar expansão.

## 12. Testes criados
- `SurveyResultCalculatorTests` cobre cálculo por escala, escolha única, múltipla escolha, texto preenchido, pesos, percentuais, faixa e dimensões.

## 13. Comandos executados
- `dotnet build backend/Valora.sln` — não executou por ausência do binário `dotnet` no ambiente.
- Inspeções com `find`, `rg`, `sed`, `cat` e `git status`.

## 14. Comandos não executados e motivo
Os comandos `dotnet build`, `dotnet test`, `npm run backend:build`, `npm run backend:test`, `npm run web:build` dependem do .NET SDK, ausente neste container. Os validadores npm relacionados ao Web/API também chamam `dotnet` ou dependem da build ASP.NET, portanto foram bloqueados pelo mesmo limitador ambiental.

## 15. Riscos
- Compilação não validada localmente pela ausência do .NET SDK.
- Scripts SQL precisam ser expandidos para entidades adicionadas.
- Algumas telas/endpoints podem ainda depender de gaps controlados já documentados.
- Importação legado ainda está em contrato/preview, não em execução transacional completa.

## 16. Próximo passo recomendado
Instalar .NET SDK 8 no ambiente de CI/agente, rodar `dotnet build backend/Valora.sln` e `dotnet test backend/Valora.sln`, corrigir eventuais falhas de compilação, ampliar SQL/repositories Dapper para entidades novas e então implementar endpoints CRUD reais por módulo.
