# Checklist de segurança de identidade

- [x] Política mínima de 10 caracteres e complexidade.
- [x] Bloqueio inicial de senhas comuns e dados de identidade.
- [x] Value objects para códigos e hashes.
- [ ] Cadastro empresarial atômico e CNPJ obrigatório.
- [ ] Refresh token com rotação, uso único e detecção de reuse.
- [ ] Sessões, logout e logout-all.
- [ ] RBAC e escopo aplicados a todos os endpoints e queries.
- [ ] Rate limiting de login/recuperação.
- [ ] SMTP/outbox sem token bruto persistido.
- [ ] Testes de IDOR, CSRF, CORS, injeção e escalada.
