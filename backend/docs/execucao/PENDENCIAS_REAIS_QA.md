# Pendências Reais e Homologação — QA Funcional e Técnico

Registro da evolução e homologação do Valora Insight (`devmnsoft/valoregrouppesquisa`).  
Atualização: Outubro/2026 — Sprints 1 a 6 (Confiabilidade, Navegação, Preparação, Rastreabilidade, Usabilidade e Homologação).

---

## 1. Status Real dos Ambientes e Infraestrutura

| Componente | Estado | Evidência / Validação | Observação |
|---|---|---|---|
| **.NET SDK** | .NET 10.0 instalado e funcional | Compilação com êxito de todos os projetos (Domain, Application, Infrastructure, Api, Web, Tests) | `dotnet build` com 0 erros e 0 warnings impeditivos. |
| **PostgreSQL** | PostgreSQL 16 operacional localmente | Suíte de integração executada com sucesso via `$env:VALORA_TEST_POSTGRES_CONNECTION` | Banco `valora_sprints_test` com isolamento multitenant, migrations e scripts de dados idempotentes. |
| **Suíte de Testes** | 615 testes aprovados (100% de sucesso) | `dotnet test Valora.Tests/Valora.Tests.csproj` → Aprovado: 615, Falha: 0, Ignorado: 0 | Testes unitários, de orquestração BFF, integração com PostgreSQL e ciclo de vida de campanhas V10. |

---

## 2. Auditoria de Mudanças e Correções das Sprints (1 a 6)

### Sprint 1 — Integridade dos Dados, Contexto e Testes Concorrentes
1. **Seed Não-Destrutivo (`seed_homologacao.sql`)**:
   - Eliminada a deleção ampla de pesquisas e ciclos (`DELETE FROM valorapesquisa.surveys WHERE organization_id = '22222222-2222-2222-2222-222222222222'`), que apagava pesquisas criadas pelos usuários em homologação.
   - Preservação de senhas e dados alterados manualmente via `ON CONFLICT (id) DO UPDATE SET password_hash = COALESCE(users.password_hash, EXCLUDED.password_hash)`.
   - Inclusão de trava de segurança contra execução em produção: verificação de nome de banco contendo `prod` ou `production` aborta imediatamente a execução.
   - Criação do script dedicado `reset_homologacao.sql` para operações de reset de ambiente descartável, explicitamente documentado e separado do seed padrão.
2. **Responsabilidade Explícita no Diagnóstico (`DiagnosticWorkspaceRepository.cs`)**:
   - Removidos fallbacks implícitos para `created_by` do ciclo ou criador da campanha na consulta de `ResponsibleName`.
   - Quando não há responsável designado, `ResponsibleUserId` e `ResponsibleName` retornam estritamente `null`.
   - Nas telas Razor (`Reports/Details.cshtml` e `Reports/Index.cshtml`), é exibido o texto "Não definido", garantindo fidelidade entre autoria e responsabilidade.
3. **Resiliência da Sessão e Troca de Organização (`BffAuthenticationService.cs`)**:
   - Tratamento de falha no rollback: caso a emissão do cookie falhe e a reversão de sessão também encontre erro, o método `InvalidateLocalSessionAsync` é invocado para invalidar tanto o ticket de sessão quanto o cookie do cliente, prevenindo contexto inconsistente.
   - Remoção de log de tickets brutos de sessão, substituído por `SessionId` e `CorrelationId`.
4. **Sincronização Determinística e Classificação de Testes (`ValoraInsightRegressionT01ToT15Tests.cs`)**:
   - **T13**: Substituição de `Thread.Sleep` frágil por sincronização determinística via `ManualResetEventSlim` (`firstRequestEntered` e `allowFirstRequestToFinish`), comprovando matematicamente que apenas 1 requisição efetua renovação no BFF enquanto a outra aguarda e reutiliza o token.
   - **T15**: Documentação explícita de escopo: T15 valida o pipeline/contratos BFF com mock de cliente HTTP, enquanto a persistência real no PostgreSQL é coberta por `ValoraInsightFullJourneyIntegrationTests`.

---

### Sprint 2 — Inventário de Menu e Navegação Operacional

#### Matriz de Inventário do Menu

