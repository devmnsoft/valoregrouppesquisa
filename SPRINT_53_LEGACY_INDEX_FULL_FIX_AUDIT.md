# Sprint 53 — Auditoria completa do index legado

## Respostas objetivas
1. Correções do Valora.Web pendentes no legado: mitigadas por validadores de paridade e contratos no index antigo.
2. Correções do legado pendentes no Valora.Web: documentadas em paridade e validadas por `web:correction-parity`.
3. Link público usa `publicToken`; `tokenHash` não é publicado.
4. Pesquisa gratuita não expira indevidamente por `expiresAt` no runtime tolerante.
5. `isBetween` não bloqueia a pesquisa gratuita oficial com token público válido e status ativo.
6. `ensureSurveyPublicLink` persiste `publicToken`, `publicUrl` e `publicLink` no objeto/repair.
7. `buildHomeFeaturedSurveyUrl` gera URL compartilhável `index.html?survey=...&token=...&org=...`.
8. `renderTakeSurvey` valida link público sem exigir login.
9. `loadPublicSurvey`/validação aceita `publicToken` antes de legado.
10. `loadValidSurvey` aceita pesquisa gratuita com expiração tolerante.
11. Repair aceita credenciais por flags e variáveis.
12. Cadastro de cliente exige organization/company/settings e campos mínimos.
13. Cadastro de usuário chama `createCompanyUser`.
14. Cloud Function cria usuário no Firebase Auth.
15. Cloud Function cria `users/{uid}`.
16. Cloud Function define custom claims.
17. `signInWithPassword 400` é mapeado para mensagens amigáveis.
18. Menu mobile antigo possui funções runtime e CSS.
19. Menu mobile Valora.Web possui validação runtime.
20. Bloqueios restantes: credenciais/implantação/homologação real.

## Status Sprint 53
- Projeto antigo da raiz preservado e mantido com Firebase como provedor principal.
- Pesquisa gratuita oficial deve usar `publicToken` em URL pública e nunca `tokenHash` como token compartilhável.
- Expiração da pesquisa gratuita é tolerante no runtime e corrigida para longo prazo pelo repair seguro.
- Cadastro de cliente e usuário possui contratos estruturais validados.
- Menu administrativo mobile possui funções explícitas, overlay, ARIA e fechamento por ESC/resize/item.
- Valora.Web permanece ASP.NET Core MVC/Razor Pages com Bootstrap, JavaScript, jQuery e AJAX.

## Riscos restantes
- Execução com dados reais depende de credenciais Firebase Admin e homologação em ambiente controlado.
- Cloud Functions precisam ser implantadas após aprovação.
