# Auditoria — SMTP real, erros e acesso público por senha

1. `submitSurveyResponse` retorna `resultEmail.status` no objeto final `{ ok, responseId, resultToken, resultEmail, ... }` em `functions/index.js`.
2. `submitSurveyResponse` salva a resposta, gera `resultToken`, grava `resultTokenHash` e tenta envio direto via `sendResultEmailInternal`; não depende de `queued` para o fluxo público principal.
3. Existe `processEmailJobs` agendado para a fila administrativa `emailJobs`, mas o submit público não retorna `queued` nesse caminho.
4. `processEmailJobs` está exportada em `functions/index.js` com `onSchedule`.
5. O deploy solicitado precisa incluir `submitSurveyResponse`, `getPublicResult`, `sendResultEmail`, `getParticipantResultsByPassword`, `sendTestEmail` e `debugEmailDelivery`; a fila agendada é independente do envio direto do submit.
6. `submitSurveyResponse` declara `secrets:[...EMAIL_SECRETS,PARTICIPANT_PASSWORD_PEPPER]`.
7. `sendResultEmail` declara `secrets:EMAIL_SECRETS`.
8. `sendTestEmail` declara `secrets:EMAIL_SECRETS`.
9. `sendResultEmailInternal` executa `await transporter.verify()` antes do envio.
10. `sendResultEmailInternal` executa `await transporter.sendMail(...)`.
11. `normalizeSendMailResult` valida `messageId`, `accepted` contendo o destinatário e ausência em `rejected` antes de marcar `sent`.
12. Erros SMTP são classificados por `classifyEmailError`, retornados em `resultEmail.errorCode/errorMessage` e gravados em `email_logs` por `writeEmailLog`.
13. O front envia `accessPassword` em `buildPublicSurveySubmitPayload` via `accessPassword: getFormValue(formEl, 'accessPassword') || ''`.
14. O backend salva somente `participantAccess.emailHash` e `participantAccess.passwordHash`, calculado por `participantAccessHash`, nunca a senha pura.
15. O acesso público por e-mail + senha usa `ValoraRepository.getParticipantResultsByPassword` e a callable `getParticipantResultsByPassword`; o fluxo público validado não usa Firebase Auth/signInWithPassword.
16. Correções feitas: diagnóstico SMTP com campos faltantes, transporter TLS explícito, classificação de erros, falha não bloqueante honesta no submit, reenvio com erro real, `sendTestEmail` com log/classificação, validadores novos para secrets no submit e classificação, scripts npm, e auditoria documentada.
