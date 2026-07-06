# Auditoria final — Sprint Web Legacy Public Parity

## 1. Resumo
Foi criada uma camada pública ASP.NET MVC/Razor separada da camada administrativa para aproximar a jornada e identidade do legado raiz.

## 2. Diagnóstico inicial
Registrado em `SPRINT_WEB_LEGACY_PARITY_DIAGNOSTIC.md` antes das alterações de layout e código.

## 3. Diferenças encontradas entre legado e Web nova
A Web nova usava layout administrativo na Home e páginas públicas; faltavam topbar/footer públicos, bot visual, modal/toast e jornada comercial completa.

## 4. Layout público criado
`Views/Shared/_PublicLayout.cshtml` contém skip link, topbar pública, main, footer, modal, confirmação, toast, bot panel, ações flutuantes, CSS e JS públicos.

## 5. Layout administrativo separado
`Views/Shared/_AdminLayout.cshtml` preserva sidebar, topbar interna, scripts e CSS administrativos; `_ViewStart.cshtml` aponta para ele por padrão.

## 6. CSS público criado
`wwwroot/css/valora-public.css` define variáveis de marca, hero, cards, botões, badges, grids, timeline, footer, modal, toast, bot, ações flutuantes e mobile-first.

## 7. JS público criado
Criados scripts em `wwwroot/js/public/` para loading, toast, modal/bot, validação básica, bloqueio de submit, consumo da API oficial e CTAs.

## 8. Home pública migrada
`Views/Home/Index.cshtml` usa `_PublicLayout` e apresenta hero, CTA diagnóstico, WhatsApp, benefícios, como funciona, dimensões, resultado/certificado, LGPD e contato.

## 9. Jornada diagnóstico migrada
`/diagnostico-gratuito` usa formulário público e tenta criar diagnóstico via `/api/free-diagnostics`.

## 10. Jornada resultado migrada
`/resultado/{responseId}` usa view pública e carrega resultado via `/api/public/results/{responseId}`.

## 11. Jornada certificado migrada
`/certificado/{certificateId}` e `/certificado/validar/{codigo}` usam layout público e componentes de certificado/validação.

## 12. Jornada LGPD migrada
`/lgpd` e `/lgpd/solicitacao` usam layout público e mensagens de privacidade/minimização.

## 13. WhatsApp/contato migrados
`/whatsapp` e `/contato` foram criadas como páginas públicas com CTA seguro e sem envio automático de dados sensíveis.

## 14. ValoraBot/botPanel migrado ou pendente justificado
Bot panel visual foi migrado sem Firebase; respostas inteligentes/base de conhecimento ficam pendentes para integração futura com serviço oficial.

## 15. Paridade mobile
CSS público aplica menu colapsável, cards em uma coluna, botões grandes, floating actions acessíveis e ausência de sidebar nas páginas públicas.

## 16. Documentos de paridade criados
Criados `LEGACY_PUBLIC_JOURNEY_TO_ASPNET_PARITY.md` e `LEGACY_PUBLIC_LAYOUT_TO_ASPNET_PARITY.md`.

## 17. Validadores criados
Criado `tools/validate-aspnet-web-public-legacy-parity.js` e script `web:public-legacy-parity`.

## 18. Testes E2E criados
Criado `tests/e2e-web/public-legacy-parity.spec.js` para verificar Home, diagnóstico, resultado, certificado, LGPD e ausência de sidebar.

## 19. Comandos executados
- `npm run web:public-legacy-parity` — PASS após ajuste de termos sensíveis e remoção de menção indevida em `Valora.Web`.
- `npm run web:permission-parity` — PASS.
- `npm run web:module-parity` — PASS.
- `npm run web:journey-parity` — PASS.
- `npm run web:ui-parity` — PASS.
- `npm run web:no-fake-admin-data` — PASS.
- `npm run web:business-rules` — PASS.
- `npm run web:no-sensitive-ui` — PASS após remoção de termo sensível literal da página LGPD.
- `npm run web:no-data-access` — PASS.
- `npm run web:final-release-gate` — PASS.
- `npm run check:critical` — PASS.
- `node --check tools/validate-aspnet-web-public-legacy-parity.js && for f in backend/Valora.Web/wwwroot/js/public/*.js; do node --check "$f"; done` — PASS.
- `dotnet --version && dotnet restore backend/Valora.sln && dotnet build backend/Valora.sln && dotnet test backend/Valora.sln` — NÃO EXECUTOU por limitação do ambiente: `dotnet` não encontrado.
- `npm run web:e2e` — NÃO EXECUTOU por limitação do ambiente: Playwright tentou iniciar a Web com `dotnet`, mas `dotnet` não está instalado.

## 20. Comandos não executados e motivo
- Nenhum comando obrigatório foi omitido voluntariamente; os comandos .NET e E2E foram tentados, mas bloqueados pela ausência do SDK/runtime `dotnet` no container.

## 21. Gaps restantes
Integração fina de envio real de e-mail público, renderização rica de perguntas dinâmicas e ValoraBot inteligente dependem de contratos oficiais/validação em ambiente integrado.

## 22. Riscos
Alguns validadores legados podem exigir padrões anteriores ou serviços externos; testes Playwright dependem da aplicação em execução.

## 23. Próximo passo recomendado
Rodar homologação visual com screenshots, testar em celular, comparar com o site legado publicado e corrigir diferenças finas de UX antes do RC2.
