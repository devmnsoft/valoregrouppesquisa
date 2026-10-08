# Registro de Estabilização — Valora Insight (Bloco A + B1)

Baseline: `main` @ `f39d5a7591c332bc0fa39bd8600ac9adb784566f`.
Ambiente local: .NET SDK 10.0.400 (`global.json` 10.0.100 / rollForward latestFeature), PostgreSQL 18 `:5432`, banco `valora_evo_test`, schema `valorapesquisa`, scratch `valora_evo_scratch`.
Escopo: evolução apenas em `backend/` (ASP.NET Core 10); a raiz permanece legado JS/Firebase até cutover.

## 1. Gammas executadas (evidência real)

| Gama | Resultado | Observação |
|---|---|---|
| `dotnet test Valora.sln -c Release` | **460/460** | 458 sem variável de ambiente + 2/2 regressões PostgreSQL com `VALORA_TEST_POSTGRES_CONNECTION` (sem ela, os 2 saltam com `$XunitDynamicSkip$`, não são falhas funcionais) |
| `npm run test:surveys-web` | 4/4 | baseline |
| `npm run repository:boundaries` | 3/3 | baseline |
| `npm run web:premium-layout` | ok | baseline |
| Apply limpo de `script_completo.sql` (`ON_ERROR_STOP=1`) em banco novo | ok | `valora_evo_scratch`: 751 tabelas; `schema_migrations` com todos os blocos incl. os 6 de drift; `ux_evidence_response_question_concept` com predicado `deleted_at IS NULL AND response_id IS NOT NULL` |
| Jornada funcional 2 tenants (sessão BFF autenticada) | ok | smoke 2 tenants + matriz de isolamento cross-org (fecham D4/D5) |
| Health pós-rebuild | ok | `GET /health` e `GET /health/database` 200 (`database:ok`); BFF `GET /health/web/api` 200 `status:ok` |

Conexão usada nas regressões PostgreSQL: `Host=127.0.0.1;Port=5432;Database=valora_evo_test;Username=postgres;Password=123456;Search Path=valorapesquisa,public`.

## 2. Defeitos de código identificados e corrigidos

### EVD-1 — `LogStageAsync` com parâmetro `@status` não vinculado (PG 42703)
- **Sintoma:** erro 42703 no registro de cada estágio do pipeline; a persistência da cadeia de estágios estava morta silenciosamente.
- **Raiz:** `Valora.Infrastructure/Repositories/IntelligenceProcessingJobRepository.cs` — `LogStageAsync` referenciava `@status` sem vinculação, colidindo com a coluna `status`.
- **Correção:** parâmetro renomeado para `@StatusValue` (SQL + parâmetro).
- **Evidência ao vivo:** 45 linhas de stage-run com `status` vinculado em 4 jobs terminais (valores `completed` e `skipped`).

### EVD-2 — Predicado do arbítrio em `ON CONFLICT` da extração de evidência (PG 42P10)
- **Sintoma:** todo job `response_received` caía em dead-letter após 3 tentativas; o envelope amigável (`PIPELINE_DEAD_LETTER`) mascarava o erro real.
- **Raiz:** `Valora.Infrastructure/Repositories/IntelligencePipelineRepository.cs` (`ExtractResponseEvidenceAsync`, L36) usava arbítrio `ON CONFLICT(response_id,question_id,concept_code) WHERE deleted_at IS NULL`, mas o índice canônico `ux_evidence_response_question_concept` (`script_completo.sql` L1994) exige `AND response_id IS NOT NULL` → `42P10 infer_arbiter_indexes` em qualquer instalação nova.
- **Correção:** arbítrio alinhado ao índice canônico. Conservativa: `response_id` recebe sempre `r.id` (nunca nulo); ocorrência única do SQL no código (nenhum contrato de teste fixava a forma antiga).
- **Evidência ao vivo:** 3 jobs por resposta concluídos (26 evidências cada; 78 no nível do survey); cadeia completa de 9 estágios; teste de drift de peso abaixo PASS.

## 3. Drift de schema — decisão D6 (SCH)

Decisão: **alinhamento aditivo** do schema às expectativas do código do fluxo legado (Plan A) — sem migração de dados e sem alterar nomes já consumidos. Sete blocos versionados idempotentes aplicados **ao vivo** no `valora_evo_test` **e espelhados** no `script_completo.sql`:

1. `2026_09_survey_admin_public_columns`
2. `2026_09_survey_flow_lookup_columns`
3. `2026_09_survey_lifecycle_flags`
4. `2026_10_result_scores_columns`
5. `2026_10_dimension_scores_nullable_linkage`
6. `2026_10_submit_path_columns`
7. `2026_10_intelligent_alerts_decision_columns` (detecção **D7**; ver item 6)

Dois convênios de soft-delete coexistem de propósito (legado `is_deleted` × modelo estruturado `deleted_at`); o alinhamento seguiu os nomes do legado.

**Resíduo fora do caminho A1 (registrado, não corrigido):** família certificados — `CertificateOperationalRepository.GetAsync` espera colunas ausentes ao vivo (`survey_id`, `participant_email_masked`, `company_name`, `total_score`, `level`, `validation_url`, `payload_json`); visualizador admin/FreeDiagnostics espera `email_jobs.resend_count/last_resend_at/reviewed_at` + `generated_reports`; FreeDiagnostics lê `rs.maturity_level` mas o escritor grava `maturity_label`. Nenhum desses pontos está na jornada A1; decisão de alinhamento fica para o workstream que os exercita.

## 4. Invariantes verificadas ao vivo (A1)

Jornada: survey `3fa18f08-78de-4651-867f-034083fe7557` ("Jornada EVO A1"), org Tenant A.

| Invariante | Evidência |
|---|---|
| Snapshot metodológico imutável | 78 linhas de evidência, todas `mapped`, exatamente 1 hash distinto `d28023ef…fe86434` = hash da linha `surveys`; versão fixada `7a7843f8-2d2d-4e05-acb5-b014f509a286`; captura pelo trigger `trg_capture_survey_methodology_snapshot` (draft→active) com guarda de imutabilidade |
| Reprocessamento não reinterpreta | Drift de peso: linha global do catálogo oficial 1.0000→42.4242 → novo processamento → `source_hash` **idêntico** nos 5 métricos + 5 índices entre a execução pré-drift (`09a08ef7…`) e pós-drift; peso da evidência manteve-se 1.0000; drift revertido |
| Retry = tentativa, não nova operação | Job `9279c580-6432-4491-a615-a7285207f1d9` com falha injetada: `attempts=2`, mesmo `id` de operação, histórico de estágios duplicado **dentro** da mesma operação, terminal `completed`, `error_code` limpo após sucesso |
| Concorrência idempotente | 2 `POST /workspace/process` simultâneos → ambos 200 "Já existe um processamento em andamento" com `jobId=null`; nenhuma duplicata (chave parcial ativa do pipeline) |
| Reprocessar = nova operação, história preservada | `manual_recalculation` `09a08ef7…` e reprocesso de drift `e76fe5d5…` criaram operações novas sem apagar as anteriores |
| Cadeia de estágios | 9 estágios por job (evidence→metrics→indices→inference→insights→heatmap→radar→evolution→benchmark); métricas/índices `calculated` (metric-culture/governance/growth/leadership/people; ICO/IGO/ILI/IMO/IPO) |

