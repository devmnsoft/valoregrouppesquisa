# Matriz de formulários e operações — MVC/API

Atualização: 2026-09-15. Esta matriz cobre o lote **Forms/Builder** em profundidade e registra o inventário estático das demais superfícies; não transforma inspeção estática em homologação. A aplicação publicada pelo projeto ASP.NET está em `backend/Valora.Web` e usa a API em `backend/Valora.Api`. O `index.html` e `app.js` da raiz pertencem ao frontend Firebase legado e não são consumidores das views MVC.

## Inventário

- 78 views/partials MVC contêm formulário ou diálogo (108 tags `form` e 41 tags `dialog`). O comando reproduzível está na seção Evidências.
- Grupos encontrados: Account; ActionCenter; ActionPlans; AdminHub; Advisor; Architecture; AssistedOperations; Benchmarks; Certificates; CommandCenter; DecisionCenter; Evolution; Experience; Forms; FreeDiagnostics; Governance; Indicators; Insights; Intelligence; Journey; Knowledge; Lgpd; Methodology; Onboarding; OneOnOne; OperationalIntelligence; Organization; People; PublicPages; PublicSurvey; Reports; Results; Saas; SaasAdmin; SolutionPacks; SuccessCenter; Surveys; Users; Workspace; e componentes Shared.
- O frontend legado mantém CRUD administrativo Firebase/Cloud Functions documentado em `ADMIN_SURVEY_FORM_CRUD_AUDIT.md`; ele não foi confundido nem alterado neste lote.

## Matriz rastreável do lote Forms/Builder

| Módulo/rota | Interface e operação | Endpoint/verbo e contrato | Aplicação, repository e tabelas | Escopo/regras | Evidência e situação |
|---|---|---|---|---|---|
| Forms `/Forms` | `Views/Forms/Index.cshtml`, modal: criar | `POST /bff/forms` → `POST /api/v1/forms`; `CreateFormRequest` | `FormAdministrationService` → `FormAdministrationRepository`; `forms`, `form_versions`, `audit_logs` | autenticado; `Forms.Create`; organização explícita; nome, descrição, categoria e 1–480 minutos | Handler captura formulário e payload antes do primeiro `await`, bloqueia confirmação e envio duplicados, preserva campos/erro, exige identificador confirmado pelo servidor e só então abre o Builder. O cancelamento libera o botão. Teste comportamental com confirmação realmente assíncrona: **implementado e verificado; PostgreSQL bloqueado**. |
| Forms `/Forms` | listar, buscar, filtrar, atualizar | `GET /bff/forms?page={n}&pageSize=20`; `FormListQuery`/`FormListResponse` | serviço/repository; `forms`, `form_versions`, seções/perguntas | `Forms.Read`; organização; SQL parametrizado e ordenação estável | Busca, status e categoria são aplicados no servidor sob o mesmo escopo tenant do total; resposta traz navegação, métricas do filtro, catálogo completo autorizado e indicadores de uso atual/histórico/respostas. Ordenação usa `updated_at` e `id`. A UI preserva página/filtros e ignora respostas fora de ordem. **Implementado e verificado por testes locais; PostgreSQL bloqueado**. |
| Builder `/Forms/{id}/Builder` | consultar estrutura/preview | `GET /bff/forms/{id}`; `FormDetailResponse` | serviço/repository; versões, seções, perguntas e opções | `Forms.Read`; organização; 404 não revela outro tenant | Preview deriva da estrutura recém-consultada. **Verificado estaticamente; execução bloqueada**. |
| Builder | editar dados básicos | `PUT /bff/forms/{id}`; `UpdateFormRequest` | serviço/repository; `forms` | `Forms.Update`; versão esperada; limite e intervalo também no servidor | Autosave concorrente removido; submissão explícita e confirmada; valores permanecem no conflito. **Corrigido; banco bloqueado**. |
| Builder | criar/editar/arquivar seção | `POST/PUT/DELETE .../sections`; contratos tipados | serviço/repository; `form_section_versions`, `form_versions`, `audit_logs` | `Forms.Manage`; somente draft; organização e versão | Exclusão lógica e auditoria existentes, UI confirma uma vez. **Verificado estaticamente; banco bloqueado**. |
| Builder | criar/editar/arquivar pergunta | `POST/PUT/DELETE .../questions`; contratos tipados | serviço/repository; `question_versions`, versões/auditoria | `Forms.Manage`; draft; tipos allowlist; versão | Tipo visual `explanatory_text` incompatível foi substituído pelo tipo canônico `description`; mudança para tipo sem opções é rejeitada no SQL enquanto houver opções. **Corrigido; banco bloqueado**. |
| Builder | criar/editar/arquivar opção | `POST .../questions/{id}/options`; `PUT/DELETE .../options/{id}` | serviço/repository; `question_option_versions`, versões/auditoria | `Forms.Manage`; draft; pergunta do mesmo formulário/organização; versão; score 1–5 ou nulo | A ação de arquivar opção, antes ausente da UI, foi conectada. Backend impede opções em tipos incompatíveis. **Corrigido; banco bloqueado**. |
| Builder | reordenar seção | `POST /bff/forms/{id}/reorder`; `ReorderFormItemRequest` | serviço/repository; tabelas versionadas | `Forms.Manage`; draft, organização, versão | Botões Subir/Descer persistem e recarregam. **Verificado estaticamente; banco bloqueado**. |
| Forms/Builder | nova versão | `POST /bff/forms/{id}/versions`; `CreateFormVersionRequest` | repository transacional; cópia seções/perguntas/opções | somente publicada, versão esperada e rascunho único | A biblioteca informa a versão de origem, preserva a publicada e abre o novo rascunho; conflito orienta sobre rascunho existente. **Implementado; PostgreSQL bloqueado**. |
| Builder | publicar | `POST /bff/forms/{id}/publish`; `PublishFormVersionRequest` | serviço/repository; `form_versions`, `forms`, auditoria | `Forms.Publish`; draft válido; publicação torna versão imutável | Confirma consequência e só mostra sucesso após resposta. Falha permanece visível. **Verificado estaticamente; banco bloqueado**. |
| Forms | arquivar formulário | `DELETE /bff/forms/{id}`; `ArchiveFormRequest` | serviço/repository; `forms`, dependências/auditoria | `Forms.Archive`; versão e restrição de uso | Biblioteca confirma nome e consequência, preserva o histórico, envia token de concorrência e ajusta a última página. O servidor revalida o tenant/versão, bloqueia vínculos com coleta ativa e audita o ator; histórico isolado não bloqueia. **Implementado e verificado estaticamente; PostgreSQL bloqueado**. |

