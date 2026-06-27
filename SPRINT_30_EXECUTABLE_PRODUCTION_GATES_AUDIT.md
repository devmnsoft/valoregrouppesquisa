# Sprint 30 — Auditoria de Gates Executáveis de Produção

Data: 2026-06-27

## Escopo auditado

Foram auditados `package.json`, validadores `scripts/validate-*`, frontend híbrido (`index.html`, `config.js`, `runtime-capabilities.js`, `api-client.js`, `api-repository.js`, `repository.js`, `app.js`), backend `backend/`, schema `database/postgresql/`, migração `migration/`, scripts Windows, Docker, Firebase Hosting/Rules e `functions/`.

## Respostas objetivas

1. **Gates ainda apenas documentais:** `prod:security-gate` foi fortalecido nesta sprint para validar código/configuração além dos documentos. Nenhum gate Sprint 30 novo deve passar apenas por documento.
2. **Gates que passam sem testar fluxo real vivo:** `prod:saas-e2e` executa contrato local e só chama API real com `VALORA_E2E_LIVE=1`; portanto exige execução live na homologação final.
3. **Scripts `prod:*` inexistentes:** nenhum dos obrigatórios Sprint 30 ficou ausente.
4. **Scripts `prod:*` fracos:** risco residual em `prod:saas-e2e` quando executado sem API local viva; mitigação: rodar com PostgreSQL/API no roteiro Windows 65.
5. **Fluxos SaaS sem teste E2E:** WhatsApp real e PDF/PNG binário real permanecem como limitação conhecida quando operam por fallback seguro.
6. **Fluxos SaaS sem tela frontend:** não identificado bloqueio crítico; cobertura frontend valida marcadores mínimos de Login, Cadastro, Planos, Pesquisas, Resultado, Certificado, Comunicação, Auditoria, Migração e Status.
7. **Fluxos sem `DATA_PROVIDER=api`:** dependem da execução local API/PostgreSQL com `VALORA_E2E_LIVE=1` para comprovação final.
8. **Fluxos sem `DATA_PROVIDER=hybrid`:** validação híbrida existe; homologação deve confirmar ausência de escrita duplicada em ambiente controlado.
9. **Endpoints legacy existentes:** `/legacy/planned`, `/legacy/admin/database/migrate`, `/legacy/admin/architecture/status`, `/legacy/admin/migration/status`, `/legacy/public/surveys/{surveyId}/validate`, `/legacy/public/surveys/{surveyId}/responses`, `/legacy/public/results/{responseId}`.
10. **Endpoints legacy que podem vazar para produção:** mitigados por `LegacyEnabled` e `legacy_route_disabled`; `prod:no-legacy` e `prod:security-gate` bloqueiam falta de guarda.
11. **Funcionalidades com marcadores TODO/mock/stub/pending:** existem ocorrências documentais e operacionais; pendências críticas devem permanecer em `BUG_RISK_REGISTER.md` e `KNOWN_LIMITATIONS_BEFORE_PRODUCTION.md`.
12. **Features só documentação:** cobrança externa real, WhatsApp real e render PDF/PNG binário podem depender de provedor/integração externa.
13. **Módulos SaaS mínimos incompletos:** sem bloqueio crítico detectado pelos validadores; módulos mínimos possuem tabela/repositório/endpoint/frontend ou integração/validador.
14. **Regras de plano não aplicadas:** validação cobre `activeSurveys`, `responsesPerMonth`, `managers` e resposta `PLAN_LIMIT_REACHED`; demais capacidades exigem homologação por cenário.
15. **Relatórios/certificados/e-mails sem E2E pleno:** certificado binário pode responder fallback JSON seguro; e-mail depende de SMTP configurado para envio real.
16. **Riscos que impedem homologação final:** falha de qualquer gate do script Windows 65, ausência de API/PostgreSQL local para E2E live ou segredo em configuração.
17. **Riscos aceitos como limitação conhecida:** fallback seguro de certificado, SMTP externo indisponível, WhatsApp manual e cutover sem aprovação explícita.
18. **Correções antes de produção:** executar `tools/windows/65-validar-produto-saas-final-e2e.bat`, revisar relatórios, aprovar cutover, validar rollback e manter Firebase como produção padrão.

## Mapeamento de marcadores

Busca executada com `rg` para: `TODO`, `NotImplemented`, `NotSupportedException`, `mock`, `stub`, `demo`, `pending`, `pending-backfill`, `legacy`, `hardcoded`, `será integrado`, `próxima fase`, `not implemented`, `StatusCode(501`, `Console.WriteLine`, `console.log`, `console.error`, `SaveResponseAsync`, `/legacy/`, `validate-`, `PASS`, `missing`, `documentation`.

Resultado: há ocorrências esperadas em documentação, scripts validadores e compatibilidade legacy. O risco operacional é controlado por `prod:no-pending`, `prod:no-legacy`, `api:no-fake-validator-comments`, `api:no-sensitive-logs` e pelo `prod:security-gate` fortalecido.

## Diagnóstico real

O produto está mais próximo de homologação SaaS porque os gates Sprint 30 validam código, rotas, schema, frontend, segurança, migração, cutover e rollback. Não há promessa de zero bug; o release deve ser bloqueado por falha em gate crítico ou por ausência de execução E2E live com API/PostgreSQL.

## Plano recomendado

1. Subir PostgreSQL local e backend.
2. Executar backend build/test.
3. Executar `VALORA_E2E_LIVE=1 npm run prod:saas-e2e`.
4. Executar `npm run prod:final-gate` para gerar relatórios.
5. Executar script Windows 65 no host de homologação.
6. Aprovar cutover somente com relatório de divergência aceitável e rollback testado.