Mapeamento dos defeitos do auditor original: **D2** = ausência de semente/demo no ambiente (resolvido por registro, item OBS-10); **D6** = família de drift de schema (item 3). D1/D3/D4/D5 foram fechados durante a estabilização do Bloco A (evidência: smoke BFF 2 tenants, matriz de isolamento cross-org e jornada autenticada persistente).

## 5. A2 — Correção da classificação de evolução (FECHADO)

**Problema:** `OrganizationalIntelligenceService.EvolutionAsync` classificava qualquer variação ≥ ±2 entre linhas consecutivas de `organizational_intelligence_runs` como evolução/regressão sem verificar se as duas execuções mediram a mesma população. Reprocessamentos que captaram respostas novas (contagem de evidências alterada) geravam pontos falsos de "evolução"; a ordenação era apenas por `created_at`, sem desempate (não determinística em timestamps iguais).

**Correção (3 arquivos + 1 suíte nova):**
- `Valora.Application/OrganizationalIntelligence/OrganizationalIntelligenceService.cs` — `EvolutionAsync`: ordem total `(created_at, id)`; desduplicação de medições consecutivas idênticas (mesmo `maturity_index` e mesmo `evidence_count` = reprocessamento da mesma medição, não novo ponto temporal); **gate de comparabilidade**: populações diferentes (`evidence_count` divergente) → classificação `not_comparable` + `Limitation` explicativa, mantendo o intervalo observado sem afirmar direção; limiares existentes preservados sem alteração para pares comparáveis (≥2 evolução, ≤−2 regressão, <0,5 estagnação, estável).
- `EvolutionPointDto`: membro aditivo `string? Limitation = null` (compatível em JSON; o renderer web `intelligence-page.js` já lia `x.limitation` com fallback).
- `Valora.Infrastructure/Repositories/OrganizationalIntelligenceRepository.cs` — `ListRunsAsync`: `ORDER BY created_at DESC, id DESC` (ordem total também no nível do repositório).
- `Valora.Tests/EvolutionSelectionTests.cs` — 5 testes de contrato: deduplicação de repetição; mudança de população → `not_comparable` + limitação; limiares preservados; determinismo em timestamps iguais (independente da ordem devolvida pelo repositório); execuções sem maturidade ignoradas sem deslocar a série; `SaveCalls == 0` fixa o caráter somente-leitura (histórico nunca apagado/reescrito).

**Evidência:**
- Suíte completa: **465/465** (`dotnet test Valora.sln -c Release` + regressões PostgreSQL com `VALORA_TEST_POSTGRES_CONNECTION`).
- Ao vivo (Tenant A): 2× `POST /intelligence/generate` (população idêntica) → runs 0→2 (histórico preservado) e série de evolução sem crescimento por reprocessamento.
- Ao vivo com 4 execuções avaliadas semeadas (maturidades 68/71/71/74; evidências 50/60/60/60): série = `baseline(68)` → `not_comparable(71, Δ+3, "População de evidências diferente entre as medições (50 → 60)…" )` [a execução repetida foi desduplicada] → `evolution(74, Δ+3)`; linhas de semente removidas em seguida (restam 0) e série voltou ao estado anterior.

**Limitações registradas (não inventadas):** troca de composição com contagem líquida zero (exclusão + inclusão na mesma quantidade) não é distinguível nesta tabela e segue tratada como comparável; versão de metodologia não é rastreada nesta tabela legada — `HasSufficientHistory` continua `false` e `EstimatedNextCycle` continua `null`.

## 6. A4 — OrganizationalIntelligence × Action Center: navegação/rastreabilidade (FECHADO)

**Auditoria (duas famílias confirmadas):** Sistema-1 ("Valora Action™": `valora_actions` + kanban em `/Intelligence/Action`) e Sistema-2 (Action Center: `action_plans`/`action_items` + `/ActionCenter/*`). Defeito encontrado: o catálogo de navegação expunha o item "Plano de Ação" (`intelligence.actions`) apontando para `ActionPlans` — alias legado que sempre redireciona (302) para `/ActionCenter/Plans` (Sistema-2) — duplicando o item canônico "Planos de Ação" (`execution.plans`, mesmo destino) e deixando a superfície real do Sistema-1 fora do menu; o KPI "Ações atrasadas" do DecisionCenter contava `valora_actions` (Sistema-1) mas linkava para o Sistema-2.

**Correções aplicadas (só navegação/rastreabilidade — zero migração/exclusão de dados por semelhança):**
1. `Valora.Web/Navigation/NavigationCatalog.cs` — `intelligence.actions` redirecionado de `(ActionPlans)` para `(Intelligence, Action)`, renomeado "Valora Action™" (módulo `organizational_intelligence`). A permissão extra `organizational_intelligence.read` foi substituída pelo gate de módulo idêntico aos irmãos da seção — coerente com o acesso real da página (`[Authorize]` + middleware comercial); comportamento intencional registrado.
2. `Views/DecisionCenter/Index.cshtml` — KPI "Ações atrasadas" agora aponta para `/Intelligence/Action` com rótulo "Abrir Valora Action™" (mesma fonte que a contagem: `valora_actions`, `DecisionCenterRepository.cs` L12).
3. `Valora.Api/Controllers/WorkspaceController.cs` — 4 policies cruas `"action.read"`/`"action.manage"` → constantes `ValoraPermissions.Action.Read`/`.Manage` (única ocorrência crua no backend).
4. `script_completo.sql` — nomes de exibição das permissões `action.read`/`action.manage` unificados nos nomes canônicos ("Visualizar/Gerenciar Valora Action"): Bloco A ≡ Bloco B em qualquer ordem de execução; banco ao vivo já confirmava os nomes do Bloco B (`ON CONFLICT DO UPDATE`), logo sem UPDATE ao vivo necessário.
5. `script_completo.sql` — DDL de `journey_event_action_links` marcado **RESERVADO** (tabela morta: nenhum writer/reader no código; mantida por compatibilidade histórica, sem drop).
6. `Valora.Tests/NavigationRegressionTests.cs` — guarda nova: o catálogo não pode expor destino-alias (controllers `ActionPlans`/`ValoraAction`) e deve expor `(ActionCenter, Plans)` exatamente uma vez.
7. **D7 (SCH)** — duas DDLs conflitantes de `intelligent_alerts` no `script_completo.sql` (L2191 forma System-1 × L3805 forma Decision Center); no apply limpo a L2191 vence o `CREATE TABLE IF NOT EXISTS` e o repositório do Decision Center lê colunas ausentes → 42703 em `source_type` em **qualquer instalação nova** (bloqueava a verificação do /DecisionCenter). Correção: bloco aditivo **`2026_10_intelligent_alerts_decision_columns`** — as 8 colunas lidas/escritas pelo código (`source_type`, `alert_type`, `title`, `message`, `evidence_summary`, `assigned_to_user_id`, `acknowledged_at`, `resolved_at`) + CHECK de severidade = união dos dois vocabulários já declarados no script (sem semântica inventada; defaults espelham colunas existentes `evidence_id`/`systemic_relation`; tabela vazia no ambiente). Ao vivo + espelhado + fresh-applied.

