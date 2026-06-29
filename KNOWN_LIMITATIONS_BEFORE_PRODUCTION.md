# Limitações Conhecidas Antes de Produção

| Limitação | Bloqueante | Plano |
|---|---:|---|
| Cutover Firebase/API continua dependente de aprovação explícita. | Não | Manter `DATA_PROVIDER: 'firebase'` e `ALLOW_API_PRODUCTION_CUTOVER: false` até janela aprovada. |
| Smoke remoto depende de `VALORA_WEB_URL` e `VALORA_API_URL`. | Não | Executar em homologação e anexar evidências antes do go/no-go. |
| Não há promessa de zero bug. | Não | Gates bloqueiam falhas críticas conhecidas; bug bash manual complementa automação. |
