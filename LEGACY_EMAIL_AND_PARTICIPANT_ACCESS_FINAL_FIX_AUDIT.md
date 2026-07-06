# Auditoria final — e-mail legado e acesso público do participante

## Escopo auditado
- `app.js`
- `firebase-repository.js`
- `repository.js`
- `functions/index.js`
- `functions/package.json`
- `config.js`
- `config/config.production.js`
- `scripts/validate-*.js`

## Diagnóstico e correções

1. **Onde `submitSurveyResponse` retornava `resultEmail.status=queued`**: a implementação atual foi corrigida para envio direto; o trecho de submit inicializa `not_requested`, chama `sendResultEmailInternal` e não retorna `queued`.
2. **Se existe `email_jobs`**: há legado de fila em `emailJobs`/validadores antigos de `email_jobs`, mas o fluxo público corrigido não depende dessa fila para o e-mail do resultado.
3. **Se existe `processEmailJobs`**: existe `exports.processEmailJobs` agendada para `emailJobs` legado.
4. **Se `processEmailJobs` está exportada**: sim, exportada em `functions/index.js`.
5. **Se `processEmailJobs` foi incluída no deploy**: o deploy solicitado para a correção final não depende dela; as funções críticas são `submitSurveyResponse`, `getPublicResult`, `sendResultEmail`, `getParticipantResultsByPassword`, `sendTestEmail` e `debugEmailDelivery`.
6. **Se `sendResultEmail` usa Nodemailer real**: sim; foi centralizada em `sendResultEmailInternal`, com `createSmtpTransporter()` e `nodemailer.createTransport()`.
7. **Se `sendMail` é aguardado com `await`**: sim; `sendResultEmailInternal` executa `await transporter.verify()` e `await transporter.sendMail(...)`.
8. **Se `sendMail` valida `accepted/messageId`**: sim; `normalizeSendMailResult()` só considera `sent` quando há `messageId`, destinatário aceito e destinatário não rejeitado.
9. **Onde o front mostra mensagem de e-mail**: `renderEmailDeliveryStatus()` em `app.js`.
10. **Onde `accessPassword` é coletado no formulário**: formulário público de pesquisa em `app.js`, campo `name="accessPassword"`.
11. **Se `accessPassword` é enviado no payload**: sim; `buildPublicSurveySubmitPayload()` envia `accessPassword`.
12. **Se `accessPasswordHash` é salvo na response**: a senha pura não é salva; o backend salva `participantAccess.passwordHash` e `participantAccess.emailHash`.
13. **Se existe login público de participante**: sim; `#acessar-resultado` renderiza formulário público e chama `getParticipantResultsByPassword`.
14. **Onde ainda chama Firebase Auth/signInWithPassword para participante**: o fluxo `#acessar-resultado` não chama Firebase Auth. Chamadas de Auth permanecem apenas no login administrativo/portal.
15. **Correções aplicadas**: envio SMTP direto e honesto, logs em `email_logs`, reenvio por `sendResultEmail`, teste SMTP admin, debug admin, hash de senha pública, rotação de `resultToken` após login público por senha, front para acesso por e-mail+senha e validadores de regressão.
