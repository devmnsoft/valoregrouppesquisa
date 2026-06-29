# Template de Resultado por E-mail

Assunto: Seu diagnóstico gratuito Valora Pulse está pronto

Conteúdo: Valora Pulse™, Valora Group, participante, pesquisa, pontuação total, nível de maturidade, dimensões, recomendação, links de resultado/certificado, CTA WhatsApp e rodapé LGPD.

## Evidências Sprint 46
- Home pública: sem cards comerciais; CTA principal é Diagnóstico gratuito Valora Insight.
- Link público: padrão `index.html?survey={surveyId}&token={token}&org={slug}`, sem login, com expiração de 3650 dias.
- E-mail: resultado obrigatório por job e SMTP real via `SmtpEmailSender`; falha mantém fila.
- Certificado: conteúdo rico, código/link de validação, LGPD e CTA discreto.
- WhatsApp: CTA comercial pós-resultado configurado por `WHATSAPP_CONTACT_URL`.
- Valora.Web: ASP.NET Core API-first, consumindo Valora.Api por HTTP.
