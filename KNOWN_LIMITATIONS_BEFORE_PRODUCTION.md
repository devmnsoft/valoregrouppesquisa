# Limitações conhecidas antes de produção

- Envio real depende de `SMTP_PASSWORD` configurado como Firebase Secret e conta Google Workspace autorizada.
- Smokes de produção são bloqueados por padrão para evitar spam e submissões duplicadas.
- PDF client-side depende dos recursos do navegador; HTML/print permanece fallback seguro.
