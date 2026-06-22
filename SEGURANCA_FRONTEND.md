
## Auditoria XSS, build e storage — 2026-06-21

- O frontend publicado deve ser sempre o diretório `dist/`; a raiz do projeto não deve ser publicada.
- Source maps (`*.map`) e arquivos `.env` não devem existir no artefato publicado.
- `scripts/security-check.js` falha ao encontrar tokens Telegram/SMTP, service account, CSP com `connect-src *`, `script-src *`, `unsafe-eval` ou handlers inline em `dist/`.
- JS no navegador nunca é segredo absoluto; regras críticas permanecem em Firestore Rules, Cloud Functions, App Check e validação server-side.
- Campos dinâmicos devem usar `esc()`/`textContent`; URLs devem bloquear `javascript:` e protocolos não esperados.
- `localStorage`/`sessionStorage` não são fonte de verdade de segurança em produção: role, companyId, plano e permissões precisam ser validados no backend/Rules.
