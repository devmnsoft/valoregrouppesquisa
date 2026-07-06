# Auditoria final — e-mail `queued` sem envio SMTP

## Escopo auditado

- `functions/index.js`
- `functions/package.json`
- `app.js`
- `firebase-repository.js`
- `repository.js`
- `config/config.production.js`
- `scripts/validate-*.js`

## Diagnóstico encontrado

1. `submitSurveyResponse` decidia `resultEmail.status` dentro de `functions/index.js`, após salvar a resposta e registrar auditoria.
2. O status `queued` era retornado no próprio `submitSurveyResponse` ao criar documento na fila `emailJobs`, sem aguardar confirmação SMTP.
3. Existe fluxo legado de fila em coleção `emailJobs`; não foi encontrada coleção chamada exatamente `email_jobs` para jobs. A coleção `email_logs` passa a registrar tentativas e resultados SMTP.
4. Existe Function agendada `processEmailJobs`.
5. `processEmailJobs` está exportada como `exports.processEmailJobs`.
6. Mesmo com `processEmailJobs` exportada, o submit público não deve depender da fila para o projeto legado; o deploy solicitado prioriza envio direto em `submitSurveyResponse`.
7. `sendResultEmail` já usava Nodemailer real em parte do fluxo, mas foi corrigida para reutilizar `sendResultEmailInternal`.
8. `sendMail` passa a ser aguardado com `await` em `sendResultEmailInternal`.
9. O aceite SMTP passa a ser normalizado por `normalizeSendMailResult`, exigindo `messageId`, destinatário em `accepted` e ausência em `rejected` para retornar `sent`.
10. O front já possuía mensagem honesta para `queued`; os validadores foram reforçados para impedir regressão.
11. O fluxo público podia aguardar inicialização de Auth antes de renderizar rotas com token; `init` agora libera `?survey=&token=` e `?result=&rt=` antes de esperar Auth.
12. Correções aplicadas: envio direto no submit, helper SMTP interno reutilizável, logs em `email_logs`, reenvio público por token sem login, `sendTestEmail`, `debugEmailDelivery`, validadores e auditoria.

## Estratégia oficial implementada

A estratégia principal agora é envio direto em `submitSurveyResponse`. O resultado nunca é bloqueado por falha SMTP: em caso de erro, a Function retorna `failed_non_blocking`, persiste `resultEmail` na resposta e registra `email_logs` com código/mensagem sanitizados.

## Critério de verdade para `sent`

O sistema só retorna `sent` quando:

- `messageId` existe;
- `accepted` contém exatamente o destinatário;
- `rejected` não contém o destinatário.

Qualquer ausência desses sinais retorna `failed` ou `failed_non_blocking`, nunca um falso enviado.
