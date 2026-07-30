# Sprint Backend Oficial — Migration Reality Check Diagnostic

## 1. Identificação do projeto novo
O projeto novo oficial é `backend/Valora.sln`, composto por `Valora.Api`, `Valora.Application`, `Valora.Domain`, `Valora.Infrastructure`, `Valora.Tests` e `Valora.Web`.

## 2. Identificação do projeto antigo
O legado preservado na raiz é iniciado por `index.html` e usa HTML, Bootstrap, JavaScript puro, Firebase Auth, Firestore, Firebase Functions e serviços JavaScript como `app.js`, `repository.js`, `firebase-repository.js`, `local-repository.js`, `role-definitions.js`, `module-definitions.js`, `analytics-service.js`, `report-service.js` e `pdf.js`.

## 3. Diferença entre legado, backend, projeto .NET predecessor removido e scripts atuais
- Legado: referência funcional e operacional, sem receber migração estrutural nesta sprint.
- `backend`: base oficial ASP.NET/PostgreSQL para evolução, homologação e cutover.
- `projeto .NET predecessor removido`: referência histórica, sem novas features.
- Scripts SQL atuais: contrato oficial PostgreSQL em `script_completo.sql` e `backend/database/postgresql/*.sql`, devendo ser compatíveis com o schema real.

## 4. Mapa do que já foi migrado
- Solution oficial e camadas API/Application/Domain/Infrastructure/Web.
- Repositórios PostgreSQL para organizações, usuários, formulários, pesquisas, links, respostas, auditoria, settings e uso.
- Serviços de permissões, módulos, entitlements, menus, relatórios, certificados, LGPD, e-mail, exportações e migração.
- Views Web oficiais para home pública, login, dashboard, organizações, usuários, formulários, pesquisas, links públicos, respostas, certificados, auditoria, migração, comunicações e operações.
- Scripts de schema e seeds para planos, limites, capacidades, organização Valora e assinatura.

## 5. Mapa do que ainda falta migrar
- Homologação real com PostgreSQL e usuários piloto.
- Validação runtime fim a fim dos fluxos de e-mail, certificados e relatórios em ambiente real.
- Redução dos gaps controlados HTTP 501 ainda documentados.
- Revisão visual fina de paridade das telas Web com jornadas completas do legado.

## 6. Regras do legado ainda não aplicadas no backend
- Algumas regras comerciais de apresentação pública dos planos existem apenas no legado como rótulos (`priceLabel`, `badge`, `publicSubtitle`, `ctaLabel`) e não pertencem ao schema oficial atual.
- O backend oficial deve derivar rótulos a partir de `monthly_price`, `annual_price`, `name`, `description` e capacidades em vez de persistir colunas inexistentes.

## 7. Regras do legado já aplicadas no backend
- Perfis oficiais: `admin_valora`, `consultor_valora`, `empresa_admin`, `gestor_pesquisa`, `analista_resultados`, `gestor_area`, `participante`, `convidado_externo`.
- Módulos oficiais de dashboard, clientes/organização, financeiro/planos, usuários, formulários, pesquisas, links, respostas, relatórios, certificados, LGPD, exportações, comunicações, auditoria, migração e operações.
- Bloqueio por entitlement para relatórios, certificados, exportações e e-mail.

## 8. Endpoints oficiais existentes
APIs oficiais cobrem autenticação, organizações, usuários, formulários, pesquisas públicas e administrativas, respostas, planos, relatórios, certificados, LGPD, e-mail, exportações, auditoria, migração e health/operations conforme controllers em `backend/Valora.Api`.

## 9. Endpoints ainda incompletos
Devem permanecer apenas como gap controlado HTTP 501 quando não houver implementação real. `ASPNET_WEB_API_GAPS.md` deve ser a fonte de acompanhamento por endpoint e motivo.

## 10. Telas Web oficiais existentes
`backend/Valora.Web` contém páginas Razor e JavaScript modular para home pública, login, dashboard, organização, usuários, formulários, pesquisas, links públicos, respostas, certificados, auditoria, migração, comunicações, status e configurações.

## 11. Telas Web ainda incompletas
Jornadas que dependem de runtime real, e-mail/certificado/relatório e homologação com dados reais ainda exigem validação final antes de usuários piloto.

## 12. Validadores existentes
Há validadores npm para backend oficial, relatórios/e-mail, migração/importação, homologação/cutover, paridade de permissões, módulos, jornadas, ausência de dados fake e regras de negócio.

## 13. Validadores faltantes
O validador `tools/validate-backend-official-sql-schema.js` precisa bloquear regressões de colunas inexistentes em `plans`, contratos legados de `plan_limits`/`plan_capabilities`, `organizations(plan_id)` incompatível e seed textual em `plans(id)`.

## 14. Erros SQL encontrados
- `ERROR: column "price_label" of relation "plans" does not exist`.
- `ERROR: column "badge" of relation "plans" does not exist`.

## 15. Causa provável dos erros SQL
Seeds e/ou consultas estavam usando atributos comerciais do legado como se fossem colunas físicas de `valorapesquisa.plans`, enquanto o schema oficial usa UUID `id`, `code`, preços numéricos, descrição, ordem, status e timestamps.

## 16. Plano de correção
- Confirmar schema real de `plans`, `plan_limits`, `plan_capabilities`, `organizations` e `subscriptions`.
- Remover referências SQL e consultas a colunas inexistentes.
- Usar `plans.code` como chave natural e lookup para `plan_id` UUID.
- Garantir seeds idempotentes com `ON CONFLICT`.
- Atualizar validadores e documentação.

## 17. Riscos de segurança
- Exposição de token/senha/hash/secret em telas, logs ou exports.
- Endpoints temporários retornando dados fake ou stack trace.
- Uso indevido de Firebase na Web oficial.

## 18. Riscos de dados
- Seeds incompatíveis falhando em produção.
- Assinatura sem `plan_id` válido.
- Limites/capacidades divergentes do plano contratado.
- Migração legado -> PostgreSQL sem reconciliação e rollback testados.

## 19. Plano objetivo da sprint
Corrigir schema/seeds e repositório de planos, ampliar validador SQL, revisar paridade de regras legadas, atualizar gaps/documentação, executar builds e validadores possíveis, registrar auditoria final, commitar e abrir PR.