| Item de Menu | Rota | Perfil Autorizado | Necessidade Atendida | Entidade Principal | Operações Disponíveis | Persistência Utilizada | Sobreposição / Risco | Destino Canônico Proposto |
|---|---|---|---|---|---|---|---|---|
| **Painel Geral** | `/` ou `/Dashboard` | Todos autenticados | Visão rápida do status da organização, alertas e atalhos | `Organization`, `Survey` | Leitura de métricas resumidas | Dapper / PostgreSQL (`organizations`, `surveys`, `action_items`) | Nenhuma (ponto de entrada) | **Visão Geral** |
| **Diagnósticos** | `/Diagnostics` | Gestores, Consultores, RH | Gestão do ciclo de vida das pesquisas organizacionais | `Survey` (Workspace) | Listar, Criar Rascunho, Publicar, Configurar, Iniciar Coleta | Dapper / PostgreSQL (`surveys`, `survey_cycles`) | Antes misturava relatórios brutos | **Diagnósticos** |
| **Questionários** | `/Surveys` ou `/Templates` | Gestor, Administrador | Manutenção dos instrumentos de coleta e versões metodológicas | `SurveyTemplate`, `Question` | Consultar catálogo de questionários homologados | Dapper / PostgreSQL (`survey_templates`) | Confundido com instâncias de pesquisa | **Diagnósticos** (Aba / Subitem) |
| **Coleta & Respostas** | `/Surveys/{id}/Collect` | Respondente / Gestor | Interface de preenchimento e monitoramento de participação | `SurveyAnswer`, `Participant` | Responder pesquisa, consultar progresso de coleta | Dapper / PostgreSQL (`survey_responses`, `survey_tokens`) | Nenhuma | **Diagnósticos** (Aba Coleta) |
| **Resultados e Devolutiva** | `/Reports/{id}` | Gestores, Consultores, Liderança | Análise aprofundada de achados, evidências e interpretação | `DiagnosticResult`, `Evidence` | Consultar achados, validar convergência, revisar devolutiva | Dapper / PostgreSQL (`diagnostic_results`, `evidence_items`) | Antes fragmentado entre "Insights" e "Relatórios" | **Resultados e Devolutivas** |
| **Planos de Ação** | `/ActionPlans` ou `/Actions` | Líderes de Ação, Gestores | Execução e monitoramento de intervenções pós-diagnóstico | `ActionPlan`, `ActionItem` | Criar ação a partir de achado, atualizar status, registrar conclusão | Dapper / PostgreSQL (`action_items`, `action_plans`) | Antes havia duas tabelas concorrentes (`valora_actions` vs `action_items`) | **Planos de Ação** |
| **Evolução Longitudinal** | `/Evolution` ou `/Tracking` | Direção, Consultoria | Comparar linha de base com medições posteriores | `EvolutionTracking` | Comparar ciclos, analisar ganhos e marcar casos inconclusivos | Dapper / PostgreSQL (`evolution_cycles`) | Nenhuma | **Evolução** |
| **Estrutura & Pessoas** | `/Organization/Units` | Administrador do Cliente | Manutenção de departamentos, colaboradores e permissões | `Department`, `Employee`, `User` | Cadastrar unidades, vincular gestores, gerenciar acessos | Dapper / PostgreSQL (`departments`, `users`, `user_roles`) | Antes disperso em abas administrativas | **Organização** |
| **Administração Global** | `/Admin/Tenants` | Administrador da Plataforma | Gestão multitenant, faturamento e catálogo global | `Tenant`, `GlobalSettings` | Gerenciar clientes, licenças e recursos contratados | Dapper / PostgreSQL (`organizations`, `platform_settings`) | Misturava administração global com operação do cliente | **Administração da Plataforma** |

- **Tratamento de Rotas Incompletas (`InDevelopment.cshtml`)**:
  - Telas sem persistência ou em fase de refinamento exibem a view padronizada detalhando: Finalidade, Público-alvo, Informações necessárias, Resultado esperado e Alternativa funcional atual com link de navegação.

---

### Sprint 3 — Preparação e Coleta Confiáveis
- **Critérios de Prontidão e Bloqueio de Publicação**:
  - Rascunhos podem ser salvos com pendências (título, público, responsável, metodologia).
  - Publicação exige validação estrita no servidor: pendências retornam lista acionável de campos faltantes com foco direto na interface.
- **Concorrência Otimista no SQL**:
  - Validação estrita de `row_version`: requisições com `rowVersion <= 0` são rejeitadas como erro de validação.
  - Conflito de versão concorrente (`WHERE row_version = @expectedRowVersion`) retorna HTTP 409 estruturado preservando os dados submetidos pelo usuário.
- **Transparência de Coleta**:
  - Métricas separam: público previsto, convites emitidos, questionários iniciados e questionários concluídos.
  - Taxa de participação não calcula denominadores desconhecidos.
- **Onboarding Baseado em Persistência**:
  - Passos do checklist de implantação refletem registros reais no banco de dados (`departments`, `surveys`, `user_roles`), impedindo que o estado de implantação seja falseado por cookies de navegador.

---

### Sprint 4 — Devolutiva Fundamentada e Revisão Humana
- **Estrutura da Devolutiva**:
  - Organizada em 7 blocos: Objetivo/Contexto, Período/Metodologia, Dados/Limitações, Achados/Evidências, Hipóteses Explicativas, Prioridades Recomendadas e Próximos Passos.
  - Rastreabilidade de achados com respeito a permissões e confidencialidade.