## Evidências executáveis e bloqueios

- Inventário: `rg -l '<form|<dialog' backend/Valora.Web/Views -g '*.cshtml' | wc -l`; contagens: `rg -n '<form' ...` e `rg -n '<dialog' ...`.
- Regressão local: `node --check backend/Valora.Web/wwwroot/js/pages/forms-page.js`, `node --check backend/Valora.Web/wwwroot/js/pages/form-builder.js` e `npm run test:forms-web` (confirmação assíncrona, clique repetido e cancelamento sem escrita).
- Solução oficial: `dotnet restore backend/Valora.sln && dotnet build backend/Valora.sln --no-restore && dotnet test backend/Valora.sln --no-build`.
- Integração PostgreSQL requer `VALORA_TEST_POSTGRES_CONNECTION`. Neste ambiente não há `dotnet`, `psql`, Docker, conexão configurada, sessão autenticada nem aplicação executável. Por isso criação/reconsulta em nova conexão/edição/arquivamento/auditoria e os cinco viewports permanecem **não verificados**, não aprovados por HTTP 200 ou por mock.

## Continuidade real

Concluído no código: criação confiável com navegação ao Builder; armazenamento de filtros tolerante a indisponibilidade e isolado por organização/usuário; publicação sem reutilizar `event.currentTarget` depois da confirmação assíncrona; validação server-side de dados básicos; tipo de pergunta canônico; compatibilidade de opções; exclusão lógica de opções acessível; edição sem autosave duplicado; seleção reidratada após recarga; modal/preview responsivos.

Pendente para o próximo lote: catálogo autorizado de dimensões em vez de texto livre; ordenação de perguntas/opções na UI; revisão de publicação detalhada com impedimentos navegáveis; consulta prévia dedicada de elegibilidade ao arquivamento; fixtures PostgreSQL multi-organização e concorrência; percurso autenticado e capturas em 1366×768, 1280×720, 768×1024, 390×844 e 360×800; aprofundar as 76 superfícies MVC restantes.


## Matriz curta de estado e versão

| Estado do formulário | Versão selecionada | Editar estrutura | Publicar | Nova versão | Arquivar |
|---|---|---:|---:|---:|---:|
| `draft` | `current_draft_version_id`, mutável e protegido por `row_version` | sim | sim, após validação definitiva | não; o rascunho já existe | sim, se não há coleta atual |
| `published` | `latest_published_version_id`, imutável | não | não | sim, com `forms.version` esperado e sem outro rascunho | sim, se não há coleta atual |
| `archived` | versão histórica preservada | não | não | não | não |

`forms.version` protege operações no agregado (dados básicos, nova versão e arquivamento); `form_versions.row_version` protege edição/publicação da estrutura. Publicar não reatribui pesquisas antigas, criar rascunho gera novos identificadores em transação, e arquivar não exclui versões, pesquisas, respostas nem relatórios. Uso atual significa vínculo a pesquisa `active`, `published` ou `open`; uso histórico significa qualquer vínculo; respostas são informadas separadamente.