**Preservado propositalmente:** cadeia de alias `/OperationalIntelligence/ActionPlans` → `/ActionPlans` → `/ActionCenter/Plans` (pinned por `AccountHealthServiceTests`); links de intenção-criação para `/ActionPlans` em `Views/Decisions/Details.cshtml` e `Views/CommandCenter/Index.cshtml`; views mortas `Views/ActionPlans/Index.cshtml` + `wwwroot/js/pages/action-plans-page.js` (OBS-17).

**Evidência:**
- Suíte: **466/466** com `VALORA_TEST_POSTGRES_CONNECTION` (inclui a guarda nova).
- Gates npm: `test:surveys-web` 4/4 · `repository:boundaries` ok · `web:premium-layout` ✅.
- Apply limpo do `script_completo.sql` com o bloco novo (`ON_ERROR_STOP=1`, `valora_evo_scratch`): 751 tabelas; 8 colunas novas presentes; CHECK de severidade em união gravado; versão de migração registrada.
- Smoke autenticado ao vivo (Tenant A, sessão BFF): sidebar renderiza `Valora Action™` → `href="/Intelligence/Action"` (`data-navigation-code="intelligence.actions"`); "Planos de Ação" intacto (`execution.plans`); `href="/ActionPlans"` = 0 no menu; `/ActionPlans` → 302 → `/ActionCenter/Plans`; `/Intelligence/Action` → 200; **`/DecisionCenter` → 200** com KPI `href="/Intelligence/Action">Abrir Valora Action™` (new_link=1, old_label=0) — antes da correção D7 era 500 (42703 `source_type`).
- `SELECT_ORG=404 ORGANIZATION_NOT_FOUND` no smoke: não é defeito — o campo `organizationId` do BFF carrega o id do **cliente** (OBS-12); o contexto da org do Tenant A já é definido no login (Dashboard/KPIs escopados por org renderizam corretamente).

## 7. B1 — Comparação entre leituras de evolução a partir de snapshots imutáveis (FECHADO)

**Escopo:** expor a comparação ponto-a-ponto (linha de base × leitura atual) construída exclusivamente sobre dados persistidos/imutáveis da ativação — sem métrica inventada, sem campo de tendência favorável/desfavorável, variação absoluta apenas quando comparável.

**Decisões de projeto (conforme executado):**
- Unidade de comparação = **avaliação** (survey) com snapshot de metodologia capturado na transição draft→active (contrato `be4d791f`, preservado — zero alterações em snapshots/triggers).
- Sem alterações no BFF: o forwarder genérico já expõe `GET /bff/intelligence/evolution/comparison/options` e `GET /bff/intelligence/evolution/comparison` (query string passada integralmente — verificado ao vivo com e sem parâmetros).
- Endpoints em `OrganizationalIntelligenceController`: `GET evolution/comparison/options` (`Read<T>`) e `GET evolution/comparison` (`Validate`; id fora do conjunto elegível → null → 404 `INTELLIGENCE_EVOLUTION_COMPARISON_NOT_FOUND` com correlação). Permissão `"organizational_intelligence.read"`.
- Elegibilidade filtrada no servidor: org escopada, `deleted_at IS NULL`, `status='active'`, hash+JSON de snapshot não nulos, ordem total `(created_at, id)`; ids omitidos → baseline=mais antiga, current=mais recente; mesma avaliação nos dois lados → veredito "A mesma avaliação não pode ser usada como linha de base e leitura atual." (não é erro HTTP).
- Base comparável derivada apenas de dados congelados: versão metodológica do JSON do snapshot (`código@versão`; falha → "indefinida"); assinatura de dimensões (códigos ordenados ordinal); assinatura de escala (textos brutos dos nós restritos às dimensões medidas, dedup/sort); critérios de cálculo (linhas `dim|metric|index|weight|polarity`, nulos → `-`); população = SHA-256 hex dos identificadores agregados de participantes das respostas pontuadas.
- Maturidade por avaliação = média das pontuações por dimensão persistidas (NULL excluídas; nada persistido → `null` = não avaliado, nunca 0). Julgamento via `EvolutionComparisonService.Compare` estático (não modificado): unidade `"pontos da escala 0 a 100"` somente quando comparável; **nenhum** campo de tendência.
- DTOs versionados em contratos: `EvolutionComparisonSchemas.V1 = "evolution-comparison/v1"` (`schemaVersion` na resposta), serialização estável com strings pinadas em teste.
- UI dentro do card existente `#evolution` (hierarquia preservada): h3 "Comparação entre leituras", parágrafo "Como usar", dois selects (`data-baseline-select`/`data-current-select`) + botão `data-evolution-compare`, todos desabilitados até carregar opções; resultado em `data-evolution-comparison-result`; falha de carga das opções não quebra a renderização da página (try/catch independente); erro do compare mantém os valores selecionados e exibe a mensagem inline.

