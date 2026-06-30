# Sprint 66 — Documento operacional legado

Este documento registra o ajuste do projeto antigo na Sprint 66.

- O legado permanece em Firebase/Cloud Functions com produção sem cutover automático para API.
- Segredos de SMTP devem ser configurados somente como Firebase Secret `SMTP_PASSWORD`.
- O e-mail remetente operacional é `valoragroup@mnsoft.com.br`.
- O fluxo público usa provider automático com fallback para Cloud Functions.
- O resultado exige `responseId` real e token de resultado válido.
- O certificado exibe código de validação, URL pública, e-mail mascarado e ações de download/impressão.
- Planos usam Firestore com fallback oficial: free, essential, professional, corporate e enterprise.
- O link gratuito oficial não expira por data vencida quando não estiver revogado.
- O menu mobile administrativo usa bridge independente carregado depois de `app.js`.
- Smoke de produção só roda com variáveis explícitas de autorização e participante de teste.

## Publicação
1. Configurar secret: `firebase functions:secrets:set SMTP_PASSWORD`.
2. Publicar Functions: `firebase deploy --only functions`.
3. Publicar Hosting: `npm run build:prod` e `firebase deploy --only hosting`.
