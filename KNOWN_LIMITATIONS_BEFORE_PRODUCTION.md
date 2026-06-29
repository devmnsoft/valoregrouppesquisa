# Limitações conhecidas antes de produção

- Validar SMTP real e DNS SPF/DKIM/DMARC no domínio final.
- Executar bug bash manual em mobile, desktop, IIS e Docker.
- Confirmar migração PostgreSQL em banco limpo e banco com dados.
- Monitorar falsos positivos do anti-abuso nas primeiras 48 horas.
