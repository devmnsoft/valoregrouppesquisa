# Sprint 46 — Auditoria E2E do Diagnóstico Gratuito

1. A Home ainda exibe cards de planos? **Não na Home pública inicial.**
2. A Home exibe diagnóstico gratuito como chamada principal? **Sim.**
3. O botão gera link público válido? **Sim, com survey/token/org.**
4. O link pode ser compartilhado sem login? **Sim.**
5. O link expira indevidamente? **Não; configuração 3650 dias.**
6. Exige nome, e-mail e LGPD? **Sim.**
7. Resultado calculado corretamente? **Coberto por calculadoras/testes existentes.**
8. Resultado enviado por e-mail obrigatoriamente? **Sim, por job/endpoint.**
9. Envio usa SMTP real? **Sim.**
10. Se SMTP falhar, job fica em fila? **Sim.**
11. Certificado enriquecido? **Sim.**
12. CTA WhatsApp aparece após resultado? **Sim.**
13. Mensagem comercial adequada? **Sim, consultiva.**
14. CSP permite Bootstrap e API? **Sim.**
15. Valora.Web tem paridade? **Sim, documentada e validada.**
16. Admin acompanha respostas/e-mails? **Parcial: UI de email_jobs existente; produção exige homologação real.**
17. Gaps bloqueantes: SMTP real de produção e cutover supervisionado.
18. Gaps não bloqueantes: remoção futura da rota compatível.

## Evidências Sprint 46
- Home pública: sem cards comerciais; CTA principal é Diagnóstico gratuito Valora Insight.
- Link público: padrão `index.html?survey={surveyId}&token={token}&org={slug}`, sem login, com expiração de 3650 dias.
- E-mail: resultado obrigatório por job e SMTP real via `SmtpEmailSender`; falha mantém fila.
- Certificado: conteúdo rico, código/link de validação, LGPD e CTA discreto.
- WhatsApp: CTA comercial pós-resultado configurado por `WHATSAPP_CONTACT_URL`.
- Valora.Web: ASP.NET Core API-first, consumindo Valora.Api por HTTP.
