# Plano de Migração Firebase → PostgreSQL

1. Exportar Firestore com `node migration/export-firestore.js`.
2. Transformar com `node migration/transform-firestore-to-postgres.js`.
3. Validar dry-run com `node migration/import-postgres.js --dry-run`.
4. Aplicar localmente com `--apply`.
5. Comparar com `node migration/compare-firebase-postgres.js`.
