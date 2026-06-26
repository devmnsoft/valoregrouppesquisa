# Fluxo de envio de resultado por e-mail

## Mapa técnico revisado
- Finalização da pesquisa: `submitSurvey` salva a resposta, calcula resultado e chama `dispatchSurveyCompletedCommunications` em segundo plano.
- Resultado: `calculateResult`, `calculateValoraInsightResult`, `generateValoraInsightDevolutiva`, `buildCertificateViewModel`, `buildResultLink` e `buildCertificateLink` montam devolutiva, links e certificado no frontend.
- Disparo: `dispatchPostSurveyCommunication(responseId)` chama o gateway com apenas `{ responseId, resultToken, channels: { email: true } }`.
- Gateway: `POST /communication/result/send` valida origem/payload, executa `loadResultContext`, busca response/survey/form/company no Firestore, reconstrói o payload real, monta HTML e envia por SMTP.
- Histórico: `communications` no frontend e coleção `communications`/JSONL no gateway registram status, tentativas, mensagem e falhas.
- Reenvio: `resendResultEmail(responseId)` aparece em Admin > Respostas e Admin > Comunicações e respeita permissão por empresa.
- Retry: `communication-gateway/src/queue/email-queue.js` persiste `logs/email-queue.json` e controla idempotência `survey-result:{responseId}:email`.
- Teste SMTP: Admin > Configurações > Comunicação usa `POST /communication/email/test`.

## Comportamento ao finalizar
1. A resposta é persistida.
2. O resultado é calculado e exibido.
3. A comunicação é disparada em segundo plano.
4. Falhas de SMTP não bloqueiam a tela de resultado.
5. Refresh não duplica envio quando a fila ou o histórico já possuem status `sent`, `pending` ou `processing`.

---

## Atualização — Sprint Jornada Principal

- Pesquisa pública usa provider configurável e não depende de Cloud Functions no Firebase Spark.
- Produção usa gateway externo quando `PUBLIC_SUBMISSION_PROVIDER='external-api'`.
- Fallback Firestore client só é permitido quando explicitamente configurado.
- Home prioriza pesquisa destaque válida antes dos planos.
- Certificados PDF/PNG usam ViewModel único e bloqueiam dados inválidos.
- Menu mobile usa `toggleMenu(force)` e fecha em navegação/logout.
- Cadastro Firebase cria Auth, organização e `users/{uid}` sem salvar senha em texto puro.
- Comunicação pós-pesquisa registra status e não bloqueia resultado.
