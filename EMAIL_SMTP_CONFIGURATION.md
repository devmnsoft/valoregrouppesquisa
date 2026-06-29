# Configuração SMTP

Variáveis: Email__Enabled, Email__Provider, Email__FromName, Email__FromEmail, Email__ReplyTo, Email__Smtp__Host, Email__Smtp__Port, Email__Smtp__UseSsl, Email__Smtp__Username, Email__Smtp__Password, Email__Smtp__TimeoutSeconds. Nunca commitar senha real.

## Evidências Sprint 46
- Home pública: sem cards comerciais; CTA principal é Diagnóstico gratuito Valora Insight.
- Link público: padrão `index.html?survey={surveyId}&token={token}&org={slug}`, sem login, com expiração de 3650 dias.
- E-mail: resultado obrigatório por job e SMTP real via `SmtpEmailSender`; falha mantém fila.
- Certificado: conteúdo rico, código/link de validação, LGPD e CTA discreto.
- WhatsApp: CTA comercial pós-resultado configurado por `WHATSAPP_CONTACT_URL`.
- Valora.Web: ASP.NET Core API-first, consumindo Valora.Api por HTTP.
