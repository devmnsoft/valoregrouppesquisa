# SPRINT 44 — Auditoria CSP, Endpoint e SMTP

1. CSP bloqueava Bootstrap porque `firebase.json` não incluía `https://cdn.jsdelivr.net` em `script-src`, `script-src-elem`, `style-src` e `style-src-elem`.
2. CSP bloqueava API porque `connect-src` não incluía `https://api.valoragroup.mnsoft.com.br`.
3. Domínios externos necessários: `cdn.jsdelivr.net`, `api.valoragroup.mnsoft.com.br`, `www.gstatic.com`, APIs Google/Firebase, `viacep.com.br` e `brasilapi.com.br`.
4. Bootstrap continua por CDN nesta sprint; opção local está documentada em `CSP_LOCAL_ASSETS_OPTION.md`.
5. Front legado chamava `/communication/result/send`; Valora.Web chama `/communications/result/{responseId}/send-email`.
6. Endpoint oficial existe em `CommunicationsController`.
7. Sim, havia divergência; foi padronizada com rota compatível `[Obsolete]` sem duplicar fluxo.
8. Envio era mock; agora é SMTP real via `System.Net.Mail.SmtpClient`.
9. `SmtpEmailSender` está em `backend/Valora.Infrastructure/Email/SmtpEmailSender.cs`.
10. Agora envia de verdade quando `Email.Enabled=true` e SMTP válido.
11. Configuração SMTP criada em `appsettings` com senha vazia e suporte a variáveis de ambiente.
12. `email_jobs` existe e foi reforçada por migration idempotente `044_email_jobs_smtp_real.sql`.
13. Worker/processador real criado em `EmailQueueProcessor` e exposto em `/admin/email-jobs/process`.
14. Template textual real criado em `EmailTemplateService`.
15. Validadores e testes unitários de esqueleto foram adicionados para cobrir SMTP/config/fila/controller/CSP.
16. Logs usam `LogSanitizer.MaskEmail` e `SanitizeError`.
17. Auditoria operacional é registrada por status de jobs e comunicações existentes.
18. Gaps de produção: aplicar migration, configurar SMTP real, validar credenciais no IIS e remover rota compatível futura.

Mapeamento realizado para: Content-Security-Policy, script-src, style-src, style-src-elem, script-src-elem, connect-src, cdn.jsdelivr.net, api.valoragroup.mnsoft.com.br, endpoints de comunicação, SmtpEmailSender, EmailJobService, EmailTemplateService, CommunicationRepository, email_jobs, SMTP/System.Net.Mail.
