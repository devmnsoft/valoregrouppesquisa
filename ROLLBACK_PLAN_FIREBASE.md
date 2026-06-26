# Rollback para Firebase

## Voltar provider
Publicar configuração com `DATA_PROVIDER=firebase` e `ALLOW_API_PRODUCTION_CUTOVER=false`.

## Preservar respostas feitas na API
Exportar respostas API criadas durante a janela, registrar lote de compensação e reprocessar para Firebase ou manter fila auditável.

## Reprocessar divergências
Usar relatório de comparação, `migration.import_errors` e diagnósticos hybrid para corrigir respostas, resultados e comunicações.

## Revalidar produção
Executar `npm run check`, `npm run cutover:ready` e healthcheck PRD.

## Publicar rollback no IIS
Gerar pacote, publicar `config.js` Firebase, reciclar app pool se necessário e validar URL pública.

## Estado Sprint 8
- Produção permanece segura com `DATA_PROVIDER: 'firebase'` e `ALLOW_API_PRODUCTION_CUTOVER: false`.
- API/PostgreSQL ficam disponíveis para homologação local/controlada com `DATA_PROVIDER: 'api'` ou `DATA_PROVIDER: 'hybrid'`.
- Firebase, `firebase-repository.js` e `repository.js` são preservados.
- Frontend não armazena SMTP, segredos de e-mail ou token WhatsApp; comunicação deve passar por Gateway/API.
