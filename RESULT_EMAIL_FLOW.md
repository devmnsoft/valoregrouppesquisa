# Fluxo de e-mail de resultado

## Diagnóstico técnico
- A resposta local é salva em `submitSurvey`, que monta `response`, faz `state.responses.push(response)` e chama `save()`.
- Em Firebase Spark, a resposta pública é salva pela callable `submitSurveyResponse` em `functions/index.js`, que recalcula o resultado e grava `responses/{responseId}`.
- O resultado é calculado por `calculateResult`; para Valora Insight™, `calculateValoraInsightResult` e `generateValoraInsightDevolutiva` consolidam pontuação, nível e dimensões.
- O e-mail deve ser disparado depois de salvar/calcular, por `dispatchSurveyCompletedCommunications`, que agora chama `dispatchPostSurveyCommunication`.
- A função antiga de envio genérico é `sendEmail`; o novo envio transacional usa `sendSurveyResultToClient` para chamar `/communication/result/send`.
- A configuração que impedia envio real seguro era `EMAIL_TRANSPORT:'disabled'` ou ausência de `EXTERNAL_API_BASE_URL`/gateway/token. SMTP nunca fica no frontend.
- Dados do participante: nome, e-mail, telefone/WhatsApp, LGPD e preferência de comunicação.
- Dados da pesquisa: `surveyId`, título, empresa, formulário e status.
- Dados do resultado: pontuação, máximo, percentual, nível, dimensão mais forte, dimensão prioritária, devolutiva, link do resultado e link do certificado.

## Disparo pós-pesquisa
Ao concluir a pesquisa, o frontend salva a resposta, calcula o resultado e chama `dispatchSurveyCompletedCommunications(response.id)`. O fluxo evita duplicidade se `response.communication.resultEmail.status === 'sent'`.

## Estados registrados
`communications` armazena `sent`, `failed`, `pending-provider` e `missing-recipient`, com `providerMessageId`, mensagem, datas e vínculo com empresa/pesquisa/resposta.

## Mensagens ao participante
A tela pública só mostra mensagens amigáveis: resultado enviado, envio pendente ou ausência de e-mail. Termos técnicos de infraestrutura não são exibidos.
