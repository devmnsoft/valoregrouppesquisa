# Sprint 29 — Auditoria de Gates Funcionais Reais

Data: 2026-06-27  
Escopo: fechamento de homologação SaaS para Valora Pulse / Valora Insight, preservando Firebase como produção padrão e PostgreSQL/API apenas por cutover controlado.

## 1. Validadores ainda apenas documentais

Nenhum dos validadores críticos da Sprint 29 permanece somente documental. Os validadores abaixo agora checam presença de código, endpoints, tabelas/migrations, frontend, testes ou contratos executáveis:

- `scripts/validate-saas-production-readiness.js`
- `scripts/validate-certificate-production-flow.js`
- `scripts/validate-email-production-flow.js`
- `scripts/validate-billing-entitlements.js`
- `scripts/validate-saas-feature-coverage.js`
- `scripts/validate-saas-end-to-end-flow.js`
- `scripts/validate-saas-frontend-coverage.js`
- `scripts/validate-cutover-dry-run.js`
- `scripts/validate-rollback-readiness.js`

Limitação conhecida: o E2E SaaS executa chamadas HTTP reais quando `VALORA_E2E_LIVE=1` e valida contrato/código quando a API local não está iniciada. Para homologação final, rodar com PostgreSQL e API ativos.

## 2. Gates que passavam sem executar fluxo real

Antes da Sprint 29, os gates com maior risco de aprovação documental eram:

- `prod:saas-readiness`
- `prod:certificate-flow`
- `prod:email-flow`
- `prod:billing`
- `prod:saas-features`

Eles foram fortalecidos para validar arquivos de backend, migrations PostgreSQL, rotas, integrações frontend, testes e bloqueios de produção.

## 3. Scripts `prod:*` que ainda não existem

Todos os scripts obrigatórios existem em `package.json`:

- `prod:saas-readiness`
- `prod:no-legacy`
- `prod:no-pending`
- `prod:saas-features`
- `prod:auth-flow`
- `prod:certificate-flow`
- `prod:email-flow`
- `prod:billing`
- `prod:security-gate`
- `prod:saas-e2e`
- `prod:frontend-saas`
- `prod:cutover-dry-run`
- `prod:rollback-ready`
- `prod:final-gate`

## 4. Scripts `prod:*` existentes, mas fracos

Após a Sprint 29, os scripts críticos foram endurecidos. Risco residual: `prod:saas-e2e` precisa ser executado em modo live (`VALORA_E2E_LIVE=1`) no ambiente de homologação para comprovar HTTP + PostgreSQL de ponta a ponta.

## 5. Fluxos SaaS ainda sem teste E2E

Não há bloqueio conhecido sem gate. O fluxo SaaS E2E cobre contrato de health, auth, `/me`, planos, survey pública, submit, resultado, certificado, e-mail, auditoria, usage, bloqueio de limite, correlationId e sanitização. O modo live deve ser usado na homologação final.

## 6. Fluxos SaaS ainda sem tela frontend

A cobertura frontend valida renderizações/telas para login, cadastro, recuperação de senha, dashboard, organizações, usuários, planos, formulários, pesquisas, links públicos, respostas, resultado, certificado, comunicação, auditoria, migração, status do ambiente e configurações da empresa.

## 7. Fluxos SaaS ainda sem `DATA_PROVIDER=api`

Risco residual controlado: o modo API depende de backend local e PostgreSQL com migrations aplicadas. A validação está coberta por `api:provider`, `api:e2e`, `prod:saas-e2e`, `backend:health` e `postgres:mvp`.

## 8. Fluxos SaaS ainda sem `DATA_PROVIDER=hybrid`

Risco residual controlado: `hybrid:check` valida o modo híbrido sem trocar produção automaticamente e sem duplicar escrita fora das regras configuradas.

## 9. Endpoints legacy ainda existentes

Rotas e referências legacy continuam mapeadas para compatibilidade, mas `prod:no-legacy` e `prod:security-gate` bloqueiam exposição indevida em produção. Firebase e arquivos legados preservados por regra de negócio.

## 10. Endpoints legacy que podem vazar para produção

Nenhum vazamento crítico aceito. O gate de segurança exige bloqueio de rotas legacy em produção e preserva `ALLOW_API_PRODUCTION_CUTOVER=false`.

## 11. Funcionalidades com `TODO`/mock/stub/pending

Mapeamento por busca textual no repositório em 2026-06-27:

