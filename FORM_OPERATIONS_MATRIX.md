# Matriz de formulários e operações — MVC/API

Atualização: 2026-09-15. Esta matriz cobre o lote **Forms/Builder** em profundidade e registra o inventário estático das demais superfícies; não transforma inspeção estática em homologação. A aplicação publicada pelo projeto ASP.NET está em `backend/Valora.Web` e usa a API em `backend/Valora.Api`. O `index.html` e `app.js` da raiz pertencem ao frontend Firebase legado e não são consumidores das views MVC.

## Inventário

- 78 views/partials MVC contêm formulário ou diálogo (108 tags `form` e 41 tags `dialog`). O comando reproduzível está na seção Evidências.
- Grupos encontrados: Account; ActionCenter; ActionPlans; AdminHub; Advisor; Architecture; AssistedOperations; Benchmarks; Certificates; CommandCenter; DecisionCenter; Evolution; Experience; Forms; FreeDiagnostics; Governance; Indicators; Insights; Intelligence; Journey; Knowledge; Lgpd; Methodology; Onboarding; OneOnOne; OperationalIntelligence; Organization; People; PublicPages; PublicSurvey; Reports; Results; Saas; SaasAdmin; SolutionPacks; SuccessCenter; Surveys; Users; Workspace; e componentes Shared.
- O frontend legado mantém CRUD administrativo Firebase/Cloud Functions documentado em `ADMIN_SURVEY_FORM_CRUD_AUDIT.md`; ele não foi confundido nem alterado neste lote.

## Matriz rastreável do lote Forms/Builder

| Módulo/rota | Interface e operação | Endpoint/verbo e contrato | Aplicação, repository e tabelas | Escopo/regras | Evidência e situação |
|---|---|---|---|---|---|
| Forms `/Forms` | `Views/Forms/Index.cshtml`, modal: criar | `POST /bff/forms` → `POST /api/v1/forms`; `CreateFormRequest` | `FormAdministrationService` → `FormAdministrationRepository`; `forms`, `form_versions`, `audit_logs` | autenticado; `Forms.Create`; organização explícita; nome, descrição, categoria e 1–480 minutos | Handler impede fechamento nativo de `method=dialog`, bloqueia duplo envio, preserva campos/erro, exige confirmação do servidor e abre o Builder pelo `id`. Testes de contrato e sintaxe: **corrigido; PostgreSQL bloqueado**. |
| Forms `/Forms` | listar, buscar, filtrar, atualizar | `GET /bff/forms?page=1&pageSize=100`; `FormListQuery`/`FormListItemResponse` | serviço/repository; `forms`, `form_versions`, seções/perguntas | `Forms.Read`; organização; SQL parametrizado e ordenação estável | Estados loading/vazio/erro são distintos; filtros atuais são locais sobre até 100 registros e não constituem paginação completa. **Parcial/bloqueado**, pendência abaixo. |
| Builder `/Forms/{id}/Builder` | consultar estrutura/preview | `GET /bff/forms/{id}`; `FormDetailResponse` | serviço/repository; versões, seções, perguntas e opções | `Forms.Read`; organização; 404 não revela outro tenant | Preview deriva da estrutura recém-consultada. **Verificado estaticamente; execução bloqueada**. |
| Builder | editar dados básicos | `PUT /bff/forms/{id}`; `UpdateFormRequest` | serviço/repository; `forms` | `Forms.Update`; versão esperada; limite e intervalo também no servidor | Autosave concorrente removido; submissão explícita e confirmada; valores permanecem no conflito. **Corrigido; banco bloqueado**. |
| Builder | criar/editar/arquivar seção | `POST/PUT/DELETE .../sections`; contratos tipados | serviço/repository; `form_section_versions`, `form_versions`, `audit_logs` | `Forms.Manage`; somente draft; organização e versão | Exclusão lógica e auditoria existentes, UI confirma uma vez. **Verificado estaticamente; banco bloqueado**. |
| Builder | criar/editar/arquivar pergunta | `POST/PUT/DELETE .../questions`; contratos tipados | serviço/repository; `question_versions`, versões/auditoria | `Forms.Manage`; draft; tipos allowlist; versão | Tipo visual `explanatory_text` incompatível foi substituído pelo tipo canônico `description`; mudança para tipo sem opções é rejeitada no SQL enquanto houver opções. **Corrigido; banco bloqueado**. |
| Builder | criar/editar/arquivar opção | `POST .../questions/{id}/options`; `PUT/DELETE .../options/{id}` | serviço/repository; `question_option_versions`, versões/auditoria | `Forms.Manage`; draft; pergunta do mesmo formulário/organização; versão; score 1–5 ou nulo | A ação de arquivar opção, antes ausente da UI, foi conectada. Backend impede opções em tipos incompatíveis. **Corrigido; banco bloqueado**. |
| Builder | reordenar seção | `POST /bff/forms/{id}/reorder`; `ReorderFormItemRequest` | serviço/repository; tabelas versionadas | `Forms.Manage`; draft, organização, versão | Botões Subir/Descer persistem e recarregam. **Verificado estaticamente; banco bloqueado**. |
| Builder | publicar | `POST /bff/forms/{id}/publish`; `PublishFormVersionRequest` | serviço/repository; `form_versions`, `forms`, auditoria | `Forms.Publish`; draft válido; publicação torna versão imutável | Confirma consequência e só mostra sucesso após resposta. Falha permanece visível. **Verificado estaticamente; banco bloqueado**. |
| Forms | arquivar formulário | `DELETE /bff/forms/{id}`; `ArchiveFormRequest` | serviço/repository; `forms`, dependências/auditoria | `Forms.Archive`; versão e restrição de uso | Endpoint existe, mas a biblioteca ainda não oferece a ação e não há prova PostgreSQL da restrição em uso. **Pendente**. |

## Evidências executáveis e bloqueios

- Inventário: `rg -l '<form|<dialog' backend/Valora.Web/Views -g '*.cshtml' | wc -l`; contagens: `rg -n '<form' ...` e `rg -n '<dialog' ...`.
- Regressão local: `node --check backend/Valora.Web/wwwroot/js/pages/forms-page.js` e `node --check backend/Valora.Web/wwwroot/js/pages/form-builder.js`.
- Solução oficial: `dotnet restore backend/Valora.sln && dotnet build backend/Valora.sln --no-restore && dotnet test backend/Valora.sln --no-build`.
- Integração PostgreSQL requer `VALORA_TEST_POSTGRES_CONNECTION`. Neste ambiente não há `dotnet`, `psql`, Docker, conexão configurada, sessão autenticada nem aplicação executável. Por isso criação/reconsulta em nova conexão/edição/arquivamento/auditoria e os cinco viewports permanecem **não verificados**, não aprovados por HTTP 200 ou por mock.

## Continuidade real

Concluído no código: criação confiável com navegação ao Builder; validação server-side de dados básicos; tipo de pergunta canônico; compatibilidade de opções; exclusão lógica de opções acessível; edição sem autosave duplicado; seleção reidratada após recarga; modal/preview responsivos.

Pendente para o próximo lote: contrato paginado com total coerente e métricas globais; catálogo autorizado de dimensões em vez de texto livre; ordenação de perguntas/opções na UI; criação de nova versão publicada pela biblioteca; ação de arquivamento com nome/consequência; fixtures PostgreSQL multi-organização e concorrência; percurso autenticado e capturas em 1366×768, 1280×720, 768×1024, 390×844 e 360×800; aprofundar as 76 superfícies MVC restantes.
