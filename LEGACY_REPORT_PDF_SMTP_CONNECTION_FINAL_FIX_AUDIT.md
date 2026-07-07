# Auditoria final — legacy report PDF, legacy_run e SMTP Gmail

Data: 2026-07-07.

## 1. Onde aparece `data-action="legacy_run"`

Não há ocorrência literal de `data-action="legacy_run"` nos arquivos auditados. A ação existia como ação de sistema no helper `run(...)`, que usa o rótulo/action `legacy_run` para executar rotinas legadas. Os botões reais usam ações específicas como `reportResponsePdf`, `certificatePdf`, `downloadCertificatePdf`, `reloadPublicResult` e `resendResultEmail`.

## 2. Onde `legacy_run` é roteado

`legacy_run` agora é roteado em `createActions()` para `legacyRun(el)`. `legacyRun` lê somente `data-run`, `data-fn` ou `data-legacy-action` e resolve o nome por allowlist em `LEGACY_PUBLIC_ACTIONS`; não usa `eval` nem `window[name]`.

## 3. Onde `reportResponsePdf` é chamado

`reportResponsePdf` aparece em botões da listagem administrativa de respostas, na tela de resultado individual e no mapeamento de actions. Também há aliases seguros para `downloadResultReport`, `reportPdf` e `downloadReport`.

## 4. Se `reportResponsePdf` existe ou não

Antes da correção, havia chamadas para `reportResponsePdf`, mas a função oficial não existia no escopo de execução, causando `ReferenceError`. Agora existe `async function reportResponsePdf(el)` e ela é registrada diretamente em `createActions()`.

## 5. Qual função real deveria gerar relatório

A geração real deve usar `window.ValoraPdf.createReport` ou `window.ValoraPDF.createReport`, implementada em `pdf.js` como `createReport(options, filename)`. A nova `reportResponsePdf(el)` carrega o resultado público com `ValoraRepository.loadPublicResult(responseId, resultToken)`, normaliza o bundle/view model e chama `createReport`.

## 6. Onde `certificatePdf` / `downloadCertificatePdf` é chamado

`certificatePdf` é usado nos cards de certificados, no resultado individual e em `createActions()`. `downloadCertificatePdf` é usado no resultado público imediato com `responseId` e `resultToken`. O handler `downloadCertificatePdf(el)` permanece protegido, e `createActions()` também mapeia `downloadCertificatePdf(el){ return certificatePdf(el); }` para compatibilidade de ações legadas.

## 7. Onde `sendResultEmailInternal` cria o transporter

`sendResultEmailInternal(...)`, em `functions/index.js`, chama `createSmtpTransporter()` e executa `await transporter.verify()` antes de `sendMail(...)`.

## 8. Valores mascarados de SMTP usados pela Function

Foi criada `smtpDebugConfig()`, que retorna:

- `enabled`
- `host`
- `port`
- `secure`
- `requireTLS`
- `userMasked`
- `fromEmailMasked`
- `replyToMasked`
- `hasPassword`

Para a configuração esperada de Gmail, o diagnóstico deve mostrar:

- host: `smtp.gmail.com`
- port: `587`
- secure: `false`
- requireTLS: `true`
- userMasked: `va***@mnsoft.com.br` (exemplo mascarado)
- fromEmailMasked: `va***@mnsoft.com.br` (exemplo mascarado)
- hasPassword: `true`

## 9. Por que `smtp_connection_failed` está ocorrendo

O erro `smtp_connection_failed` indica falha antes/aut durante a conexão SMTP, normalmente por host/porta incorretos, TLS/secure incompatível, rede bloqueada, DNS/porta recusada, timeout, ou secrets não disponíveis no runtime. Para Gmail em 587, `secure` precisa ser `false` e `requireTLS` precisa ser `true` para STARTTLS. A correção força essa combinação no transporter e aumenta timeouts para 45s. Se o erro persistir após deploy, use `sendTestEmail` para validar a conexão real e conferir `smtpDebugConfig().hasPassword`.

## 10. Correções feitas

- Criada `reportResponsePdf(el)` com carregamento seguro do resultado público, normalização e geração por `ValoraPdf/ValoraPDF.createReport`.
- Criados aliases `downloadResultReport`, `reportPdf` e `downloadReport`.
- Criado `LEGACY_PUBLIC_ACTIONS` e `legacyRun(el)` com allowlist.
- `createActions()` passou a mapear ações de relatório, certificado e `legacy_run` sem referência indefinida.
- `safeRun` já captura Promise; o helper `run` foi simplificado para não anexar catch duplicado que pudesse causar erro cru.
- `handleActionError` grava `ValoraRuntimeDiagnostics.lastActionError` sem exibir mensagem técnica crua na interface.
- `createSmtpTransporter()` agora força Gmail 587 com `secure:false`, `requireTLS:true`, TLS v1.2 e timeouts de 45s.
- `classifyEmailError()` diferencia autenticação, conexão, configuração incompleta e destinatário inválido, incluindo detalhe técnico de conexão sem expor senha.
- `sendTestEmail` agora executa `verify()`, envia e-mail real, normaliza aceite/rejeição, grava log e retorna config mascarada.
- `debugEmailDelivery` retorna `smtpDebugConfig()` mascarado.
- Mensagens de falha de e-mail no front exibem status amigável e detalhes técnicos (`Código` e `Detalhe`).
- O fallback local de job de e-mail não retorna mais `queued:true` quando o processador direto não é usado.
- Criados validadores específicos para PDF legacy, allowlist, handlers, SMTP Gmail, secrets, sendTestEmail, classificação de erro, queued e visibilidade do erro de e-mail.