| Marcador | Ocorrências |
|---|---:|
| TODO | 78 |
| NotImplemented | 20 |
| NotSupportedException | 3 |
| mock | 30 |
| stub | 21 |
| demo | 205 |
| pending | 119 |
| pending-backfill | 6 |
| legacy | 116 |
| hardcoded | 5 |
| será integrado | 2 |
| próxima fase | 2 |
| not implemented | 9 |
| StatusCode(501 | 2 |
| Console.WriteLine | 2 |
| console.log | 142 |
| console.error | 133 |
| SaveResponseAsync | 4 |
| /legacy/ | 9 |
| validate- | 582 |
| PASS | 457 |
| missing | 66 |
| documentation | 3 |

Interpretação: há marcadores históricos/documentais e utilitários de diagnóstico. Marcadores críticos de produção são bloqueados pelos gates `prod:no-pending`, `prod:no-legacy`, `prod:saas-readiness` e `prod:security-gate`.

## 12. Features ainda só documentação

Nenhuma feature mínima de homologação ficou somente em documentação sem gate. Certificados, comunicação/e-mail, billing, cobertura SaaS, frontend SaaS, cutover e rollback possuem scripts executáveis.

## 13. Módulos SaaS mínimos incompletos

Os módulos mínimos têm cobertura por migration/tabela, repository, service/regra, endpoint, frontend/integração e teste/validador. Pontos que exigem homologação operacional: SMTP real, domínio final, CORS final, credenciais JWT de produção e banco PostgreSQL restaurável.

## 14. Regras de plano/assinatura ainda não aplicadas

O gate de billing valida plano free no cadastro, limites `activeSurveys`, `responsesPerMonth`, `managers` e resposta comercial `PLAN_LIMIT_REACHED`. Recursos adicionais (`organizations`, `units`, `resultEmail`, `resultWhatsApp`, `customBranding`, `executiveReport`) permanecem no radar de homologação e devem ser conferidos com massa real.

## 15. Relatórios/certificados/e-mails sem ponta a ponta

- Certificado: endpoints PDF/PNG/validate existem com fallback JSON seguro se binário real não estiver pronto.
- E-mail: fila, listagem/admin processing e falha SMTP sanitizada são validados.
- Relatórios: release candidate gera `reports/release-candidate-report.json` e `RELEASE_CANDIDATE_REPORT.md`.

## 16. Riscos que impedem homologação final

- Falha em qualquer gate obrigatório no script Windows final de 65 passos.
- API/PostgreSQL local não subir para `VALORA_E2E_LIVE=1`.
- SMTP real sem credenciais ou sem timeout testado.
- Divergência em compare Firebase/PostgreSQL sem relatório e aceite.
- Segredo real detectado em frontend, appsettings ou logs.

## 17. Riscos que podem ficar como limitação conhecida

- Fallback JSON seguro para PDF/PNG enquanto geração binária real não for aprovada.
- Cutover API/PostgreSQL bloqueado até aceite formal.
- Firebase permanece produção padrão.
- Homologação não promete zero bug; reduz regressão com gates funcionais.

## 18. O que precisa ser corrigido antes de produção

1. Rodar `tools/windows/65-validar-produto-saas-final-e2e.bat` em ambiente Windows de homologação.
2. Rodar `VALORA_E2E_LIVE=1 npm run prod:saas-e2e` com API e PostgreSQL ativos.
3. Validar SMTP real com conta operacional e sem vazamento de senha.
4. Validar backup, dry-run, apply local, compare e rollback testável.
5. Aprovar plano de cutover antes de alterar `DATA_PROVIDER` em produção.

## Diagnóstico real do produto

O produto está em estágio homologável condicionado: os gates funcionais foram implantados, Firebase continua preservado como produção padrão, API/PostgreSQL estão protegidos por cutover explícito e há documentação de risco, rollback, LGPD, observabilidade e deploy.

## Plano de homologação

1. Subir PostgreSQL local.
2. Aplicar migrations.
3. Subir backend ASP.NET Core.
4. Rodar gates unitários e estáticos.
5. Rodar `VALORA_E2E_LIVE=1 npm run prod:saas-e2e`.
6. Rodar script Windows final de 65 passos.
7. Registrar evidências em `TESTES_EXECUTADOS.md` e no release report.

## Plano de publicação

1. Publicar somente Firebase atual se não houver cutover aprovado.
2. Manter `DATA_PROVIDER='firebase'` e `ALLOW_API_PRODUCTION_CUTOVER=false`.
3. Validar `prod:health` pós-publicação.
4. Monitorar logs com correlationId e sem dados sensíveis.

## Plano de cutover

1. Executar export real Firebase.
2. Executar transform real.
3. Executar import dry-run.
4. Executar backup antes do apply.
5. Executar apply local/homologação.
6. Executar compare real.
7. Aprovar divergências.
8. Habilitar API somente por flag explícita e janela controlada.

## Plano de rollback

1. Preservar Firebase como origem segura.
2. Manter backup PostgreSQL pré-apply.
3. Reverter flag de provider para Firebase.
4. Restaurar banco se necessário.
5. Validar saúde, login e jornada pública.

## Próxima sprint recomendada

Sprint 30: homologação live assistida com API/PostgreSQL/SMTP reais, geração binária definitiva de PDF/PNG, testes Playwright para jornada pública mobile e revisão de limites avançados por plano.
