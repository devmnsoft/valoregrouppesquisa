# Plano de migração Firebase → PostgreSQL

1. Exportar Firestore com `migration/export-firestore.js`.
2. Normalizar e transformar documentos com `migration/transform-firestore-to-postgres.js`.
3. Importar em transação por domínio com `migration/import-postgres.js`.
4. Validar contagens, hashes de respostas e amostras com `migration/validate-migration.js`.
5. Comparar resultados Firebase/PostgreSQL com `migration/compare-firebase-postgres.js`.

Senhas nunca serão migradas em texto puro; usuários deverão manter hash seguro existente ou passar por reset controlado.
