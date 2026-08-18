# Checklist de deploy seguro

- [ ] `ASPNETCORE_ENVIRONMENT=Production` e `VALORA_SEED_DEMO=false`.
- [ ] Connection string e JWT fornecidos por secret store, nunca por arquivo versionado.
- [ ] Segredo JWT exclusivo com ao menos 32 caracteres.
- [ ] `script_completo.sql` aplicado com `ON_ERROR_STOP=1` e backup validado.
- [ ] SMTP, armazenamento e diretório de backup configurados ou explicitamente desabilitados.
- [ ] HTTPS, proxy reverso, logs e retenção de auditoria revisados.
- [ ] `/health`, `/health/database` e `/health/web/api` saudáveis.
- [ ] `/EnvironmentStatus` não revela connection string, token, senha, SQL ou stack trace.
- [ ] Smoke autenticado e público concluído com uma organização de homologação isolada.
