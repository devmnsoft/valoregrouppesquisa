# Diagnóstico — Importação Controlada Legado/Firebase/localStorage → PostgreSQL Oficial

1. **Fontes legadas**: raiz JavaScript (`app.js`, `repository.js`, `firebase-repository.js`, `local-repository.js`), amostras `firestore.seed.sample.json` e `database.sample.json`, Cloud Functions em `functions/index.js` e utilitários.
2. **Coleções Firebase**: organizations/companies/companyProfiles, users/authUsers/companyUsers/participants, plans/modules/companyModules/subscription, forms/questions/dimensions/options, surveys/publicLinks/surveyLinks/invites, responses/answers/results/scores, certificates, emailJobs/communications/outbox, auditLogs/logs/events, consents/privacyRequests.
3. **Estruturas localStorage/JSON**: snapshots por coleção, arrays de documentos, objetos por id e export manual com `collections` ou `data`.
4. **Cloud Functions**: criação/alteração de pesquisas públicas, respostas, resultados, certificados, logs e comunicação/e-mail; nesta sprint não serão chamadas.
5. **Entidades oficiais existentes**: organizations, users, roles, permissions, plans, modules, forms, surveys, responses, results, certificates, communications, audit, LGPD, subscriptions e usage.
6. **Ajustes necessários**: tabelas de controle, mapeamentos, conflitos, arquivos fonte e rollback por batch.
7. **Mapeamento**: detalhado em `LEGACY_TO_POSTGRES_MAPPING.md`.
8. **Campos sem destino claro**: flags visuais antigas, caches, tokens públicos brutos, preferências temporárias e payloads operacionais sem contrato oficial.
9. **Dados sensíveis**: e-mail, telefone, CPF/CNPJ, tokens, hashes, senha, refresh token, SMTP e connection string.
10. **Não importar**: senha em texto, tokens brutos como segredo, refresh tokens, credenciais Firebase, connection strings e segredos SMTP.
11. **Deduplicação**: documento/slug/e-mail/nome para organizações; e-mail/legacyId/documento para usuários; legacyId e chaves compostas para demais entidades.
12. **Conflitos**: divergência bloqueante quando altera identidade, ownership, vínculo de resposta ou token; conflitos menores ficam como warning.
13. **Tabelas**: `migration_batches`, `migration_source_files`, `migration_records`, `migration_mappings`, `migration_conflicts`, `migration_rollback_items`.
14. **Services**: readers, normalizer, mapping, dry-run, apply, reconciliation, rollback, cutover readiness e report.
15. **Endpoints**: `/migration/sources`, `/migration/batches`, dry-run, apply, reconcile, conflicts, rollback e cutover readiness.
16. **Telas Web**: `/Migration` e páginas de batch, upload, dry-run, conflitos, conciliação, rollback e prontidão.
17. **Validadores**: `tools/validate-backend-official-migration-import.js`.
18. **Riscos de perda**: sobrescrita indevida, deduplicação errada, rollback parcial e documentos órfãos.
19. **Riscos de segurança**: vazamento de segredo, token bruto, stack trace, payload sensível e importação sem confirmação.
20. **Plano**: implantar estrutura oficial, dry-run persistente, apply protegido, rollback por batch, documentação e validação automatizada.
