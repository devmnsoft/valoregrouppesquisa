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
| Builder | revisar/publicar | `GET /bff/forms/{id}/publication-review` e `POST /bff/forms/{id}/publish`; `FormPublicationReviewResponse`/`PublishFormVersionRequest` | serviço/repository; estrutura ativa, `form_versions`, `forms`, auditoria | `Forms.Read` para revisão; `Forms.Publish` para publicar; draft válido e versão imutável após publicação | A revisão separa seções, perguntas respondíveis, informativos, vínculos, impedimentos e avisos; cada problema navegável retorna ao item. A confirmação identifica a versão e só mostra sucesso após resposta. **Implementado e verificado por análise local; banco bloqueado**. |
| Forms | arquivar formulário | `DELETE /bff/forms/{id}`; `ArchiveFormRequest` | serviço/repository; `forms`, dependências/auditoria | `Forms.Archive`; versão e restrição de uso | Biblioteca confirma nome e consequência, preserva o histórico, envia token de concorrência e ajusta a última página. O servidor revalida o tenant/versão, bloqueia vínculos com coleta ativa e audita o ator; histórico isolado não bloqueia. **Implementado e verificado estaticamente; PostgreSQL bloqueado**. |

## Evidências executáveis e bloqueios

- Inventário: `rg -l '<form|<dialog' backend/Valora.Web/Views -g '*.cshtml' | wc -l`; contagens: `rg -n '<form' ...` e `rg -n '<dialog' ...`.
- Regressão local: `node --check backend/Valora.Web/wwwroot/js/pages/forms-page.js`, `node --check backend/Valora.Web/wwwroot/js/pages/form-builder.js` e `npm run test:forms-web` (confirmação assíncrona, clique repetido e cancelamento sem escrita).
- Solução oficial: `dotnet restore backend/Valora.sln && dotnet build backend/Valora.sln --no-restore && dotnet test backend/Valora.sln --no-build`.
- Integração PostgreSQL requer `VALORA_TEST_POSTGRES_CONNECTION`. Neste ambiente não há `dotnet`, `psql`, Docker, conexão configurada, sessão autenticada nem aplicação executável. Por isso criação/reconsulta em nova conexão/edição/arquivamento/auditoria e os cinco viewports permanecem **não verificados**, não aprovados por HTTP 200 ou por mock.

## Continuidade real

Concluído no código: criação confiável com navegação ao Builder; armazenamento de filtros tolerante a indisponibilidade e isolado por organização/usuário; publicação precedida por revisão server-side; validação server-side de dados básicos; tipos de pergunta traduzidos sem mudar os códigos; catálogo de dimensões publicadas autorizado pela organização, preservando vínculos antigos inativos; compatibilidade e exclusão lógica de opções; edição com escolha explícita entre salvar, descartar e permanecer; mutações centralizadas; seleção reidratada após recarga; ordenação persistida de seções, perguntas e opções por botões acessíveis; modal/preview responsivos.

Pendente para o próximo lote: consulta prévia dedicada de elegibilidade ao arquivamento; fixtures PostgreSQL multi-organização e concorrência; percurso autenticado e capturas em 1366×768, 1280×720, 768×1024, 390×844 e 360×800; aprofundar as 76 superfícies MVC restantes.


## Matriz curta de estado e versão

| Estado do formulário | Versão selecionada | Editar estrutura | Publicar | Nova versão | Arquivar |
|---|---|---:|---:|---:|---:|
| `draft` | `current_draft_version_id`, mutável e protegido por `row_version` | sim | sim, após validação definitiva | não; o rascunho já existe | sim, se não há coleta atual |
| `published` | `latest_published_version_id`, imutável | não | não | sim, com `forms.version` esperado e sem outro rascunho | sim, se não há coleta atual |
| `archived` | versão histórica preservada | não | não | não | não |

`forms.version` protege operações no agregado (dados básicos, nova versão e arquivamento); `form_versions.row_version` protege edição/publicação da estrutura. Publicar não reatribui pesquisas antigas, criar rascunho gera novos identificadores em transação, e arquivar não exclui versões, pesquisas, respostas nem relatórios. Uso atual significa vínculo a pesquisa `active`, `published` ou `open`; uso histórico significa qualquer vínculo; respostas são informadas separadamente.

## Incremento — integração Forms → Diagnósticos (2026-09-15)

| Operação | Estado | Evidência | Limitação |
|---|---|---|---|
| Consumir listagem paginada de Forms | **Implementado e verificado localmente** | `FormsApi.normalizeList` valida `items`, total e paginação; biblioteca e seletor deixaram de tratar envelope como array. Teste carrega JSON camelCase equivalente à serialização ASP.NET e rejeita contrato antigo/malformado. | Integração HTTP autenticada permanece bloqueada sem aplicação compilável. |
| Carregar diagnósticos e seletor | **Implementado e verificado estaticamente** | Requisições independentes; erro do catálogo não substitui diagnósticos já carregados. | Banco/API real indisponíveis. |
| Buscar formulário fora da primeira página | **Implementado no cliente** | Busca com debounce, páginas de 10 itens e navegação Anterior/Próxima; nenhuma escolha arbitrária. Edição consulta o vínculo por id antes de pesquisar o catálogo. | O endpoint ainda precisa expor metadados explícitos da última publicação em todas as instalações para distinguir integralmente rascunho posterior. |
| Cadastro/edição do diagnóstico | **Implementado no cliente; backend pendente** | Título, objetivo, versão, início e término; erro dentro do diálogo, valores preservados, bloqueio de duplo envio, estado de envio e aviso de alterações pendentes. | A API administrativa legada ainda não persiste `formVersionId`/período nem aplica a matriz transacional completa; não foi declarada pronta para produção. |
| Revisar/abrir/encerrar | **Parcial** | Textos e confirmações distinguem revisão, abertura e consequência do encerramento; cliente não envia status no create/update. | Revalidação transacional, concorrência encerramento×resposta e autorização por transição permanecem pendentes no endpoint administrativo canônico. |
| Gerar/copiar link | **Implementado no cliente** | Ação “Gerar link de participação” difere de “Copiar link”; URL obrigatória; fallback selecionável quando Clipboard falha; sucesso somente após `writeText`. | Revogação existe no SDK, mas ainda não ganhou ação nesta lista; persistência real não verificada. |
| Preview do Builder | **Implementado e verificado por sintaxe** | Renderizador próprio de resposta, sem `data-select`, exclusão, reordenação ou inclusão; campos são experimentáveis e informam que nada será salvo. | Percurso visual autenticado bloqueado. |
| Acompanhamento | **Parcial** | Estado, formulário/versão quando disponível, período, quantidade real quando fornecida e links de respostas/resultados; ausência de denominador não produz percentual. | API atual não fornece contagem/última atividade confiáveis em todas as instalações. |

