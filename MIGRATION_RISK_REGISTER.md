# Registro de riscos da migração

| Risco | Mitigação |
|---|---|
| Dados sem shape padronizado | normalizadores e relatório de rejeições |
| Senhas inseguras/legadas | não migrar texto puro; forçar reset quando necessário |
| Resultado divergente | modo híbrido e comparação por amostra |
| Jobs de e-mail duplicados | idempotência por `response_id` e status |
| Corte abrupto de produção | ativação por empresa/piloto via `DATA_PROVIDER` |
