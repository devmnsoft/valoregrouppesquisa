# Cutover Firebase → API/PostgreSQL

## Pré-requisitos
- Backup Firebase validado.
- Backup PostgreSQL validado.
- Dry-run obrigatório sem erros críticos.
- Apply local obrigatório em ambiente controlado.
- Comparação Firebase x PostgreSQL obrigatória.
- `ALLOW_API_PRODUCTION_CUTOVER=true` somente na janela aprovada.

## Execução
1. Congelar janela de manutenção.
2. Exportar Firestore.
3. Transformar e importar PostgreSQL.
4. Comparar entidades críticas.
5. Alterar `DATA_PROVIDER` de `firebase` para `api` apenas com aprovação.
6. Validar health, login, planos, pesquisa pública, resposta, resultado, certificado e comunicação.
7. Monitorar logs, divergências, e-mail e performance.

## Critério de rollback
Rollback imediato se health/database falhar, resposta pública falhar, divergência crítica aumentar, e-mail bloquear jornada ou certificados retornarem dados inválidos.

## Estado Sprint 8
- Produção permanece segura com `DATA_PROVIDER: 'firebase'` e `ALLOW_API_PRODUCTION_CUTOVER: false`.
- API/PostgreSQL ficam disponíveis para homologação local/controlada com `DATA_PROVIDER: 'api'` ou `DATA_PROVIDER: 'hybrid'`.
- Firebase, `firebase-repository.js` e `repository.js` são preservados.
- Frontend não armazena SMTP, segredos de e-mail ou token WhatsApp; comunicação deve passar por Gateway/API.
