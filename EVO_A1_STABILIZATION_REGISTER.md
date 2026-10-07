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

## 8. Candidatos de auditoria registrados (triagem pendente)

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

## 9. Próximos passos

1. ~~**A2** — corrigir `EvolutionAsync` + seleção determinística (ver item 5 — FECHADO).~~
2. ~~**A4** — navegação/rastreabilidade OrganizationalIntelligence × Action Center + D7 (ver item 6 — FECHADO).~~
3. ~~**B1** — comparação de evolução exposta a partir de snapshots imutáveis (ver item 7 — FECHADO; 479/479 + npm trio + smoke autenticado ao vivo incl. cenário reversível de duas avaliações).~~
4. **B2–B4** — passo a passo do workspace (situação/evidência/impedimento/próxima ação por estado persistido), ciclo fechado insight→plano→atividades→evidência→reevaluação com learning, e validação visual em 3 viewports (1440×900, 768×1024, 390×844) + bateria de gates a cada mudança.
5. Ao fim da sessão: dropar `valora_evo_scratch`.
