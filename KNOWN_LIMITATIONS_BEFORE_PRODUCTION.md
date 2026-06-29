# Limitações Conhecidas antes da Produção

- Validar credenciais SMTP reais no ambiente final.
- Executar homologação E2E com e-mail real e evidências operacionais sem commitar anexos.
- Remover rota compatível obsoleta em sprint futura.

## Evidências Sprint 46
- Home pública: sem cards comerciais; CTA principal é Diagnóstico gratuito Valora Insight.
- Link público: padrão `index.html?survey={surveyId}&token={token}&org={slug}`, sem login, com expiração de 3650 dias.
- E-mail: resultado obrigatório por job e SMTP real via `SmtpEmailSender`; falha mantém fila.
- Certificado: conteúdo rico, código/link de validação, LGPD e CTA discreto.
- WhatsApp: CTA comercial pós-resultado configurado por `WHATSAPP_CONTACT_URL`.
- Valora.Web: ASP.NET Core API-first, consumindo Valora.Api por HTTP.
