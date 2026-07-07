# Release Candidate Notes — Valora Backend Oficial

## 1. Versão sugerida
`0.9.0-rc1`

## 2. Data
2026-07-02

## 3. Escopo
Release Candidate de homologação real da versão oficial baseada exclusivamente em `backend/Valora.sln` e `database/postgresql`.

## 4. Features entregues
- API ASP.NET Core oficial.
- Web MVC ASP.NET oficial.
- Scripts de homologação PostgreSQL.
- Scripts oficiais de build, publish, package e healthcheck de produção.
- Validador automatizado de Release Candidate.
- Documentação de homologação, segurança, backup/restore e plano piloto.

## 5. Gaps conhecidos
- Neste ambiente não há .NET SDK, Docker, `psql` ou `pg_dump`; portanto restore/build/test, banco real, API/Web runtime e pacote real devem ser reexecutados em estação completa.

## 6. Como instalar
1. Instalar .NET SDK 8+.
2. Instalar Docker e cliente PostgreSQL.
3. Copiar `.env.example` para `.env` local de homologação sem versionar secrets.
4. Executar scripts `tools/linux/backend-hml-*` ou `tools/windows/backend-hml-*`.

## 7. Como validar
Executar `dotnet restore backend/Valora.sln`, `dotnet build backend/Valora.sln`, `dotnet test backend/Valora.sln` e todos os validadores `npm run backend:*validate` oficiais.

## 8. Como reverter
Seguir `ROLLBACK_PLAN.md` e `BACKUP_RESTORE_RUNBOOK.md`; restore exige `CONFIRM_RESTORE=RESTORE_LOCAL_HML` em ambiente local/homologação descartável.

## 9. Riscos
- Não executar cutover automático.
- Não usar dados reais versionados.
- Não expor secrets, hashes, tokens, stack traces ou connection strings.

## 10. Checklist de aceite
- [ ] Restore/build/test reais executados em ambiente completo.
- [ ] PostgreSQL homologação criado e schema aplicado.
- [ ] API/Web sobem e health checks respondem.
- [ ] Fluxos SaaS, pesquisa, relatórios, LGPD, e-mail, importação e backup/restore validados.
- [ ] Pacote de produção gerado sem secrets/dumps/logs sensíveis.

## Sprint backend oficial — reality check SQL/schema (2026-07-06)

- Base oficial mantida em `backend/Valora.sln`; `backend-v2` segue apenas como referência histórica e o legado da raiz permanece preservado.
- O schema oficial de planos é UUID em `plans.id` com chave natural `plans.code`; os seeds oficiais devem usar `ON CONFLICT (code)` e nunca gravar códigos textuais em `plans.id`.
- Os atributos comerciais legados `price_label`, `badge`, `public_subtitle`, `public_description`, `highlight_text` e `cta_label` não são colunas do schema oficial atual e não devem aparecer em INSERT/UPDATE SQL.
- `plan_limits` usa colunas estruturadas (`active_surveys`, `responses_per_month`, `users`, `managers`, `forms`, `public_links`, `email_invites_per_month`, `storage_mb`) com lookup por `plans.code` para obter `plan_id`.
- `plan_capabilities` usa `capability_code` e `enabled`; `capability_key`, `capability_level` e `capability_type` são contratos legados e permanecem bloqueados nos SQL oficiais.
- A organização Valora deve usar `organizations.plan_code` quando disponível e assinatura ativa em `subscriptions` apontando para `plans.id` resolvido por `plans.code`.
- O validador `npm run backend:sql-schema-validate` foi adicionado/confirmado como gate obrigatório para bloquear regressões de schema/seeds antes da homologação real.
- Endpoints ou telas ainda sem implementação real devem permanecer documentados como gap controlado; não é permitido retornar dados fake, JSON bruto sensível, stack trace, senha, hash, token ou secret.

## Release Candidate 0.9.0-rc2

RC2 registra a homologação real possível neste container: validadores Node oficiais executados, correção de UI sensível nas views operacionais, documentação de diagnóstico/auditoria/paridade/bugs e novo gate `npm run backend:rc2-homologation-validate`. A homologação runtime completa ainda deve ser repetida em ambiente com SDK .NET 8 e PostgreSQL/Docker disponíveis para executar `dotnet restore`, `dotnet build`, `dotnet test`, aplicação SQL idempotente, API/Web, health checks, importação e backup/restore reais.