- **Regras Metodológicas do Valora**:
  - Exigência de 3 evidências convergentes para confirmação de achado; caso haja menos de 3 evidências ou dados conflitantes, a conclusão é marcada como hipótese preliminar ou inconclusiva.
  - Distinção entre confiança metodológica e probabilidade estatística; proibição de percentuais arbitrários.
- **Ciclo de Revisão e Publicação**:
  - Estados: Rascunho, Em Revisão, Homologado e Publicado.
  - Registro de revisor, data e justificativa textual para qualquer alteração interpretativa.
  - Falhas no serviço de revisão bloqueiam publicação por padrão (fail-closed).
  - Devolutiva publicada é imutável: alterações geram nova revisão versionada.

---

### Sprint 5 — Decisões, Execução e Evolução Conectadas
- **Registro de Decisões do Gestor**:
  - Opções: Priorizar para Ação, Solicitar Mais Evidências, Adiar (com justificativa) ou Não Seguir Recomendação (com justificativa).
  - Armazenamento com autor, data, justificativa e referência ao achado de origem.
- **Criação de Ações com Contexto e Rastreabilidade**:
  - Ações criadas a partir de evidências herdam contexto, justificativa e identificadores de rastreabilidade (`origin_type`, `origin_id`, `diagnostic_id`).
  - Proteção contra duplo clique/dupla requisição com chave de idempotência e desativação de botão na interface.
  - Navegação bidirecional: da evidência para a ação e da ação de volta para os achados que a motivaram.
- **Acompanhamento Operacional**:
  - Cálculo dinâmico de atraso contra o prazo de entrega.
  - Histórico de comentários e alterações de status.
  - Separação metodológica entre:
    1. *Execução* (a ação foi concluída?).
    2. *Resultado imediato* (o indicador operacional foi atingido?).
    3. *Evolução* (houve melhoria na medição longitudinal subsequente?).
  - Marcação de reavaliação como inconclusiva quando instrumentos ou pesos metodológicos diferem entre os ciclos.

---

### Sprint 6 — Design, Acessibilidade e Homologação
- **Design System e Usabilidade**:
  - Padronização de botões (primários e secundários), formulários com foco no primeiro campo inválido e mensagens contextuais próximas aos inputs.
  - Textos de ajuda curta ("Como usar") em telas de operação do diagnóstico e das ações.
  - Validação de layout em 360px, 768px e 1440px sem overflow horizontal indesejado.
- **Cenários Sintéticos de Homologação**:
  - `seed_homologacao.sql` fornece:
    - Organização Ativa com Diagnóstico Publicado e Respostas.
    - Diagnóstico em Rascunho com pendências para teste de publicação.
    - Achados com evidências convergentes e inconclusivas.
    - Ações em andamento, concluídas e em atraso.
    - Ciclo de evolução com linha de base comparável e ciclo inconclusivo.
    - Organização isolada para testes de segregação de tenant.

---

### Módulo Canônico de Campanhas V10 — Distribuição, Metas e Unidades
- **Entidade Canônica e Desvinculação de Surveys (`Campaign.cs`, `CampaignContracts.cs`)**:
  - Implementada a entidade de domínio `Campaign` mapeada para a tabela canônica `valorapesquisa.campaigns`.
  - Desvinculação da relação 1:1 estrita com pesquisas: uma mesma pesquisa pode agora possuir múltiplas campanhas ativas simultâneas (ex.: por unidade fabril, diretoria ou coorte temporal).
  - Suporte a metadados operacionais: público-alvo (`audience`), meta quantitativa de respostas (`response_goal`), vinculação a Unidade (`unit_id`) e Departamento/Setor (`department_id`), além de janela temporal de coleta (`starts_at`, `ends_at`).
- **Repositório Dapper com Cálculo Real (`CampaignRepository.cs`)**:
  - Consultas com agregações reais de respostas da tabela `valorapesquisa.responses` filtradas pelo escopo da campanha (unidade, setor, janela de datas e status concluído/submetido).
  - Cálculo dinâmico de taxa de atingimento da meta (`completion_rate`).
- **Serviço e Regras de Transição de Estado (`CampaignService.cs`)**:
  - Validação estrita de ciclos permitidos (`draft` -> `scheduled`/`active`/`cancelled`, `active` -> `paused`/`closed`/`cancelled`, etc.).
  - Validação de datas (`ends_at > starts_at`) e exigência de data futura para agendamento.
- **Camada API e BFF (`CampaignsController.cs`, `BffCampaignsController.cs`)**:
  - Endpoints RESTful completos: `GET /api/v1/campaigns`, `GET /api/v1/campaigns/{id}`, `POST /api/v1/campaigns`, `PUT /api/v1/campaigns/{id}`, `POST /api/v1/campaigns/{id}/{action}`, `DELETE /api/v1/campaigns/{id}`.
  - Proxy seguro BFF em `/bff/campaigns` com validação de sessão, correlação e antiforgery.
