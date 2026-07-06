# Auditoria final — submit público, e-mail de resultado e rota pública

1. **Payload enviado ao `submitSurveyResponse`**: o front monta e valida o payload público antes da Function, incluindo `surveyId`, token público, organização, respostas, identificação do participante e consentimento LGPD. O diagnóstico sanitizado fica em `[Valora] submitSurveyResponse payload`.
2. **Retorno bruto da Function `submitSurveyResponse`**: após `submitPublicSurveyResponse(payload)`, o app registra um resumo sanitizado em `[Valora] submitSurveyResponse result` e em `window.ValoraRuntimeDiagnostics.lastSubmitFunctionResult`, contendo `responseId`, presença de `resultToken`, `resultEmail.status`, `messageId`, contagens de `accepted/rejected` e erro SMTP.
3. **Onde `resultEmail.status` é definido**: no backend, o status de SMTP é produzido por `normalizeSendMailResult(info, to)` e persistido em `responses.communication.resultEmail` e `responses.resultEmail`.
4. **Onde `sendResultEmail` é chamado**: o submit público recebe o resultado da Function `submitSurveyResponse`; reenvios públicos usam `ValoraRepository.sendResultEmail`, que chama a callable `sendResultEmail` com `responseId` e `resultToken`.
5. **SMTP real**: `sendResultEmail` e `sendTestEmail` usam `nodemailer.createTransport(...).sendMail(...)` com secrets SMTP.
6. **Retorno do `sendMail`**: `normalizeSendMailResult` exige `messageId`, destinatário em `accepted` e ausência em `rejected`; caso contrário retorna `failed` com `reason: smtp_not_accepted`.
7. **`email_logs`**: todo envio de resultado grava `email_logs` com `type: result`; `sendTestEmail` grava `type: test`, ambos com `messageId`, `accepted`, `rejected`, status e erro sanitizado.
8. **`email_jobs`/fila**: o legado ainda cria fallback em `email_jobs`; o backend administrativo usa a coleção `emailJobs` para filas/retries e `debugEmailDelivery` retorna esses registros como `email_jobs`.
9. **Mensagem honesta no front**: `renderEmailDeliveryStatus` só mostra “Resultado enviado...” para `sent`; `queued/pending` indicam processamento, `failed_non_blocking/failed` indicam falha, e `unknown` não confirma envio.
10. **Rota `?result=&rt=` sem Auth**: a rota pública chama `ValoraRepository.loadPublicResult(responseId, resultToken)`/`getPublicResult` e os validadores bloqueiam uso de login/Auth nessa rota.
11. **Ruído de extensão**: `unhandledrejection` ignora apenas a mensagem conhecida de canal assíncrono quando a stack é de extensão ou vazia, preservando erros reais do app quando a stack contém marcadores do app.