### Matriz de transições observada e alvo de proteção

| Origem | Destino | Intenção na interface | Pré-condições | Efeito esperado |
|---|---|---|---|---|
| `draft` | `active` | Revisar e abrir | versão publicada respondível, período coerente, organização/permissão/módulo | aceita respostas e permite gerar link |
| `active`/`open`/`published` | `closed` | Encerrar | vínculo/versionamento ainda válidos e confirmação | revoga/bloqueia participação nova; preserva respostas |
| `closed` | — | nenhuma reabertura oferecida | — | histórico e resultados permanecem consultáveis |

A matriz descreve o comportamento requerido, mas a proteção direta da API antiga está marcada **pendente** até que o repository administrativo seja migrado integralmente do contrato compatível (`form_id`) para o esquema canônico (`form_version_id`). Não foi criada entidade paralela nem feita associação retroativa à versão mais recente por suposição.

## Incremento — coleta vinculada à versão publicada (2026-09-15)

| Ponto de continuidade | Classificação | Evidência / limite |
|---|---|---|
| Compatibilidade Surveys × Forms paginado | **verificado** | O cliente mantém o envelope paginado e envia `formId` + `formVersionId`; a API agora exige ambos. |
| Seleção da versão publicada | **implementado** | Criação e edição validam no PostgreSQL o par formulário/versão, publicação e organização; não existe fallback para a versão mais recente. |
| Cadastro e edição do diagnóstico | **implementado** | Período e versão são persistidos; somente rascunho pode ser editado e o servidor revalida o tenant. |
| Publicação e encerramento | **implementado; integração bloqueada** | Transições são allowlist, não reabrem coleta, exigem versão publicada; conclusão bloqueia a linha da pesquisa e revalida elegibilidade na transação. Falta executar a corrida em PostgreSQL neste ambiente. |
| Geração e cópia de links | **implementado** | O servidor somente gera link para coleta ativa/elegível; geração e cópia continuam ações distintas com fallback manual. |
| Preview separado do Builder | **verificado** | Permanece o renderizador sem mutações registrado no incremento anterior. |

A leitura pública, as perguntas, opções, dimensões, resposta persistida e resultado histórico usam agora o `form_version_id` da pesquisa. Respostas antigas recebem versão somente quando ela pode ser derivada diretamente da pesquisa de origem; inconsistências permanecem nulas e visíveis, sem associação automática à versão mais recente. O resultado pode consultar a pesquisa encerrada sem transformar o encerramento em perda de rastreabilidade.

### Verificação ainda bloqueada

O contêiner continua sem `dotnet`, `psql`, `VALORA_TEST_POSTGRES_CONNECTION`, identidade autenticada e navegador integrado. Assim, restore/build Razor, os 18 cenários PostgreSQL, concorrência real e os cinco viewports permanecem **não verificados**. Comandos reproduzíveis: `dotnet restore backend/Valora.sln && dotnet build backend/Valora.sln --no-restore && dotnet test backend/Valora.sln --no-build` e `VALORA_TEST_POSTGRES_CONNECTION='...' dotnet test backend/Valora.Tests/Valora.Tests.csproj --filter PostgreSql`.

## Jornada pós-diagnóstico — incremento 2026-09-15

| Operação | Estado | Evidência | Pendente/bloqueio |
|---|---|---|---|
| Consultar resultado executivo | **Implementado** | Contrato `AdminResultReadModel`; consulta parametrizada por organização/resposta; versão, score anulável, dimensões, relatórios e planos na mesma leitura. | Execução PostgreSQL e sessão autenticada bloqueadas no ambiente. |
| Gerar/reabrir relatório por resultado | **Implementado** | Relatório deriva da mesma projeção; entregável válido é reutilizado por origem/formato; download BFF preserva tipo/nome e revalida `reports.download`. | PDF e fila assíncrona não existem neste mecanismo; não foram simulados. |
| Criar plano desde resultado | **Implementado** | `origin_type=result`, `origin_id=result_id`, `CommandId`, validação tenant/módulo/referência/responsável transacional e retorno local. | Cenários SQL reais e auditoria em banco não verificados sem PostgreSQL. |
| Acompanhar planos no resultado | **Implementado** | Lista vinculada ao `results.id`, com responsável, prazo, estado, progresso e retorno contextual. | Viewports/teclado aguardam aplicação autenticada executável. |
| Reprocessar e comparar históricos | **Pendente** | Nenhum cálculo paralelo ou comparação artificial foi adicionado. | Exige política canônica de reprocessamento e compatibilidade metodológica. |
