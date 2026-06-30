# Sprint 02 — Diagnóstico da conclusão vertical .NET v2

## 1. Endpoints reais
- `POST /auth/login`, `POST /auth/logout` e `GET /me` já usavam repositório de usuários, BCrypt/JWT e auditoria básica.
- `GET/POST /organizations` e `GET/POST /users` tinham persistência parcial, mas com retorno de entidades sensíveis e sem todo o escopo obrigatório.

## 2. Endpoints placeholder
- `forms`, `surveys`, `survey-links`, `public/surveys/validate`, `public/surveys/{id}/responses`, `public/results/{id}` e `audit/events` retornavam arrays vazios, ações genéricas, tokens temporários ou resultado fixo.

## 3. Repositories implementados
- `UserRepository` e `OrganizationRepository` existiam parcialmente no arquivo único `DapperRepositories.cs`.
- `AuditService` registrava eventos sem listagem nem sanitização forte.

## 4. Repositories faltando
- `FormRepository`, `SurveyRepository`, `SurveyLinkRepository`, `ResponseRepository` e `AuditRepository` real.

## 5. Interfaces sem implementação Dapper real
- `IFormRepository`, `ISurveyRepository`, `ISurveyLinkRepository` e parte de `IResponseRepository` estavam declaradas, mas sem classe concreta registrada no DI.

## 6. Telas MVC existentes
- Login, Me, Dashboard, Organizações, Usuários, Formulários, Pesquisas, Links, Respostas, Auditoria, Responder e Resultado.

## 7. Telas que ainda não consumiam API real
- Todas eram páginas genéricas com texto inicial e sem chamadas AJAX específicas por tela.

## 8. Testes superficiais
- `FoundationTests.cs` cobria apenas criação da solution, texto do SQL, strings de controller e cálculo simples.

## 9. Riscos para concluir Sprint 01
- Placeholders em endpoints críticos.
- Ausência de isolamento consistente por `organization_id`.
- Tokens públicos/resultados sem hashing real no fluxo completo.
- API podia retornar entidades com campos sensíveis.
- SQL e testes não garantiam índices e tabelas usados pelos repositories.

## 10. Plano objetivo
1. Separar DTOs/contratos seguros.
2. Implementar repositories Dapper reais por responsabilidade.
3. Substituir `FoundationControllers.cs` por controllers reais separados.
4. Registrar todos os services/repositories no DI.
5. Atualizar SQL idempotente com tabelas e índices mínimos.
6. Atualizar Web MVC para consumir API via jQuery/AJAX sem hashes/JSON bruto.
7. Expandir testes de cálculo, segurança textual, SQL e isolamento.