**Correção encontrada durante o smoke ao vivo:** `EvolutionCriterionRow.Polarity` declarada `int` contra coluna `smallint` (Dapper exige igualdade estrita de tipo em ctor posicional) → 500 `DATA_MATERIALIZATION_ERROR` nos três endpoints. Corrigido com cast `qs.polarity::int` no SQL (sem mudar tipos C#/DTO).

**Evidência:**
- Suíte: **479/479** com `VALORA_TEST_POSTGRES_CONNECTION` (466 anteriores + 13 novos em `Valora.Tests/EvolutionComparisonQueryTests.cs`: ordem/projeção de candidatos; seleção padrão comparável Δ+6,5 com limitação de causalidade e unidade; ids explícitos honrados preservando sinal (−6,5); id fora do conjunto → null/org vazia → vazio/null; auto-comparação; guarda de mesmo id; divergências de versão metodológica/dimensões/escala/população; lado sem pontuação → "Os resultados não estão disponíveis para comparação." + `dataAvailable:false`; serialização estável exata com `schemaVersion` pinned). Build `EXIT=0`.
- Gates npm: `test:surveys-web` 4/4 · `repository:boundaries` ok · `web:premium-layout` ✅ (S1=S2=S3=0). Sem mudança de schema → sem novo apply limpo necessário.
- Smoke autenticado ao vivo (Tenant A, JWT bruto na API + sessão BFF):
  - `options` (org explícita e implícita) → 200, 1 candidata "Jornada EVO A1" (`capturedAt` do snapshot, `scoredResponseCount:3`, `maturityIndex:null` — as 3 respostas têm `result_scores` mas **zero** `dimension_scores` anexadas; estado honesto "não avaliado").
  - `comparison` padrão (única candidata) → 200 com guarda "A mesma avaliação…"; `methodologyVersion:"VALORA-2026.1@1"` lido do snapshot imutável; `dataAvailable:false` nos dois lados.
  - Id estrangeira → 404 `INTELLIGENCE_EVOLUTION_COMPARISON_NOT_FOUND`.
  - BFF: `GET /bff/intelligence/evolution/comparison/options?organizationId=…` e `comparison` (com e sem query string) → 200 com corpo passthrough; página `/Intelligence` renderiza todos os elementos novos (`data-evolution-comparison`, `data-baseline-select`, `data-current-select`, `data-evolution-compare`, `data-evolution-comparison-result`) com o contêiner legado `data-evolution` intacto.
  - **Cenário de duas avaliações (seed reversível):** survey snapshoted fechada `2da14c85` ativada temporariamente (UPDATE status → restored para `closed` no fim; `finally` garante restauração). Ao vivo: 2 candidatas na ordem `(created_at, id)`; seleção padrão oldest→newest; ids explícitos invertidos honrados; veredito "Os resultados não estão disponíveis para comparação." (verificação de disponibilidade antecede diferenças de base — `Compare` L16–17); pós-restauração o `options` volta a 1 candidata (estado de dados 100% revertido).
  - Caminho comparável + variação absoluta: coberto por testes unitários com strings exatas; o ambiente de trabalho não possui `dimension_scores` utilizáveis (ver OBS-18), então ao vivo o estado correto é o de indisponibilidade honesta.

## 8. Fechamento do Bloco A — Estabilização E2E do fluxo de formulários, contexto de organização e bootstrap web (FECHADO)

**Escopo:** tornar o caminho principal (login → contexto de organização → formulários → persistência) funcional sem erros de dependência, padronizar diálogos/confirmar, remover jQuery dos bootstraps, eliminar o redirect client-side para Login e deixar o pipeline verde.

### 8.1 A1 — Cadeia de dependência de formulários
- **Listagem de formulários 500:** ctor posicional de `FormListItemResponse` com parâmetros em ordem diferente do SELECT de 18 colunas (materialização Dapper falhava) → reordenado em `FormAdministrationContracts.cs`. Verificado ao vivo: `GET /bff/forms?page=1&pageSize=20` → **200**, `total=2` ("Formulário Block A"[draft] · "Pesquisa Oficial Valora"[active]).
- **POST /bff/forms 403:** `PermissionAuthorizationHandler` da API resolvia o código de permissão por lookup exato no banco, negando até para o admin global `admin_valora` sem grant granular → adicionado early-succeed em `IsGlobalAdministrator` em `Valora.Api/Authorization/PermissionAuthorization.cs` (espelha o handler do Web; o escopo de dados via `RequireOrganizationId()` ficou intacto). E2E ao vivo completo: diálogo de criação → confirmação padrão → `POST /bff/forms` **201** (`881080f3-4a75-4927-bd3c-0032be771fef`, rascunho "Formulário Block A") → Builder → listagem `total=2`. O artefato permanece em `valora_evo_test`.

### 8.2 Contexto de organização (admin_valora global) + persistência da seleção
- Decisão implementada e verificada ao vivo: `EffectiveOrganizationId = SelectedOrganizationId` — fallback para a home org só na **exibição** (`/bff/account/context`), nunca nas APIs escopadas.
- Cadeia: `saas_customers UNIQUE(organization_id)`; `SaasCustomerService.FindByOrganizationIdAsync`; fallback por org-id em `SaasCustomersController.SelectContext`; `GET /bff/auth/organizations` (fonte do picker); picker em `_Topbar.cshtml` + `topbar-shell.js initOrganizationPicker` (+ CSS).
- **Refresh preservando a seleção:** `RefreshCoreAsync` (`BffAuthenticationService.cs`) reconstruía o cookie no refresh sem `SelectedOrganizationId` → 403 `Organização não selecionada` em `/bff/forms`; a seleção agora é transportada tanto para `safe` quanto para `result`.
- E2E ao vivo: o picker lista as 2 orgs acessíveis (`Tenant B Group` / `Valora Group`); seleção → `POST /bff/auth/select-organization` **200** + log `BFF organization context changed … OrganizationId=1d48de43…`; pós-reload: `/bff/surveys` 200 e `/bff/forms` 200 (ambos 403 antes da seleção). Nota: `WRN Contexto de organização ausente no Web` é comportamento esperado pré-seleção, não defeito.

### 8.3 Bootstrap web: remoção do jQuery + versionamento (`?v=`) + gate de autenticação
- jQuery removido dos bootstraps de `_Layout.cshtml`/`_AdminLayout.cshtml`: `guards.js` e `app.js` em JS puro (sem mais `Uncaught ReferenceError: $ is not defined`); `node --check` PASS nos dois; `app.js` retém apenas o `#environmentBadge`; logout vinculado exclusivamente em `topbar-shell.js:171`; parametrizações legadas `$.param` convertidas para `URLSearchParams` (`responses-api.js`, `audit-api.js`).
- `asp-append-version="true"` em `guards.js` e `app.js` nos dois layouts (causa raiz dos erros de console persistentes: views pré-compiladas + cache estático desatualizado). Verificado ao vivo: `guards.js?v=wQqQqBYpz3IIK9fod-kueOpaBjYsaH51QMxvjtoBnVE` e `app.js?v=P3DLYq185W7o-KLhh1bh_bv6uxyI8h4ueBk7AJNj0yE` servidos em `/Forms` e `/Dashboard`.
- **Redirect 302 para Login (client-side) — fechado:** a reescrita em JS puro do `guards.js` reativou o predicado `requireAuth` lendo `Session.token()` — stub da era BFF (`auth-session.js`, `token()` sempre nulo) — e nenhum view renderizava `data-authenticated` → todas as páginas autenticadas redirecionavam para Login mesmo com cookie BFF válido. Correção: `<body data-authenticated="@(User.Identity?.IsAuthenticated)">` nos dois layouts + predicado agora prefere `Session.isAuthenticated()` (marca no body) com fallback para `token()`. Verificado ao vivo: `/Dashboard` e `/Forms` seguram com `data-authenticated="true"` e **zero** erros de console; sessão expirada → 401 `SESSION_EXPIRED` do servidor → `/Account/Login?reason=session-expired&returnUrl=…` → relogin pela UI aterrissa em `/Dashboard`.

### 8.4 A3 — Ciclo de vida de diálogos/confirmações (prova ao vivo)
- Contrato verificado em `valora-ui.js`: `[data-confirmation-modal]` único global (HTMLDialogElement); `[data-confirm-proceed]` → `true`; qualquer fechamento (Esc/backdrop/programático) → `false` (todos os caminhos convergem no listener `close`).
- Teste ao vivo "Arquivar 'Formulário Block A' → cancelar": `modalCount=1`; mensagem interpola o nome corretamente; **zero** chamadas em `/bff/forms` capturadas após o cancelamento (spy sobre `fetch`); linha, botão de arquivar e status preservados ("Rascunho") — sem mutação.
- `action-center.js`: todos os caminhos de fechamento (Esc→`cancel`, backdrop, `[data-dialog-close]`) passam por `requestClose` protegida por flag `closing` (garantia de prompt único — verificado por leitura de código, L26–30); usa o mesmo primitivo `ValoraUI.confirm` validado ao vivo acima. O teste ao vivo com diálogo sujo requer um plano existente → agregado ao Bloco C (C5).

### 8.5 Pipeline (pós-mudanças)
- `dotnet format whitespace Valora.sln --verify-no-changes` → **EXIT=0**.
- `dotnet build -c Release` (Web+API) → **EXIT=0**; a instância em execução (Web PID 41952 `:5088/:7088`) inclui as mudanças cshtml (hashes `?v=` novos servidos); API PID 36960 `:5080` intocada.
- `dotnet test Valora.Tests -c Debug` com `VALORA_TEST_POSTGRES_CONNECTION` → **479/479 aprovados, 0 falhas**.

### 8.6 Limitações herdadas
- **Capturas de tela:** `browser.screenshot` exige aba visível; a superfície permanece `visibilityState:"hidden"` mesmo com janela do OpenCode elevada no SO + tab focado (Chromium embutido no Electron). A evidência funcional está completa sem as capturas → as capturas reais de 3 viewports migram para a passagem de captura do Bloco D.
- **Sobrevivência de refresh de sessão — FECHADO (PASS, 2026-10-07):** bateria `b2_refresh_check.ps1` em sessão própria (curl): T0 14:15:58 → LOGIN=200, SELECT_ORG=200; `GET /bff/forms` aos 14 min (`AT_14MIN_BFF_FORMS`) = **200** e aos 14:45 min (`AT_14MIN45S_BFF_FORMS`) = **200**, atravessando a renovação automática do access token (~15 min). `web_out.log`: **3 renovações** registradas na janela — `[14:30:05 INF] BFF access token renewed. SessionId=f692dbc9-92f6-4866-8608-2c580a05dc5b UserId=9f1e8a01-… OrganizationId=1d48de43-3bbb-4827-9951-83cd1e6dc4cb` — confirmando o fix do Bloco A de que `RefreshCoreAsync` preserva `SelectedOrganizationId` no refresh em janela real (>15 min).

## 9. Candidatos de auditoria registrados (triagem pendente)

| Id | Achado | Impacto |
|---|---|---|
| OBS-1 | Gateamento de permissões: Tenant B `empresa_admin` exibe `permissions:[]`, `enabledModules:[]`, `capabilities:[]` apesar das grants de papel (reconfirmado ao vivo) | UX/autorização visível ao usuário |
| OBS-2 | Códigos de permissão semeados sem grant (`saas_customers.read`, `roles.read`) | Consistência RBAC |
| OBS-3 | Senhas divergentes entre documentos/semente (`Valora!12345` × `Valora@123456`) | Onboarding local |
| OBS-4 | Cookie de sessão BFF ~22,6 KB | Revisar conteúdo do payload |
| OBS-5 | `POST /bff/auth/refresh` retorna 500 cru em falha | Envelope de erro inconsistente |
| OBS-6 | FK `audit_logs`→`organizations` | Restringe exclusão de org |
| OBS-7 | `appsettings` do `Valora.Web` sem conexão Postgres (fornecida por variável no runner local) | Risco de drift de configuração |
| OBS-8 | Drift de rota BFF: `/bff/organization/{*}` → 404 vs API `/api/v1/organization/{resource}` | Endpoint morto via BFF |
| OBS-9 | Mapeamentos de erro confirmados: 42703→500 `DATABASE_SCHEMA_MISMATCH`; 23502/23505→503 `DATABASE_UNAVAILABLE` (23505 cru vira 503, não 409 amigável); materialização Dapper→500 `DATA_MATERIALIZATION_ERROR`; 42P10→`PIPELINE_DEAD_LETTER` genérico (sem mapeamento dedicado) | Diagnóstico de incidentes |
| OBS-10 | Semente de demo ausente; `/e2e/fixture` provisionado externamente (resolver D2) | Reprodutibilidade do ambiente |
| OBS-11 | Vazamento do pipeline: linhas `evolution-{runId}` escritas pelo pipeline em `evolution_cycles` (governança) entram na `List` dos ciclos humanos; hardcode `firstCycle = module == "evolution"` (~L176 `RefreshProjectionAsync`) | Listagem de ciclos de evolução |
| OBS-12 | Nome do contrato BFF: `SelectOrganizationRequest.organizationId` carrega na prática o id do **cliente** SaaS (`POST /api/v1/saas/customers/{id}/select-context`) | Confusão de contrato |
| OBS-13 | Nomes de exibição misturados para códigos somente-Bloco-A (`action.approve`="Aprovar Action", `action.complete`="Concluir Action", `action.comments.manage`="Gerenciar comentários de Action"); string crua `"action.read"` em `AdministrationController.cs` L26 (metadado de exibição, não policy) | Consistência do catálogo de permissões |
| OBS-14 | Rótulo duplicado "Organizações" ×2 na seção administrativa da navegação | Navegação |
| OBS-15 | Nenhum writer `INSERT INTO intelligent_alerts` no backend atual (existem leituras e o `UPDATE` de acknowledge/resolve) — a tabela permanece vazia até existir produtor; seção de alertas do DecisionCenter renderiza vazia, KPI crítico = 0 | Dados de alertas |
| OBS-16 | Inconsistência entitlement × permissão: `CommercialModuleAccessMiddleware` gateia `/ActionCenter`+`/ActionPlans` sob módulo `action_center`, enquanto as seeds de permissão usam `module_code='organizational_intelligence'` (corrigir mudaria comportamento de entitlement — fora de escopo) | Acesso |
| OBS-17 | Views mortas `Views/ActionPlans/Index.cshtml` + `wwwroot/js/pages/action-plans-page.js` (o controller sempre redireciona; tracked desde `905a93f0`) — mantidas | Higiene do código |
| OBS-18 | Estado de dados do ambiente de trabalho: as 3 respostas da jornada têm `result_scores` mas **zero** `dimension_scores` anexadas; as 30 linhas existentes de `dimension_scores` em `valora_evo_test` referenciam `result_score_id` inexistentes (linhas antigas dos drifts `2026_10_result_scores_columns`/`2026_10_dimension_scores_nullable_linkage`) apesar da FK existir — a etapa de pontuação por dimensão não produziu linhas vinculadas às avaliações atuais. Consequência: toda leitura de maturidade comparável é `null` ao vivo (B1 exibe "não disponíveis" honestamente; `/evolution` legado usa evidências e não é afetado) | Completude de dados derivadas no ambiente local |

## 10. Próximos passos

1. ~~**A2** — corrigir `EvolutionAsync` + seleção determinística (ver item 5 — FECHADO).~~
2. ~~**A4** — navegação/rastreabilidade OrganizationalIntelligence × Action Center + D7 (ver item 6 — FECHADO).~~
3. ~~**B1** — comparação de evolução exposta a partir de snapshots imutáveis (ver item 7 — FECHADO; 479/479 + npm trio + smoke autenticado ao vivo incl. cenário reversível de duas avaliações).~~
4. ~~**Bloco A** — estabilização E2E de formulários, contexto de organização e bootstrap web (ver §8 — FECHADO; pipeline verde 479/479).~~
5. ~~**Bloco B** — consolidação de navegação: 7 áreas do cliente + ADMINISTRAÇÃO GLOBAL (ver §11 — FECHADO; 169→165 itens, 4 remoções registradas, 6 aliases GET seguros, pipeline 480/480, validação ao vivo 48/48 asserts + browser com console limpo).~~
6. **Bloco C** — CRUDs reais C1–C6 (org/acessos, ciclo de vida de formulários, diagnóstico/coleta, resultados, planos de ação, padrões de formulários), incluindo o teste de diálogo sujo ao vivo com plano real (C5).
7. **Bloco D** — cascata de layout + validação em 3 viewports (390×844 / 768×1024 / 1440×900) com capturas reais + contraste/acessibilidade (inclui a passagem de capturas herdada em §8.6).
8. **B2–B4** — passo a passo do workspace (situação/evidência/impedimento/próxima ação por estado persistido), ciclo fechado insight→plano→atividades→evidência→reevaluação com learning, e bateria de gates a cada mudança.
9. ~~Checagem final: sobrevivência de refresh de sessão >15 min (§8.6) — FECHADO: `AT_14MIN_BFF_FORMS=200`, `AT_14MIN45S_BFF_FORMS=200`, 3 renovações em `web_out.log` com `OrganizationId=1d48de43-…` preservado (ver §8.6).~~
10. Ao fim da sessão: dropar `valora_evo_scratch`.

## 11. Bloco B — Consolidação de navegação (FECHADO)

Baseline `aeb31bfe`. Escopo executado: taxonomia de 7 áreas do cliente + ADMINISTRAÇÃO GLOBAL (confirmada pelo usuário), reuso canônico de URLs, remoções registradas, aliases apenas como redirect GET seguro. Sem mudança de autorização: papéis/permissões/escopos de cada item mantidos como em HEAD (menu oculto ≠ autorização — a visibilidade é UX; o gate server-side é inalterado).

### 11.1 Taxonomia final (códigos exatos)

| Ordem | Código da seção | Rótulo (exato) |
|---|---|---|
| 10 | `executive` | "Visão Executiva" |
| 20 | `diagnostics` | "Diagnóstico" |
| 30 | `results` | "Resultados" |
| 40 | `intelligence` | "Inteligência" |
| 50 | `execution` | "Execução & Evolução" |
| 60 | `governance` | "Risco & Governança" |
| 70 | `organization` | "Organização & Segurança" |
| 90 | `global-administration` | "ADMINISTRAÇÃO GLOBAL" |

### 11.2 Menu ANTES (169 itens, 15 seções — git HEAD)

- `valora` "Admin Valora" (1): Visão Geral Valora (`valora.overview`)
- `workspace` "Visão Geral" (3): Workspace, Meu Dia, Busca Global
- `executive` "Visão Executiva" (3): Visão Geral, Prioridades, Cockpit Executivo
- `diagnostics` "Diagnóstico" (8): Onboarding, Novo Diagnóstico, Ciclos Diagnósticos, Templates, Formulários, Pesquisas, Campanhas, Respostas
- `methodology` "Metodologia Valora" (13): Visão Geral, Versões, Dimensões, Dicionário Cognitivo, Mapa Cognitivo, Índices, Perguntas Oficiais, Prompts IA, Guardrails, Validação, Templates, Scoring, Recomendações
- `intelligence` "Inteligência" (34): People, Teams, Culture, Engagement, Competencies, Development Plans, Risks, Knowledge Center, Valora Advisor™, Architecture Studio, Resultados, Inteligência Organizacional, Gerar análise IA, Revisão de IA, Mapeamento Metodológico, Centro de Processamento, Evidências, **Centro de Evidências** (`intelligence.evidence-center` — removido), Metrics™, Índices Valora™, Motor de Inferência, Insights IA™, Radar™, Heatmap™, Benchmark, Executive Reports™, One-on-One™, Lideranças, Comparativos, Evolução Histórica, Recomendações, Valora Action™, Relatórios, Certificados
- `execution-evolution` "Execução e evolução" (19): Benchmarks, Compare, Cohorts, Insights comparativos, Privacy (5); Processes, Definitions, Builder, Instances, Approvals, SLA, Insights, Templates (8); Central de ações, Planos de Ação, Ações, Evolução, Ciclos de Evolução, Histórico (6)
- `risk-compliance` "Risk & Compliance" (8): Risk & Compliance, Risks, Heatmap, Controls, Compliance, Non-Conformities, Mitigation Plans, Audits
- `decision-governance` "Governança e Decisão" (6): Decision Center, Alertas Inteligentes, Decisões, Indicadores, Ciclos de Governança, Reuniões de Governança
- `administration` "Administração" (46): Admin Hub, Solution Packs ×4; SaaS Admin, Clientes, Cobrança, Módulos, Auditoria SaaS; master: Visão Geral, **Organizações** (`master.organizations` — removido), Planos e Assinaturas, Feature Flags, Diagnósticos, Questionários e Perguntas, Respostas, Resultados, Relatórios, Certificados, Inteligência Organizacional, Benchmark, One-on-One, Notificações, Auditoria, Jobs e Processamentos, Logs do Sistema, Configurações, Aparência e Marca, Suporte; security ×9 (Segurança e Compliance, Privacidade e LGPD, Solicitações de Titulares, Retenção de Dados, Auditoria de Compliance, Incidentes de Segurança, Revisão de Acessos, Acessos Sensíveis, Chaves de API); Estrutura Organizacional, Públicos e Segmentações, **Organizações** (`administration.organizations` — removido), Usuários, Perfis e papéis, Permissões e acessos, Configuração de Benchmark
- `platform` "Plataforma" (16): Suporte (operação), Feedback, Customer Success, Métricas de Adoção, Onboarding, Solicitações Comerciais, Incidentes, Release Notes, Planos e Uso, Communication Center, Auditoria, Governança da Plataforma, Saúde do Sistema, Backup e Restore, Modo de Manutenção, Configurações
- `subscriptions` "Planos e Assinaturas" (3): Marketplace de Módulos, **Minha Assinatura** (`subscriptions.current` — removido), Limites e Uso
- `data` "Dados" (1): Data Hub
- `customer-success` "Suporte / Success Center" (7): Visão Geral, Onboarding, Saúde da Conta, Chamados, Base de Conhecimento, Playbooks, Uso do Produto
- `support` "Suporte" (1): Ajuda (`support.help`)

### 11.3 Menu DEPOIS (165 itens, 8 seções — `NavigationCatalog.cs` reescrito)

- `executive` "Visão Executiva" (7): Visão Geral Valora, Visão Geral, Workspace, Meu Dia, Prioridades, Cockpit Executivo, Busca Global *(consolida as antigas `valora` + `workspace` + `executive`)*
- `diagnostics` "Diagnóstico" (9): Onboarding, Novo Diagnóstico, Ciclos Diagnósticos, Formulários, Pesquisas, Campanhas, **Públicos e Segmentações** (movido de `administration`), Respostas, Templates
- `results` "Resultados" (7): Resultados, Comparativos, Evolução Histórica, Recomendações, Executive Reports™, Relatórios, Certificados *(seção nova, extraída de `intelligence`)*
- `intelligence` "Inteligência" (25): People ×7, Knowledge Center, Valora Advisor™, Architecture Studio, Inteligência Organizacional, Gerar análise IA, Revisão de IA, Mapeamento Metodológico, Centro de Processamento, Evidências, Metrics™, Índices Valora™, Motor de Inferência, Insights IA™, Radar™, Heatmap™, Benchmark, One-on-One™, Lideranças *(sem `evidence-center`; itens de resultados extraídos)*
- `execution` "Execução & Evolução" (7): Central de ações, Planos de Ação, Ações, Valora Action™, Evolução, Ciclos de Evolução, Histórico *(rótulo "Execução e evolução" → "Execução & Evolução"; Benchmarks/Processes movidos para `governance`)*
- `governance` "Risco & Governança" (27): risk ×8, Decision Center ×6 (Decision Center, Alertas Inteligentes, Decisões, Indicadores, Ciclos de Governança, Reuniões de Governança), Benchmarks ×5, Processes ×8 *(consolida `risk-compliance` + `decision-governance` + blocos de benchmark/processo vindos de `execution-evolution`)*
- `organization` "Organização & Segurança" (20): Admin Hub, Estrutura Organizacional, Usuários, Perfis e papéis, Permissões e acessos, Configuração de Benchmark, security ×9, Planos e Uso, Communication Center, Auditoria, Configurações, **Ajuda** (`support.help`, movido da seção `support`)
- `global-administration` "ADMINISTRAÇÃO GLOBAL" (63): SaaS Admin, Clientes, Cobrança, Módulos, Auditoria SaaS; master ×19 (sem `master.organizations`); Metodologia ×13 (seção própria dissolvida); Solution Packs ×4; operação ×8; success ×7; Marketplace de Módulos + Limites e Uso (sem `subscriptions.current`); Governança da Plataforma, Saúde do Sistema, Backup e Restore, Modo de Manutenção; Data Hub

Totais conferidos por contagem direta: 169 (HEAD) → 165 (novo); diferença = exatamente as 4 remoções de §11.4.

### 11.4 Remoções do menu (exatamente 4)

| Código removido | Destino antigo | Rótulo | Fundamento / destino canônico |
|---|---|---|---|
| `administration.organizations` | `Enterprise.Organizations` (`/Enterprise/Organizations`) | Organizações | duplicava `master.organizations` e o Admin Hub (OBS-14); gestão de organizações é do Admin Hub |
| `master.organizations` | `Administration.Organizations` (`/Administration/Organizations`) | Organizações | mesma funcionalidade; canônica = Admin Hub `companies` (`/AdminValora?module=companies`) |
| `intelligence.evidence-center` | `OrganizationalCenters.Evidence` (`/Evidence`) | Centro de Evidências | reuso canônico de evidências = `Intelligence.Evidence` (`/Intelligence/Evidence`); endpoints `/Evidence/Details/{guid}` e `/Evidence/ByDiagnostic/{guid}` mantidos e 200 ao vivo |
| `subscriptions.current` | `Saas.Subscription` (`/Organization/MyPlan`, `/Platform/Subscriptions`) | Minha Assinatura | reuso canônico = `Saas.Marketplace` (`/Marketplace`) |

Nenhuma tabela foi removida (invariante do DB intacta); `valora_actions` continua separado de `action_plans`/`action_items`.

### 11.5 Correção de destino: `saas.customers` (renomeio método + view)

- O action `[HttpGet("Clients")]` de `SaasAdminController` estava no método `Customers` (desalinhamento rota↔metodologia detectado pelos testes de reflexão do catálogo).
- Corrigido por renomeio: método `Customers` → `Clients` (+ 2 refs por `nameof`) e view `Views/SaasAdmin/Customers.cshtml` → `Clients.cshtml` via `git mv` (**única mudança STAGED no índice git**; restante do trabalho fica uncommitted, baseline permanece `aeb31bfe`).
- Item `saas.customers` agora aponta para `SaasAdmin/Clients` = `/Admin/Clients` (200 ao vivo; link antigo `/SaasAdmin/Customers` ausente do menu verificado).

### 11.6 Desvio registrado: Admin Hub na área 7 (`organization`)

`admin.hub` ficou em "Organização & Segurança" (ordem 0 da seção), e não em `global-administration`. Motivo: `AdminHubController` autoriza `admin_valora` **e** `empresa_admin`, enquanto a seção global é exclusivamente `admin_valora`. Mantê-lo na área 7 preserva a semântica de escopo do hub (administração de empresa/tenant) e a segregação da administração de plataforma.

### 11.7 Aliases — redirects GET seguros aplicados neste bloco (6 URLs)

| URL legada | Status | Destino (302) | Implementação |
|---|---|---|---|
| `/Priorities` | 302 | `/Workspace/Priorities` | `OrganizationalCenters.Priorities() => Redirect(...)` |
| `/Evidence` | 302 | `/Intelligence/Evidence` | `OrganizationalCenters.Evidence()` |
| `/Indexes` | 302 | `/Intelligence/Indices` | `OrganizationalCenters.Indexes()` |
| `/Radar` | 302 | `/Intelligence/Radar` | `OrganizationalCenters.Radar()` |
| `/Administration/Organizations` | 302 | `/AdminValora?module=companies` | `AdministrationController.Organizations()` + rota literal `[HttpGet("Administration/Organizations")]` (necessária: `[HttpGet("Administration/{module}")]` sombriava a rota convencional — atributo > convencional, e segmento literal > parâmetro) |
| `/Organization/MyPlan` e `/Platform/Subscriptions` | 302 | `/Marketplace` | `Saas.Subscription()` (duas rotas no mesmo action) |

Todos com comentário `// Bloco B — alias GET seguro: ...`. **Aliases pré-existentes preservados (não alterados):** `/Support` → `/SuccessCenter/Support`; `/Support/Tickets` e `/Support/Tickets/{id}` → `/SuccessCenter/Support[/Details/{id}]` (`AssistedOperationsController`); `/AdminValora/Organizations` (endpoint real do hub em `AdminHubController`). **Endpoints de detalhe mantidos com dados:** `/Evidence/Details/{guid}`, `/Evidence/ByDiagnostic/{guid}`, `/Indexes/Details/{code}` — todos 200 ao vivo.

### 11.8 Arquivos alterados (Bloco B)

- `backend/Valora.Web/Navigation/NavigationCatalog.cs` — reescrito (8 seções/165 itens; cabeçalho com regra de reuso canônico e registro deste §11). Linhas de itens vertiginais ao HEAD exceto seção/ordem/destino do `saas.customers` (âncora de diff: `nav_orig.txt`).
- `backend/Valora.Web/Controllers/OrganizationalCentersController.cs` — 4 actions raiz → `Redirect`; endpoints de detalhe intocados.
- `backend/Valora.Web/Controllers/AdministrationController.cs` — `Organizations()` → `Redirect("/AdminValora?module=companies")` + rota literal.
- `backend/Valora.Web/Controllers/SaasController.cs` — `Subscription()` → `Redirect("/Marketplace")`; `Usage()` inalterado.
- `backend/Valora.Web/Controllers/SaasAdminController.cs` — método `Customers`→`Clients` (ver §11.5); view renomeada por `git mv`.
- `backend/Valora.Tests/NavigationRegressionTests.cs` — `AdminValoraReceivesTheCompleteNavigationWithoutTenantClaims` reescrita (8 rótulos exatos, 12 códigos obrigatórios, 4 removidos ausentes); novo Fact `LegacyAliasActionsRedirectToTheirCanonicalRoutes` (instancia controladores diretamente, incl. `SaasController(null!, NullLogger<…>.Instance)`).

### 11.9 Pipeline e evidência ao vivo

- `dotnet format whitespace Valora.sln --verify-no-changes` → **EXIT=0** (re-validado após o último edit — rota literal em `AdministrationController`).
- `dotnet build Valora.sln -c Release` → **EXIT=0** (0 erros, 8 avisos preexistentes CS8603/CS8604). Web reiniciada: **PID 17720** `:5088/:7088` (PID 46472 anterior stopado antes do build); API PID 36960 `:5080` intocada.
- `dotnet test Valora.Tests -c Debug` com `VALORA_TEST_POSTGRES_CONNECTION` → **480/480 aprovados, 0 falhas** (479 → 480 pelo novo Fact de aliases).
- **Validação HTTP ao vivo** (`b1_nav_verify.ps1`, login `admin.tenanta@valora.local` + seleção da org Valora Group): LOGIN/SELECT_ORG/DASHBOARD/FORMS = 200; **48/48 asserts PASS, zero FAIL**: 8 seções presentes nos dois layouts (Dashboard e Forms — sidebar compartilhado); 8 rótulos exatos conferidos por decodificação das entidades HTML numéricas emitidas pelo Razor (ex.: `Vis&#xE3;o Executiva` = "Visão Executiva" — artefato de codificação do probe inicial, não defeito da app); 4 códigos removidos ausentes; `saas.customers` presente apontando para `/Admin/Clients`; 6 aliases → 302 com location exata (normalização de URL absoluta do curl); 7 destinos canônicos → 200; 3 deep links de evidência/índices → 200.
- **Validação browser** (aba `tab_72b3b8fd-0292-4554-8f53-6292d6e01b1d`, login fresco 200 + select-org 200): `/Dashboard` — link ativo `executive.overview`; `/Forms` — link ativo `diagnostics.forms`; navegação renderizada = 165 itens × 2 instâncias (sidebar desktop + drawer mobile) = 330 nós; 8 seções × 2; códigos removidos ausentes; `saas.customers` → `/Admin/Clients`; **console errors = 0 nas duas páginas**. (Obs.: `/Dashboard` exibe 2 `h1` — encaminhado ao Bloco D, item "um único h1 por página".)

### 11.10 Efeitos colaterais resolvidos / pendências

- **OBS-14 RESOLVIDA** — os dois rótulos duplicados "Organizações" (`administration.organizations` e `master.organizations`) foram removidos do menu.
- **Checagem de sobreviência de refresh >15 min — PASS (2026-10-07):** `b2_refresh_check.ps1` (shell `sh_1175d57e4001WofaalDYNF0PVf`): T0 14:15:58 login+select-org 200; `GET /bff/forms` aos 14 min e 14:45 min = 200/200; 3 renovações em `web_out.log` (primeira 14:30:05) com `OrganizationId=1d48de43-…` preservado. Detalhe em §8.6.
