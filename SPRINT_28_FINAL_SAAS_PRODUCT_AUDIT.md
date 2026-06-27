# Sprint 28 — Auditoria final do produto SaaS

## Escopo auditado
Foram auditados `package.json`, validadores `scripts/validate-*`, frontend Bootstrap/JS puro, `config.js`, camada híbrida (`api-client.js`, `api-repository.js`, `gateway-client.js`, `repository.js`, `runtime-capabilities.js`), backend ASP.NET Core, migrations PostgreSQL `valorapesquisa`, Docker, Firebase, Firestore rules, migração, scripts Windows e documentação operacional.

## Respostas objetivas
1. **Gates executáveis:** `check`, segurança, contratos de dados, resiliência de renderização, bootstrap frontend, erros API frontend, observabilidade API, rotas públicas, PostgreSQL schema, backend build/test, migração validate/compare, SaaS readiness, auth, certificado, e-mail, billing, security gate, frontend SaaS, SaaS E2E, cutover dry-run, rollback readiness e release candidate.
2. **Gates documentais:** nenhum gate Sprint 28 novo deve aprovar apenas por documento; documentos são evidência complementar.
3. **Gates que passam sem fluxo real:** `prod:saas-e2e` roda modo contratual quando `VALORA_E2E_LIVE` não está definido; para homologação real usar `VALORA_E2E_LIVE=1` com API/PostgreSQL locais.
4. **Fluxos sem endpoint real:** geração binária PDF/PNG plena ainda está em fallback JSON seguro; validação por código está exposta como endpoint seguro.
5. **Fluxos sem tela frontend:** frontend possui marcadores/telas mínimas para os módulos SaaS; refinamento UX ainda é risco conhecido para homologação assistida.
6. **Fluxos sem teste e2e:** E2E real completo depende de ambiente local vivo; gate contratual foi criado para não simular sucesso operacional indevido.
7. **Dependem apenas de Firebase:** produção publicada continua Firebase por regra de cutover; API/PostgreSQL é trilha local/híbrida.
8. **Funcionam com `DATA_PROVIDER=api`:** health, auth, planos, organizações, pesquisa pública, respostas, resultados, certificados fallback, comunicações e migração quando backend/PostgreSQL estão ativos.
9. **Funcionam com `DATA_PROVIDER=hybrid`:** leitura híbrida e fallback preservados pela camada de runtime; escrita duplicada não é habilitada automaticamente.
10. **Endpoints legacy existentes:** rotas `/legacy/*` permanecem para compatibilidade controlada.
11. **Legacy que pode vazar:** risco mitigado por flags `Compatibility:EnableLegacy*Routes`; security gate valida bloqueio em produção.
12. **TODO/mock/stub/pending:** pendências devem ficar registradas no risco/limitações; marcadores críticos de produção são bloqueados pelos validadores.
13. **Validadores com falso positivo:** antes da Sprint 28, certificado/e-mail/billing/features eram documentais; agora verificam código, endpoint, tabela, frontend e testes.
14. **Scripts promovidos para gate real:** `validate-saas-production-readiness`, `validate-certificate-production-flow`, `validate-email-production-flow`, `validate-billing-entitlements`, `validate-saas-feature-coverage`, `validate-release-candidate`.
15. **Módulos mínimos incompletos:** geração binária real de certificado, processamento SMTP vivo e execução E2E com infraestrutura sempre disponível.
16. **Regras de plano pendentes:** limites críticos (`activeSurveys`, `responsesPerMonth`, `managers`) são validados; recursos avançados dependem de homologação comercial por plano.
17. **Relatórios/certificados/e-mails E2E:** certificado possui fallback seguro; e-mail possui fila/admin; E2E vivo exige `VALORA_E2E_LIVE=1`.
18. **Riscos que impedem produção:** qualquer gate falho, segredo em configuração, legado habilitado em produção, cutover API sem aprovação, ausência de backup/rollback.
19. **Riscos como limitação conhecida:** fallback JSON de certificado, E2E contratual sem infraestrutura viva e ajustes de UX pós-homologação.
20. **Checklist real faltante:** executar `tools/windows/65-validar-produto-saas-final-e2e.bat` em Windows Server/homologação com PostgreSQL/API, registrar evidências e aprovar cutover manual.

## Marcadores mapeados
A auditoria considera os marcadores `TODO`, `NotImplemented`, `NotSupportedException`, `mock`, `stub`, `demo`, `pending`, `pending-backfill`, `legacy`, `hardcoded`, `será integrado`, `próxima fase`, `not implemented`, `StatusCode(501`, `Console.WriteLine`, `console.log`, `console.error`, `SaveResponseAsync`, `/legacy/`, `validate-`, `PASS`, `missing` e `documentation`. Marcadores críticos em caminhos de produção devem falhar nos gates; marcadores históricos/documentais devem permanecer registrados como risco ou limitação.

## Diagnóstico real
O produto está mais próximo de homologação SaaS porque os gates Sprint 28 deixam de validar somente presença documental e passam a bloquear ausência de código, endpoint, tabela, frontend e testes. A produção atual não foi migrada para PostgreSQL/API; Firebase permanece como padrão seguro até cutover aprovado.
