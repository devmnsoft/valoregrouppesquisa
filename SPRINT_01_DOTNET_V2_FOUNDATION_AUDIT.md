# Sprint 01 — Auditoria da Fundação .NET v2

## 1. Decisão arquitetural tomada
Criada base paralela `backend-v2`, preservando legado da raiz e `backend/Valora.sln`.

## 2. Estrutura criada
Solution, projetos `src/*`, testes e scripts PostgreSQL em `backend-v2/database/postgresql`.

## 3. Projetos criados
Domain, Application, Infrastructure, Api, Web e Tests.

## 4. Entidades criadas
Organization, User, Form, Question, QuestionOption, Survey, SurveyLink, SurveyResponse, ResponseAnswer, ResultScore, AuditLog.

## 5. Tabelas criadas
`organizations`, `users`, `forms`, `questions`, `question_options`, `surveys`, `survey_links`, `responses`, `response_answers`, `result_scores`, `audit_logs`.

## 6. Endpoints criados
Auth, `/me`, organizations, users, forms, surveys, survey links, public validate/responses/results e audit events.

## 7. Telas criadas
Home, Login, Meu Perfil, Dashboard, Organizações, Usuários, Formulários, Pesquisas, Links, Respostas, Auditoria, Responder e Resultado.

## 8. Services criados
BCryptPasswordHasher, Sha256TokenHasher, QuestionScoreCalculator, SurveyResultCalculator, AuditService e middlewares de erro/correlationId.

## 9. Repositories criados
Dapper inicial para usuários, organizações e auditoria; contratos preparados para formulários, pesquisas, links e respostas.

## 10. Testes criados
xUnit cobrindo cálculo, login válido/inválido, ocultação de hash, token público, token de resultado, isolamento, SQL e auditoria.

## 11. Comandos executados
`dotnet --version`, `dotnet build`, `dotnet test`, `npm run backend:build`, `npm run backend:test`, `npm run db:scriptbd-completo`.

## 12. Comandos que falharam e motivo
`dotnet` não está instalado no container. Scripts npm foram executados quando existentes; falhas foram registradas no terminal.

## 13. Pendente
Completar todos os repositories Dapper, DTOs finais, validações profundas e testes de integração com PostgreSQL real.

## 14. Próximo passo recomendado
Instalar SDK .NET 8 no ambiente de CI, restaurar/buildar a solution e evoluir repositories dos módulos ainda stubados.
