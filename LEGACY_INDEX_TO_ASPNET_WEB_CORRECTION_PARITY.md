# LEGACY INDEX TO ASPNET WEB CORRECTION PARITY

Documento da Sprint 57. As correções foram aplicadas no projeto legado da raiz e documentadas para paridade com backend/Valora.Web.

- Blaze: config.js usa plano Blaze e Cloud Functions habilitadas.
- Cloud Functions: fallback documentado para validação/submissão/resultado/e-mail.
- Provider auto: API externa primeiro, fallback para Cloud Functions quando aplicável, com idempotencyKey.
- Pesquisa gratuita: não bloqueia por expiresAt vencido quando oficial, ativa e com token válido.
- E-mail após resultado: usa responseId e resultToken reais; falha de e-mail não perde resposta.
- Menu mobile completo: fonte única getAdminMenuItems, overlay/ESC/clique fecham o menu, sidebar rola em 100dvh.
- Diagnóstico do ambiente: painel exibe Blaze, SMTP, Functions e últimos erros sem revelar senha, token, private_key ou service account.
- Limitações antes de produção: dependem de secrets SMTP reais, DNS SPF/DKIM/DMARC, deploy Firebase e smoke live autenticado.
