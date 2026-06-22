# Relatório de Auditoria de Segurança — Valora Pulse

Data: 2026-06-21  
Escopo: frontend publicado, build de produção, Firebase Auth, Firestore Rules, Cloud Functions, links públicos, APIs, webhooks, logs/Telegram, LGPD, multiempresa e dependências.

> Observação central: JavaScript no navegador nunca deve ser tratado como segredo absoluto. A proteção real precisa ficar em Firestore Rules, Cloud Functions, App Check, validação backend, ausência de segredos no frontend e monitoramento.

| ID | Área | Severidade | Descrição | Risco | Como reproduzir | Impacto | Correção aplicada | Status |
|---|---|---|---|---|---|---|---|---|
| AUD-001 | Build/Hosting | alta | Auditoria automatizada de `dist/` era insuficiente; só verificava se `dist/` estava versionado. | Publicação acidental de source maps, `.env`, tokens, service account ou CSP fraca. | Executar `npm run security:check` antes desta correção. | Exposição de segredos e código-fonte auxiliar em produção. | `scripts/security-check.js` agora valida Hosting `dist`, source maps, arquivos sensíveis, tokens Telegram/SMTP, service account, CSP com `connect-src *`, `script-src *`, `unsafe-eval` e handlers inline em artefatos publicados. | corrigido |
| AUD-002 | Cloud Functions/e-mail | alta | HTML de e-mail concatenava título, mensagem e URL sem escape/sanitização de protocolo. | XSS/HTML injection em clientes de e-mail e links `javascript:`. | Chamar envio de convite/e-mail com `<script>` ou `javascript:` no payload. | Execução/renderização maliciosa em mensagens e phishing. | `buildEmailHtml` passa a escapar HTML e aceitar apenas URL HTTP/HTTPS. | corrigido |
| AUD-003 | APIs públicas | alta | `PUT /api/v1/employees/:id` fazia merge de `req.body`, permitindo campos inesperados como `companyId`, permissões ou metadados sensíveis. | Escalada de privilégio ou alteração indevida via API key com escopo de escrita. | Enviar `role=admin_valora`, `companyId=outra` ou campos extras no PUT. | Violação de isolamento e privilégios. | PUT agora usa allowlist de campos, força `companyId` da API key e restringe roles a perfis empresariais permitidos. | corrigido |
| AUD-004 | Webhooks/SSRF | crítica | Entrega de webhook aceitava qualquer URL cadastrada. | SSRF para localhost, IP privado ou HTTP inseguro. | Cadastrar webhook `http://localhost:...` ou `https://192.168.0.1/...` e chamar teste. | Acesso indevido a rede interna/metadados. | `assertWebhookUrl` exige HTTPS e bloqueia localhost, loopback, `.local` e IPs privados IPv4 comuns antes do envio. | corrigido |
| AUD-005 | Firestore Rules | alta | Cenários críticos foram revisados: empresas não leem/editam outras; respostas públicas não gravam direto; logs são append-only pelo backend. | Bypass de isolamento multiempresa. | Rodar testes de rules e tentar leitura/escrita cruzada. | Vazamento ou adulteração de dados. | Regras existentes mantêm bloqueios críticos; não houve relaxamento. | validado |
| AUD-006 | Links públicos | alta | Pontuação poderia ser tentada no payload. | Usuário manipularia score se backend aceitasse. | Enviar `normalized5`/`score` no payload de resposta. | Resultado fraudado. | Function calcula `calc(ctx.form, answers)` e monta resposta sem aceitar score do cliente. | validado |
| AUD-007 | Logs/Telegram | média | Risco de vazamento por metadados. | Envio de tokens/API keys/e-mails/documentos para logs externos. | Chamar logs com metadata contendo `token`, `apiKey`, `cpf`, `email`. | Vazamento operacional/LGPD. | Utilitários mascaram e-mail/documento e substituem chaves sensíveis por `***`; Telegram usa metadados sanitizados e rate limit. | validado |
| AUD-008 | Frontend/localStorage | média | Frontend pode esconder botões, mas não é fonte de verdade. | Manipulação de role no storage/estado local. | Alterar localStorage/devtools e acessar rota direta. | Tentativa de escalada no cliente. | Documentado que ações críticas devem validar Rules/Functions; storage não é fonte de segurança em produção. | monitorar |
| AUD-009 | Dependências | média | Dependências precisam de auditoria antes de deploy. | Vulnerabilidades transitivas em Firebase tooling. | Executar `npm audit` e `npm outdated`. | Risco de cadeia de suprimentos/build. | Auditoria documentada; atualização deve ser planejada se houver vulnerabilidades. | pendente conforme resultado do ambiente |
| AUD-010 | App Check/rate limit | média | Rate limit existe em funções públicas principais, mas App Check não foi confirmado no código. | Abuso automatizado de callable/onRequest. | Chamadas repetidas sem App Check. | Custos/DoS leve e spam. | Rate limits existentes validados em funções críticas; App Check listado como requisito antes de produção. | pendente |

## Matriz de perfis auditados

- `admin_valora`: acesso global controlado por Rules/Functions.
- `consultor_valora`: leitura/consultoria sem permissões destrutivas globais sensíveis.
- `empresa_admin`, `gestor_pesquisa`, `analista_resultados`, `gestor_area`: escopo por `companyId`, com restrições por área quando aplicável.
- `participante`, `convidado_externo`: sem acesso administrativo, financeiro, logs ou listas amplas.
- Usuário não autenticado: sem leitura direta de pesquisas/respostas; usa Cloud Functions públicas com token.

## Decisão de aceite

**Pronto com ressalvas**: vulnerabilidades críticas/altas identificadas no código foram corrigidas. Antes de produção, executar suíte completa em emuladores, confirmar App Check, revisar `npm audit` e validar build real (`npm run build:prod && npm run security:check`).
