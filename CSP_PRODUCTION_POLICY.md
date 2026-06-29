# Sprint 44 — CSP e SMTP Valora

## Diagnóstico

A CSP do Firebase permitia `script-src` apenas com `self` e `https://www.gstatic.com`, e `style-src` apenas com `self`/`unsafe-inline`; por isso bloqueava Bootstrap em `https://cdn.jsdelivr.net`. O `connect-src` não continha `https://api.valoragroup.mnsoft.com.br`, bloqueando chamadas à API.

O front legado chamava `/communication/result/send`; o contrato oficial ASP.NET é `/communications/result/{responseId}/send-email`. Foi mantida rota compatível obsoleta e o front ASP.NET usa o endpoint oficial.

O envio real dependia de `SmtpEmailSender`, que era mock; agora usa `System.Net.Mail.SmtpClient`, opções `Email`, validação, logs com e-mail mascarado e senha nunca retornada.

## Configuração SMTP

Use variáveis de ambiente: `Email__Enabled`, `Email__Provider`, `Email__FromEmail`, `Email__FromName`, `Email__ReplyTo`, `Email__Smtp__Host`, `Email__Smtp__Port`, `Email__Smtp__UseSsl`, `Email__Smtp__Username`, `Email__Smtp__Password`, `Email__Smtp__TimeoutSeconds`.

Em desenvolvimento mantenha `Email__Enabled=false`. Em produção/IIS defina essas variáveis no Application Pool/ambiente do Windows, sem commitar senha.

## Operação

- Enviar resultado: `POST /communications/result/{responseId}/send-email` com `to`, `subject`, `message`, `includeCertificate` e `resultToken`.
- Listar fila: `GET /admin/email-jobs` autenticado.
- Processar fila: `POST /admin/email-jobs/process` autenticado, payload `{ "batchSize": 10 }`.
- Status config: `GET /admin/email/config/status` autenticado; retorna apenas flags booleanas.

## CSP

Firebase Hosting permite `https://cdn.jsdelivr.net` em scripts/styles/fontes e `https://api.valoragroup.mnsoft.com.br` em `connect-src`. Alternativa mais segura: empacotar Bootstrap localmente e remover CDN da CSP.

## Gaps e riscos

É necessário configurar SMTP real no ambiente de produção, aplicar migration `database/postgresql/044_email_jobs_smtp_real.sql` e executar testes com provedor SMTP homologado. A rota compatível `/communication/result/send` deve ser removida em sprint futura.
