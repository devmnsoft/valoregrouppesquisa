# Plano de Correção de Segurança — Valora Pulse

## Corrigir antes de produção

### Críticas
- [x] Bloquear SSRF em webhooks: somente HTTPS e sem localhost/IP privado.
- [x] Garantir que Hosting publica `dist/` e não a raiz.
- [x] Falhar build/auditoria quando `dist/` contiver source maps ou segredos conhecidos.

### Altas
- [x] Escapar HTML em e-mails e bloquear `javascript:` em botões/links.
- [x] Trocar merge amplo da API de funcionários por allowlist de campos.
- [x] Confirmar que respostas públicas são criadas somente por Cloud Function.
- [x] Confirmar que score oficial é calculado no backend.
- [x] Confirmar que logs críticos não são gravados diretamente pelo frontend.
- [ ] Executar `npm audit` e tratar achados altos/críticos do ambiente.
- [ ] Habilitar/validar Firebase App Check para app web e Cloud Functions públicas.

## Corrigir antes de cliente piloto

- [ ] Ampliar testes automatizados de Rules para todos os perfis listados.
- [ ] Criar testes unitários para `assertWebhookUrl`, sanitização de e-mail e allowlist do PUT de funcionários.
- [ ] Adicionar verificação DNS/IP pós-resolução para webhooks, reduzindo bypass por DNS rebinding.
- [ ] Revisar todas as renderizações `innerHTML` do frontend e priorizar `textContent` em componentes novos.
- [ ] Implementar paginação/limites em endpoints que retornam coleções grandes.

## Monitorar

- [ ] Taxa de falhas/rate limit de links públicos.
- [ ] Tentativas de API key inválida/revogada.
- [ ] Erros de webhook e destinos rejeitados.
- [ ] Alertas Telegram suprimidos por anti-spam.
- [ ] Volume anormal de logs, convites e respostas.
