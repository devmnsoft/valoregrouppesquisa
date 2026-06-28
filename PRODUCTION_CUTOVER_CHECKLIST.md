# Production Cutover Checklist

- Firebase preservado até aprovação explícita.
- DATA_PROVIDER=firebase permanece padrão.
- ALLOW_API_PRODUCTION_CUTOVER=false permanece padrão.
- Cutover para API requer janela aprovada, backup, dry-run, compare e plano de rollback.
- Não acessar Firebase real em dry-run sem confirmação explícita.
