# Sprint 01 — Plano da Fundação .NET v2

## 1. Decisão
Criar uma nova base paralela em `backend-v2/ValoraPesquisa.sln`.

## 2. Motivo técnico
`backend/Valora.sln` já contém muitos módulos de migração, controllers legados/parciais, contratos de paridade, comunicação, certificados e gaps controlados. A base é útil como referência, mas misturar a fundação mínima desta sprint nela aumentaria risco de regressão no legado ASP.NET/Web já existente e dificultaria um corte gradual limpo.

## 3. Estrutura planejada
`Domain`, `Application`, `Infrastructure`, `Api`, `Web`, `Tests` e scripts PostgreSQL versionados em `backend-v2/database/postgresql`.

## 4. Fluxo vertical
Organização → Usuário → Login JWT → Formulário → Pesquisa publicada → Link público → Resposta pública → Cálculo de resultado → Consulta de resultado.

## 5. Entidades mínimas
Organization, User, Form, Question, QuestionOption, Survey, SurveyLink, SurveyResponse, ResponseAnswer, ResultScore e AuditLog.

## 6. Endpoints mínimos
Auth (`/auth/login`, `/auth/logout`, `/me`), organizações, usuários, formulários, pesquisas, links, público, resultados e auditoria.

## 7. Telas mínimas
Home, login, perfil, dashboard, organizações, usuários, formulários, pesquisas, links, respostas, auditoria, responder pesquisa e resultado público.

## 8. Regras do legado reaproveitadas
Perfis `admin_valora`, `empresa_admin`, `gestor_pesquisa`, `participante`; isolamento multiempresa; links públicos com token; níveis Inicial/Intermediário/Avançado; Bootstrap 5 e jQuery/AJAX no front MVC.

## 9. Fora desta sprint
Migração completa Firebase, e-mail/SMTP real, certificados, planos avançados, relatórios ricos, WhatsApp, LGPD completa, convites em lote e importação de dados.

## 10. Riscos técnicos
Ambiente sem SDK .NET local pode impedir build/test no container; seed BCrypt em SQL exige hash pré-gerado; endpoints usam Dapper e dependem de PostgreSQL configurado em runtime.
