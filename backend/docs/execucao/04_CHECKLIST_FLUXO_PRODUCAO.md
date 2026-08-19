# Checklist do fluxo de produção

## Fundação e primeiro acesso
- [ ] `dotnet clean`, `restore` e `build` aprovados.
- [ ] API e Web iniciadas com PostgreSQL migrado duas vezes.
- [x] Produção bloqueia toda configuração marcada como obrigatória e não registra segredo.
- [ ] System Health exibe pendências sem connection string/secret.
- [ ] Login válido, inválido, logout, usuário inativo/suspenso e sem organização verificados.

## Fluxo produtivo
- [ ] Organização, usuário, perfil, permissão, plano e entitlement verificados.
- [ ] Diagnóstico criado, LGPD configurada e publicação validada.
- [ ] Link copiado, token validado, aceite LGPD e resposta persistidos atomicamente.
- [ ] Duplo envio bloqueado e participação atualizada com amostra mínima.
- [ ] Evidence, Metrics, índices, inferências e insights têm origem rastreável.
- [ ] Action, Journey e Evolution refletem processamento real.
- [ ] Heatmap/Radar ocultam grupos abaixo da amostra.
- [ ] Executive Report e certificado usam preview real e mensagem honesta sem PDF.
- [ ] Governança, auditoria e notificações recebem eventos sem dados sensíveis.

## Operação e deploy
- [ ] Todas as rotas principais e BFFs verificadas sem 404.
- [ ] Integrações bloqueadas por entitlement ou realmente configuradas.
- [ ] `dotnet publish` API/Web aprovado.
- [ ] Backup, restore, manutenção, rollback, CORS, HTTPS e headers homologados.
- [ ] Smoke manual documentado com evidência e correlationId quando houver falha.
- [ ] Testes automatizados continuam reservados para a fase final.