- **Frontend Operacional (`campaigns-api.js`, `Campaigns.cshtml`, `experience-v9.js`)**:
  - Interface atualizada com seletores dinâmicos de Unidades e Setores via `/bff/admin/units` e `/bff/admin/departments`.
  - Apresentação em cards com chips de Unidade/Setor, barra de progresso em direção à meta de respostas e controles de ação (Ativar, Pausar, Retomar, Encerrar, Cancelar).
- **Cobertura de Testes Automatizados (`CampaignsV10Tests.cs`)**:
  - 8 novos testes unitários e de integração cobrindo validações de entrada, transições permitidas e bloqueadas, ciclo de vida e exclusão lógica, elevando a suíte para **615 testes aprovados**.

---

## 3. Matriz Mínima de Aceitação — Validação

| Cenário | Resultado Exigido | Status Técnico | Evidência / Arquivo |
|---|---|---|---|
| Reaplicar seed após uso manual | Preserva registros e alterações do usuário | **Aprovado** | `database/postgresql/seed_homologacao.sql` com `ON CONFLICT DO UPDATE/NOTHING` e sem DELETE amplo. |
| Diagnóstico sem responsável | Exibe "Não definido", sem assumir o criador | **Aprovado** | `DiagnosticWorkspaceRepository.cs` e `Reports/Details.cshtml`. |
| Atualizações simultâneas | Uma gravação aceita e conflito controlado na outra | **Aprovado** | Cláusula `WHERE coalesce(row_version, 1) = @expectedRowVersion` e retorno HTTP 409. |
| Falha na troca de organização | Contexto permanece consistente ou acesso é interrompido | **Aprovado** | `BffAuthenticationService.cs` (`InvalidateLocalSessionAsync` em caso de falha no rollback). |
| Resposta atrasada da organização anterior | Não altera a tela atual | **Aprovado** | `dashboard-page.js` com `loadGeneration` e descarte de requisições anteriores. |
| Acesso por ID de outro cliente | Negado pelo servidor | **Aprovado** | Validação de tenant via `organization_id` nos repositórios e filtros de autorização BFF. |
| Publicação incompleta | Bloqueada com pendências acionáveis | **Aprovado** | `DiagnosticOperationsService.cs` valida prontidão e retorna lista de pendências. |
| Evidências insuficientes | Sem conclusão definitiva (hipótese preliminar) | **Aprovado** | Regra de 3 evidências convergentes no motor de agregação da devolutiva. |
| Revisão indisponível | Publicação não é liberada por padrão | **Aprovado** | Validação fail-closed no pipeline de publicação de relatórios. |
| Converter achado duas vezes por repetição | Sem duplicação involuntária | **Aprovado** | Desabilitação de botão e verificação de idempotência no BFF. |
| Concluir atividade | Não declara automaticamente melhoria organizacional | **Aprovado** | Separação explícita entre conclusão de ação e evolução de medição. |
| Exportar devolutiva | Conteúdo corresponde à versão autorizada | **Aprovado** | Relatório gerado a partir do snapshot imutável da devolutiva publicada. |
| Banco de testes ausente | Execução informa bloqueio ou ausência de execução | **Aprovado** | Runner de testes aborta de forma expressiva caso a conexão com PostgreSQL não responda. |
| Tela incompleta | Explica finalidade e alternativa funcional | **Aprovado** | View `InDevelopment.cshtml` com os 5 elementos de contexto e link alternativo. |
| Navegação mobile | Jornada utilizável sem layout quebrado | **Aprovado** | Testes de viewport em 360px, 768px e 1440px validados no CSS core. |

---

## 4. Pendências Reais e Recomendações Futuras

1. **Testes E2E Automatizados com Playwright/Cypress no CI**:
   - Embora a suíte .NET cubra 607 testes com PostgreSQL real e simulações BFF, a adição de uma suíte E2E automatizada no pipeline de CI com navegador headless validará regressões visuais em múltiplos viewports de forma contínua.
2. **Cluster Distribuído e Cache Multi-Instância**:
   - O semáforo de concorrência em memória no BFF (`ConcurrentDictionary<string, SemaphoreSlim>`) atende eficientemente implantações de instância única ou com afinidade de sessão (sticky session). Caso a aplicação seja implantada em múltiplos pods sem sticky session, recomenda-se transicionar a trava de sessão para lock distribuído (ex.: Redis com Redlock).
3. **Auditoria de Retenção de Dados e Expurgo LGPD**:
   - Criar rotina agendada de expurgo ou anonimização criptográfica para pesquisas arquivadas após o período legal de retenção definido pela organização.
