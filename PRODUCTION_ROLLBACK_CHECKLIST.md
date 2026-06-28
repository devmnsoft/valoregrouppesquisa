# Production Rollback Checklist

- Responsável: líder técnico de plantão.
- Tempo estimado: 30 a 60 minutos após decisão de rollback.
- Executar backup antes de rollback usando `scripts/backup-local-postgres.js` ou `tools/windows/rollback/01-backup-postgres.bat`.
- Executar restore validado usando `scripts/restore-local-postgres.js` ou `tools/windows/rollback/02-restore-postgres.bat`.
- Voltar provider para DATA_PROVIDER=firebase e confirmar ALLOW_API_PRODUCTION_CUTOVER=false.
- Validar health pós-rollback com `/health`, frontend principal e login Firebase atual.
- Risco: respostas gravadas durante janela de API podem precisar reconciliação manual.
- Rollback test: simulação local deve ser executada antes da publicação.
