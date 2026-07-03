# Auditoria final — resultado público por resultToken e envio real de e-mail

Data: 2026-07-03

## Arquivos auditados

- `app.js`
- `firebase-repository.js`
- `repository.js`
- `functions/index.js`
- `functions/package.json`
- `pdf.js`
- `config.js`
- `config/config.production.js`
- `scripts/build-production.js`
- `scripts/validate-*.js`

## 1. Detecção da rota pública `?result=&rt=`

A rota pública é detectada em `app.js` por `isPublicResultRoute()`, que exige simultaneamente os parâmetros `result` e `rt`. O boot/router chama `releasePublicUi('public_result_route')` e retorna `renderPublicResultFromRoute()` antes de qualquer renderização privada.

## 2. Tentativa de Firebase Auth/signInWithPassword

A tentativa de login permanece apenas nos fluxos privados de login do repositório Firebase (`firebase-repository.js`). A rota pública de resultado e `loadPublicResultFirebase()` não chamam `currentUser`, `getIdToken`, `signInWithPassword`, `signInWithEmailAndPassword`, `renderLogin` ou criação de usuário. O validador `scripts/validate-legacy-public-result-no-auth-login.js` bloqueia regressão.

## 3. Submit recebe `responseId`/`resultToken`

`submitSurveyResponse` cria `responses/{responseId}`, gera `resultToken`, grava apenas `resultTokenHash` no Firestore e retorna `responseId`, `resultToken` e `accessToken` ao front.

## 4. Link público do resultado

O link público é montado exclusivamente por `publicResultUrl(responseId, resultToken)` usando o secret `PUBLIC_APP_URL` e os parâmetros `result` e `rt`.

## 5. Onde `sendResultEmail` é chamado

No front, o reenvio usa `ValoraRepository.sendResultEmail(responseId, { resultToken })`. No backend, `exports.sendResultEmail` valida `responseId/resultToken` antes de enviar.

## 6. `submitSurveyResponse`: envio direto ou fila

`submitSurveyResponse` usa fila: cria `emailJobs` quando há consentimento de comunicação. Portanto retorna `resultEmail.status = 'queued'` e nunca retorna `sent` nesse caminho.

## 7. Status `sent`, `queued` e `failed`

- `queued`: decidido em `submitSurveyResponse` ao criar `emailJobs`.
- `sent`: decidido apenas por `normalizeSendMailResult()` após `sendMail` retornar `messageId`, destinatário em `accepted` e destinatário ausente de `rejected`.
- `failed`: decidido por `normalizeSendMailResult()` ou no `catch` SMTP.
- `failed_non_blocking`: reservado ao submit quando a criação de job falha sem bloquear o resultado.

## 8. `nodemailer.sendMail` é aguardado

Sim. `sendResultEmail` executa `await transporter.verify()` e `await transporter.sendMail(...)` antes de decidir o status final.

## 9. Retorno `accepted/rejected/messageId`

`normalizeSendMailResult(info, to)` normaliza `accepted`, `rejected` e `messageId`. O retorno público contém esses campos sem expor segredo.

## 10. Gravação de `email_logs`/`emailJobs`

- `emailJobs`: usado para fila e rastreio operacional.
- `email_logs`: gravado em todo envio real/tentativa real por `writeResultEmailLog()` com destinatário mascarado, `status`, `messageId`, `accepted`, `rejected`, erro e provider `smtp`.

## 11. Certificado PDF com `responseId/resultToken`

O front pega `responseId/resultToken` do dataset ou da URL, tenta carregar o resultado via `ValoraRepository.loadPublicResult()`, monta dados seguros e chama `window.ValoraPdf`/`window.ValoraPDF.createCertificate`. Falhas são convertidas em toast controlado e não substituem `#app`.

## 12. Correções feitas

- Bloqueio de Auth/login nas rotas públicas por token.
- `renderPublicResultFromRoute()` carrega resultado por Cloud Function pública `getPublicResult` e faz fallback seguro.
- `getPublicResult` retorna bundle público completo, sem exigir `req.auth` e sem retornar `resultTokenHash`.
- SMTP real via Firebase Secrets, `createSmtpTransporter()`, STARTTLS e timeouts.
- Status honesto: `sent` apenas quando SMTP aceitou o destino; `queued` não é exibido como enviado.
- Logs em `email_logs` e atualização de `responses/{id}.resultEmail`.
- `sendResultEmail` valida `resultToken` antes de enviar e não exige login público.
- Callables admin `debugEmailDelivery` e `sendTestEmail`.
- Agendador `processEmailJobs` a cada 5 minutos para fila.
- Validadores obrigatórios adicionados ao `package.json`.

## Observação operacional

A senha SMTP exposta anteriormente não foi usada nem gravada. É obrigatório gerar nova senha de app do Google e configurar exclusivamente via Firebase Secrets antes do deploy.
