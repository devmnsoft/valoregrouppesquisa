# Plano de Rollback — Firebase

## Gatilhos

- Falha em `/health` ou `/health/database`.
- Divergência crítica no comparador.
- Erro relevante em login, envio de pesquisa, resultado público, certificado ou e-mail.

## Ação imediata

1. Restaurar `DATA_PROVIDER=firebase`.
2. Restaurar providers públicos para gateway/Firebase atual conforme configuração validada.
3. Reiniciar publicação IIS do frontend estático.
4. Manter PostgreSQL/API em modo somente diagnóstico.
5. Registrar incidente e preservar relatórios de comparação.

## Validação pós-rollback

- Rodar `npm run check`.
- Rodar health PRD.
- Testar login e jornada pública Firebase.
