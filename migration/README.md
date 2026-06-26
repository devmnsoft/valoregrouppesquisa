# Migração Firestore → PostgreSQL (schema único)

Mapeamento atual: `companies/organizations → valorapesquisa.organizations`, `users → valorapesquisa.users`, `plans → valorapesquisa.plans`, `forms → valorapesquisa.forms/questions/options`, `surveys → valorapesquisa.surveys`, `responses → valorapesquisa.responses/response_answers/result_scores`.

Use `node migration/export-firestore.js --dry-run`, `node migration/transform-firestore-to-postgres.js` e `node migration/import-postgres.js --dry-run` antes de qualquer `--apply` controlado.
