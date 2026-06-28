# Production Rollback Checklist

- Responsável: líder técnico/on-call definido antes da janela.
- Tempo estimado: 15 a 30 minutos para retorno lógico; restore varia por volume.
- Risco: perda de dados escritos após backup se cutover parcial for revertido sem reconciliação.
- Backup: executar backup antes de qualquer mudança.
- Restore: validar restore em PostgreSQL local antes da janela.
- DATA_PROVIDER=firebase deve permanecer documentado como retorno imediato.
- Health pós-rollback: validar site, API health, login Firebase e jornada pública.
