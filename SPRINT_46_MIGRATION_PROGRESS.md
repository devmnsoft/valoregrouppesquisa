# Sprint 46 — Progresso de Migração

## Migrado
- Jornada pública, certificados, comunicação transacional, resultados e LGPD em contrato API-first.

## Parcialmente migrado
- Perfis, permissões, módulos, jornada empresa e admin Valora.

## Pendente
- Homologação com SMTP real de produção, carga e cutover.

## Bloqueante
- Credenciais SMTP reais e validação operacional sem simulação.

## Não bloqueante
- Remover rota compatível obsoleta em sprint futura.

## Evidências Sprint 46
- Home pública: sem cards comerciais; CTA principal é Diagnóstico gratuito Valora Insight.
- Link público: padrão `index.html?survey={surveyId}&token={token}&org={slug}`, sem login, com expiração de 3650 dias.
- E-mail: resultado obrigatório por job e SMTP real via `SmtpEmailSender`; falha mantém fila.
- Certificado: conteúdo rico, código/link de validação, LGPD e CTA discreto.
- WhatsApp: CTA comercial pós-resultado configurado por `WHATSAPP_CONTACT_URL`.
- Valora.Web: ASP.NET Core API-first, consumindo Valora.Api por HTTP.
