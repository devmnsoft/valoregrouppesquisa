# Limitações conhecidas antes de produção
- Orçamento do Google Cloud Billing precisa ser conferido no console real.
- Secrets SMTP devem ser validados no ambiente Blaze real.
- Alertas externos dependem de canais operacionais configurados.
- Smoke tests live permanecem controlados por ALLOW_PRODUCTION_* para evitar custo e ruído.