## Sprint Web Legacy Public Parity
A migração da Web oficial ASP.NET somente será considerada completa quando houver paridade verificável de layout e jornada pública com o legado da raiz: Home comercial, diagnóstico gratuito, pesquisa pública, resultado, certificado, LGPD, WhatsApp/contato, footer, modal/toast/loading, ValoraBot visual, mobile-first e separação total entre `_PublicLayout` e `_AdminLayout`. A Web oficial deve consumir apenas a API oficial, sem Firebase, sem acesso direto ao banco, sem JSON bruto, sem dados fake e sem exposição de senha, hashes, tokens, segredos, connection strings ou payload sensível.

## Sprint Backend Centralização Valora Insight™

A fonte oficial da evolução Valora Insight™ passa a ser `backend/Valora.sln` e `database/postgresql`. O diagnóstico oficial usa 5 dimensões, 25 perguntas reais extraídas do legado (`app.js`), escala 1 a 5, total máximo 125 e devolutiva determinística no backend. A Web oficial não deve usar Firebase nem acessar banco diretamente; deve consumir a API oficial. Credenciais Firebase/service account devem ficar fora do repositório e a chave compartilhada fora do fluxo seguro deve ser revogada/rotacionada.

## Sprint Visual Homologation — assets da marca

Os binários oficiais `backend/Valora.Web/wwwroot/img/brand/valora-logo-full.jpeg` e `backend/Valora.Web/wwwroot/img/brand/valora-symbol.jpeg` precisam ser adicionados manualmente, pois o Codex não manipula arquivos binários de marca. A Web oficial possui fallback visual seguro com texto institucional “Valora Group”, evitando imagem quebrada e preservando layout público/admin até o upload manual.

Validação:

- `npm run web:brand-assets` é o modo padrão e falha quando os binários oficiais não existem.
- `VALORA_ALLOW_MISSING_BRAND_ASSETS=true npm run web:brand-assets` é o modo diagnóstico; ele registra a pendência manual sem falhar por ausência dos JPEGs, mas continua bloqueando `VG` como marca final, logo externa, secrets, service account e paths inseguros.
- `npm run web:visual-homologation` valida layouts, fallback, documentação, checklist, Home, diagnóstico, resultado, segurança e ausência de logo externa.

Consulte `VALORA_BRAND_ASSETS_MANUAL_SETUP.md` para nomes obrigatórios, testes locais e commit manual dos binários.

## RC2 visual — Valora Brand Assets

- A etapa visual RC2 usa os paths oficiais `/img/brand/valora-logo-full.jpeg` e `/img/brand/valora-symbol.jpeg` no projeto `backend/Valora.Web`.
- Os binários reais da marca devem ser incluídos manualmente; o Codex não cria, converte nem anexa imagens oficiais.
- Enquanto os JPEGs não estiverem versionados, as telas públicas e administrativas exibem fallback premium textual `Valora Group`, sem `VG`/`V` solto e sem imagem quebrada.
- Validação obrigatória após inclusão dos assets: `npm run web:brand-assets`.
- Validação diagnóstica sem binários: `VALORA_ALLOW_MISSING_BRAND_ASSETS=true npm run web:brand-assets`.
- Readiness visual RC2: `npm run web:rc2-visual-readiness`.
- A homologação final ainda deve ser executada com .NET SDK, PostgreSQL e navegador real para validar desktop/mobile antes do pacote `0.9.0-rc2`.

## Sprint Valora Insight™ — correções de feedback do cliente
- Produto público padronizado como `Valora Insight™`.
- Menu público deve exibir `Início`, nunca `HOME`.
- WhatsApp oficial: `+55 91 99254-5353` / `https://wa.me/5591992545353`.
- Contato público: `Fale com a Valora Group`.
- Resultado público com data segura, fallback `Data não informada`, layout mobile sem scroll horizontal e CTAs empilhados.
- Certificado/relatório com CSS de impressão compacto em `backend/Valora.Web/wwwroot/css/valora-print.css`.
- Validação: `npm run web:client-feedback-fixes`.

## Compatibilidade oficial do `scriptbd_completo.sql`

O bootstrap PostgreSQL oficial deve ser validado com `npm run db:scriptbd-validate` antes de uso em local, homologação ou produção. A seção `-- COMPATIBILIDADE PARA BANCOS EXISTENTES` em `scriptbd_completo.sql` normaliza schemas antigos sem `DROP TABLE` destrutivo de tabelas de negócio, incluindo `plan_limits.users`, `plans.monthly_price`, `organizations.plan_code`, contratos de formulários/perguntas/opções, `email_templates.body_html/body_text` e o índice de `usage_monthly(period_month)`. Consulte `SCRIPTBD_COMPLETO_COMPATIBILITY_GUIDE.md` para o procedimento completo.

## Atualização premium SaaS
Inclui redesign visual premium Valora Insight™, dashboard executivo, sidebar/topbar SaaS, certificado/devolutiva refinados e reforço de compatibilidade SQL para bancos antigos.
