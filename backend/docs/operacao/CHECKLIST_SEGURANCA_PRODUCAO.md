# Checklist de segurança de produção

- [ ] Injetar `ConnectionStrings__Postgres` por variável protegida e exigir TLS no PostgreSQL.
- [ ] Injetar `Jwt__SigningKey` aleatória (32+ caracteres), sem reutilizar o valor de Development.
- [ ] Confirmar `App__EnableDemoSeed=false` e `App__EnableDetailedErrors=false`.
- [ ] Confirmar `Security__RequireHttps=true`, HSTS e certificado HTTPS válido.
- [ ] Definir cada origem HTTPS em `Cors__AllowedOrigins`; curingas não são aceitos para produção.
- [ ] Manter senhas SMTP e credenciais fora dos arquivos versionados.
- [ ] Validar `/SystemHealth`: itens críticos impedem o go-live; a resposta nunca deve revelar segredos.
- [ ] Validar escopo de API Keys, exportações, webhooks e isolamento por organização.
- [ ] Registrar e verificar um backup antes da janela de publicação.

Consulte `VARIAVEIS_AMBIENTE_PRODUCAO.md` e `RUNBOOK_OPERACIONAL.md` para os comandos reais.
