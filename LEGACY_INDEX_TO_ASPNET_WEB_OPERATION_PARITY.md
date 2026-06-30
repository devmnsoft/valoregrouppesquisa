# Paridade Operacional Legacy x Valora.Web
- painel operacional: documentado para migração em Valora.Web mantendo Bootstrap, JavaScript, jQuery e AJAX.
- pesquisa gratuita permanente: regra equivalente deve preservar pesquisa oficial sem expiração indevida.
- fila/retry de e-mail: emailJobs, maxAttempts, pending_retry e dead_letter devem existir no backend compartilhado.
- diagnóstico Blaze/Functions/SMTP: cards e endpoints de diagnóstico devem manter evidência runtime.
- menu mobile completo: mesma fonte do desktop, sem dashboard-only.
- backup/rollback: Valora.Web deve seguir PRODUCTION_BACKUP_ROLLBACK_RUNBOOK.md.
- logs sanitizados: não exibir token, senha, private_key, tokenHash, API key completa, CPF/CNPJ ou e-mail aberto sem necessidade.
