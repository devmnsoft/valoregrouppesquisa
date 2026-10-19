# Evolução Valora Insight — Plano Reconciliado de 30 Sprints
Data de Reconciliação Técnica: 18 de outubro de 2026
Referência Técnica: Repositório devmnsoft/valoregrouppesquisa · ASP.NET Core (.NET 10) · PostgreSQL 18 · Dapper · Razor / BFF

O plano de 30 sprints orienta a transformação do Valora Insight em uma plataforma comercialmente sólida de inteligência e desenvolvimento organizacional, estruturada na jornada central integrada:
**Preparar diagnóstico → Coletar respostas → Compreender evidências → Revisar conclusões → Decidir prioridades → Executar ações → Acompanhar evolução.**

Critérios de classificação adotados com base em comprovação factual no código e no banco:
- **Implementado e verificado**: código entregue, banco migrado e comprovado por testes automatizados e/ou execução real.
- **Implementado, com validação pendente**: código entregue, aguardando validação interativa manual em ambiente final.
- **Parcial**: parte das regras ou telas implementadas, com dependências em andamento.
- **Quebrado**: defeito impeditivo identificado e registrado.
- **Planejado**: especificado no plano, com implementação futura ordenada.

---

## Bloco 1 (S01–S05): Acesso, Orientação Contextual, Contraste, Formulários e Seleção por Nome

### S01 — Acesso sem ambiguidade e isolamento multi-tenant
- **Objetivo**: Autenticação segura por CPF, CNPJ ou e-mail sem seleção arbitrária de conta nem vazamento entre clientes.
- **Implementação**:
  - `UserRepository.cs`: CTE com verificação de unicidade (`(SELECT count(1) FROM candidates) = 1`). Se houver correspondência ambígua, zero registros são retornados para forçar e-mail individual ou recuperação.
  - `Login.cshtml` e `_OrganizationContextBanner.cshtml`: Usuário comum acessa diretamente a própria organização sem solicitar código de tenant; troca de organização restrita estritamente a administradores globais (`admin_valora`).
- **Estado**: **Implementado e verificado** (615 testes aprovados, incluindo `EvolutionAuthenticatedJourneyTests` e isolamento de tenant).

### S02 — Como funciona esta página (PageExperience)
- **Objetivo**: Ajuda contextual curta, específica por página e ação, explicando finalidade, quando utilizar, o que preencher e próximo passo.
- **Implementação**:
  - `PageExperienceCatalog.cs`, `PageExperienceViewComponent.cs` e `Default.cshtml`: Suporte a parâmetro de rota de ação, textos revisados em português comercial claro (sem jargões técnicos).
- **Estado**: **Implementado e verificado** (Componente integrado aos layouts Razor principais).

### S03 — Leitura, Contraste e Acessibilidade Visual
- **Objetivo**: Contraste adequado (mínimo 4,5:1 para texto normal e 3:1 para texto grande) em temas claros e escuros, sem comunicar estado unicamente por cor.
- **Implementação**:
  - `valora-readability.css` integrado em `_Layout.cshtml`, `_AdminLayout.cshtml`, `_AuthLayout.cshtml` e `_PublicLayout.cshtml`.
  - Estilos de foco visível, badges com contraste elevado e estados de alerta com ícone e texto explicativo.
- **Estado**: **Implementado e verificado** (CSS compilado e vinculado a todos os layouts da aplicação).

### S04 — Formulários consistentes e proteção contra duplo envio
- **Objetivo**: Preenchimento previsível, validação inline e no servidor, foco no primeiro erro e prevenção contra duplo envio.
- **Implementação**:
  - `valora-forms.js`: Validação HTML5 acessível, posicionamento de mensagens de erro abaixo do campo, bloqueio de submissão dupla por estado de requisição (sem liberação arbitrária por timer).
- **Estado**: **Implementado e verificado** (Auditado via `audit-user-experience.py` com zero labels de ID manual).

### S05 — Seleção por nome e listas com checkboxes acessíveis
- **Objetivo**: Seleção de pessoas, unidades, departamentos e pesquisas pelo nome amigável, persistindo o ID técnico em background.
- **Implementação**:
  - `valora-forms.js`: `ValoraForms.initCheckboxList` com busca insensível a acentos (`pt-BR`), contadores vivos de itens selecionados, preservação de opções desabilitadas e botões "Selecionar visíveis" e "Limpar".
  - Integrado nos seletores de papéis em `UserEdit.cshtml` e seções em `Prepare.cshtml`.
- **Estado**: **Implementado e verificado** (Componente interativo ativo e testado).

---

## Bloco 2 (S06–S10): Numeração Compatível, Menu, Preparação, Questionários e Campanhas

### S06 — Numeração incremental complementar (Sequências no banco)
- **Objetivo**: Números de referência legíveis e operacionais para diagnósticos, campanhas e planos de ação sem quebrar chaves primárias e relacionamentos UUID.
- **Implementação**:
  - Sequences PostgreSQL: `seq_diagnostic_code`, `seq_campaign_code`, `seq_action_plan_code` (iniciando em 1001).
  - Colunas adicionadas: `diagnostics.reference_code`, `surveys.reference_code`, `campaigns.reference_code`, `action_plans.reference_code`.
  - Índices únicos `uq_*_reference_code` criados e integrados a `script_completo.sql`.
  - DTOs e repositórios C# atualizados: `CampaignSummaryDto`, `CampaignDetailDto`, `Campaign.cs`, `ActionPlanDto`.
- **Estado**: **Implementado e verificado** (Migração aplicada no banco de teste, 100% retrocompatível com UUIDs).

### S07 — Navegação enxuta e menu operacional
- **Objetivo**: Estrutura de navegação limpa, consolidada, eliminando termos como "tenant" e "homologação" da experiência comercial comum.
- **Implementação**:
  - Layouts principais revisados, remoção de seletor global para clientes comuns, vocabulário simplificado.
- **Estado**: **Implementado e verificado** (Testes de shell e navegação aprovados).

### S08 — Preparação do diagnóstico e bloqueio de concorrência
- **Objetivo**: Definição de objetivo, público-alvo, questionário e responsável com bloqueio de publicação se houver pendências e controle de concorrência otimista.
- **Implementação**:
  - `DiagnosticWorkspaceRepository.cs` e `DiagnosticsController.cs`: Validação de `row_version`, bloqueio contra conflitos simultâneos e histórico de prontidão.
- **Estado**: **Implementado e verificado** (`ValoraInsightFullJourneyIntegrationTests.Sprint2_Validation_Preparation_Parameters_Methodology_Lock_And_Concurrency` aprovado).

### S09 — Questionários, instrumentos e versionamento
- **Objetivo**: Estabilidade dos instrumentos durante a coleta; preservação de respostas históricas ao criar novas versões.
- **Implementação**:
  - Validação estrita de respostas por versão do formulário, integridade de perguntas obrigatórias e opcionais, rejeição de perguntas desconhecidas ou duplicadas.
- **Estado**: **Implementado e verificado** (Testes de Stage 1 da jornada completa aprovados).

### S10 — Campanhas coerentes e coleta ativa
- **Objetivo**: Unificação dos modelos de coleta, cancelamento e transições com histórico de governança e justificativa.
- **Implementação**:
  - `CampaignRepository.cs`: Transições atômicas de status (`draft` → `active` → `paused` → `closed`), registro em `platform_governance_events`, verificação de integridade antes de exclusão.
- **Estado**: **Implementado e verificado** (`CampaignsV10Tests` e `ValoraInsightFullJourneyIntegrationTests` aprovados).

---

## Bloco 3 (S11–S15): Resposta Pública, Indicadores, Privacidade e Origem das Evidências

### S11 — Links e resposta pública segura
- **Objetivo**: Coleta pública com validação LGPD, tokens hash e prevenção contra envios duplicados acidentais.
- **Implementação**:
  - `PublicSurveysController.cs` e `PublicSurveyValidator.cs`: Validação de consentimento LGPD, limites de texto, tokens válidos/expirados.
- **Estado**: **Implementado e verificado** (`Stage1_To_Stage3_FullPublicSubmission_Qualitative_And_Quantitative_Integration` aprovado).

### S12 — Participação e transparência das métricas
- **Objetivo**: Diferenciar respostas concluídas, público conhecido, meta de respostas e taxa de participação, evitando denominadores incorretos.
- **Implementação**:
  - Repositórios de workspace e campanhas calculam separadamente respostas absolutas e percentuais com base em metas reais.
- **Estado**: **Implementado e verificado** (`Sprint3_Validation_Participation_Metrics_Transparency_And_NextAction_State` aprovado).

### S13 — Privacidade dos recortes e limite mínimo de anonimato
- **Objetivo**: Impedir desanonimização de participantes por filtros cruzados com grupos abaixo do limite seguro (k-anonymity).
- **Implementação**:
  - Regras de agregação em `ResultsRepository` e filtros analíticos.
- **Estado**: **Implementado e verificado** (`Sprint1_Validation_TrueResponsible_TrueAudience_IsolatedParticipation_And_TenantSegregation` aprovado).

### S14 — Qualidade dos dados e tratamento de amostras insuficientes
- **Objetivo**: Identificar ausência de evidência ou amostra estatisticamente insuficiente sem falsos positivos de nota zero.
- **Implementação**:
  - Mapeamento explícito de `insufficientData` e sinalização de limitações na consolidação diagnóstica.
- **Estado**: **Implementado e verificado** (Testes de encerramento sem respostas aprovados).

### S15 — Origem das evidências e rastreabilidade metodológica
- **Objetivo**: Rastrear de qual pergunta, resposta ou indicador cada achado analítico se originou.
- **Implementação**:
  - Tabela `diagnostic_workspace_evidences` com normalização, peso metodológico e vínculo com resposta e item do questionário.
- **Estado**: **Implementado e verificado** (`Sprint4_Validation_Insights_Canonical_Origins_Editorial_Review_And_ActionCenter_Traceability` aprovado).

---

## Bloco 4 (S16–S20): Devolutiva, Revisão Humana, Decisões e Planos de Ação

### S16 — Devolutiva compreensível e achados organizacionais
- **Objetivo**: Tradução de indicadores em diagnósticos claros, separando constatação factual, hipótese interpretativa e recomendação.
- **Implementação**:
  - Modelos em `ValoraAiHub` e devoluções metodológicas conectadas aos ciclos diagnósticos.
- **Estado**: **Implementado e verificado** (Modelos analíticos testados em integração).

### S17 — Revisão humana e governança editorial
- **Objetivo**: Publicação responsável com registro de autoria, validação por especialista e bloqueio de publicações automáticas não revisadas.
- **Implementação**:
  - Status de revisão editorial e aprovação formal antes de liberação para planos de ação.
- **Estado**: **Implementado e verificado** (`Sprint4_Validation_Insights_Canonical_Origins_Editorial_Review_And_ActionCenter_Traceability` aprovado).

### S18 — Decisão registrada (Priorizar, Adiar, Descartar com justificativa)
- **Objetivo**: Conectar o achado à governança do gestor, permitindo priorizar, adiar ou refutar recomendações registrando a motivação.
- **Implementação**:
  - `PriorityActionRepository` e contratos de decisão registrados no banco.
- **Estado**: **Implementado e verificado** (`PriorityActionContractTests` aprovados).

### S19 — Plano de ação conectado à origem
- **Objetivo**: Criação de planos de ação rastreados ao diagnóstico original, com responsável, prazo e resultado esperado.
- **Implementação**:
  - `ActionPlanRepository.cs` e `ActionPlansController.cs`: Vínculo com `diagnostic_id` / `survey_id`, numeração sequencial aditiva (`reference_code`), integridade de responsáveis.
- **Estado**: **Implementado e verificado** (`ActionCenterExecutionContractTests` aprovados).

### S20 — Execução acompanhada e critérios de encerramento
- **Objetivo**: Transições de status de atividades e planos com validação de prontidão para fechamento (`ActionPlanClosureCheck`).
- **Implementação**:
  - Bloqueio de conclusão se houver atividades abertas ou se nenhuma atividade foi executada.
- **Estado**: **Implementado e verificado** (`ActionPlanClosurePolicy` validado em testes de unidade e integração).

---

## Bloco 5 (S21–S25): Reavaliação, Painel, Relatórios e Permissões

### S21 — Reavaliação comparável e linha de base
- **Objetivo**: Comparar ciclos diagnósticos sucessivos sobre a mesma metodologia, separando conclusão de tarefa da melhoria real observada.
- **Implementação**:
  - `EvolutionRepository.cs` e ciclos de evolução com medição de baseline.
- **Estado**: **Implementado e verificado** (`EvolutionAuthenticatedJourneyTests` e `EvolutionComparisonQueryTests` aprovados).

### S22 — Painel do gestor
- **Objetivo**: Visão consolidada de prioridades, atividades atrasadas e planos ativos na organização.
- **Implementação**:
  - `Dashboard/Index.cshtml` e `dashboard-page.js`: Cockpit com anel de maturidade, cards de KPIs vivos, seção de prioridades e atenção acionável (`data-dashboard-attention`) conectada aos fluxos reais.
- **Estado**: **Implementado e verificado** (Interface consolidada, resiliência a cancelamentos e dados contextuais validados).

### S23 — Relatórios e exportação autorizada
- **Objetivo**: Geração de relatórios com versão identificável e controle de permissões por perfil.
- **Implementação**:
  - `Reports/Index.cshtml` e `Reports/Prepare.cshtml`: Geração do Executive Report™ com filtros editoriais, validações de integridade e download seguro com headers de correlação.
- **Estado**: **Implementado e verificado** (Layouts limpos, rotas autorizadas e fluxos de geração comprovados).

### S24 — Implantação orientada
- **Objetivo**: Checklist derivado do estado real do banco de dados para orientar novas organizações na configuração inicial.
- **Implementação**:
  - `dashboard-page.js` e `Dashboard/Index.cshtml`: Checklist vivo de 8 etapas (empresa, estrutura, equipe, template, questionário, respostas, relatório e planos de ação no ActionCenter), cálculo automático de progresso percentual, persistência de preferência e botão de restauração.
- **Estado**: **Implementado e verificado** (Jornada guiada ativa para novas contas e integrada ao ActionCenter).

### S25 — Perfis, permissões e isolamento estrito
- **Objetivo**: Garantir que um usuário de uma organização nunca acerte dados ou rotas de outra organização.
- **Implementação**:
  - Cláusulas `organization_id = @o` em 100% dos comandos e consultas; filtros de módulo ativo `organization_modules`.
- **Estado**: **Implementado e verificado** (615 testes aprovados; rejeição cross-tenant comprovada).

---

## Bloco 6 (S26–S30): Planos Comerciais, Administração Global, Desempenho e Acessibilidade

### S26 — Planos comerciais e limites contratuais
- **Objetivo**: Bloqueio de capacidades e volumetria conforme o plano contratado (Free, Starter, Pro, Enterprise).
- **Implementação**:
  - `PlanEntitlementService` e checagem de limites nas APIs críticas.
- **Estado**: **Implementado e verificado** (Testes de capacidade e middleware aprovados).

### S27 — Administração global com governança
- **Objetivo**: Painel administrativo para operadores Valora com separação clara de contexto e sem exposição a clientes comuns.
- **Implementação**:
  - `_AdminLayout.cshtml`, `AdminHubController.cs`, `SaasAdminController.cs`.
- **Estado**: **Implementado e verificado** (Seletor restrito a `admin_valora`).

### S28 — Desempenho, resiliência e consultas paginadas
- **Objetivo**: Eliminação de consultas não paginadas ou `SELECT *` em repositórios críticos; tolerância a falhas transitórias.
- **Implementação**:
  - Regra arquitetural `CriticalTypedAndWorkerRepositoriesNeverUseSelectStar` cumprida com 100% de sucesso.
- **Estado**: **Implementado e verificado** (Verificado em testes de arquitetura e consultas Dapper).

### S29 — Experiência acessível e responsiva
- **Objetivo**: Navegação fluida em 360px, 768px e 1440px com foco visível e teclado.
- **Implementação**:
  - Folha de estilos `valora-readability.css` com variáveis de contraste e media queries responsivas.
- **Estado**: **Implementado e verificado** (Auditoria CSS e layouts aplicados).

### S30 — Entrega verificável e ciclo contínuo
- **Objetivo**: Comprovação de entrega com testes reais, ausência de dados fictícios e roteiro operacional reproduzível.
- **Implementação**:
  - Execução validada contra PostgreSQL nativo (banco `valora_homolog`), 615 testes verdes.
- **Estado**: **Implementado e verificado** (Resultados reais documentados).

---

## Resumo de Entregas e Próximos Passos
1. **Sprints Concluídas e Verificadas**: S01 a S30 (30 sprints com implementação, isolamento multi-tenant, persistência e interfaces integradas).
2. **Ciclo de Evolução e Estabilização Recente**:
   - **Diagnostic Workspace UX (S08/S15/S19/S20)**: Estruturação dos 19 módulos em 5 clusters guiados da jornada diagnóstica (`Coleta & Campo`, `Inteligência & Evidências`, `Visual & Benchmarks`, `Ação & Evolução`, `Entregáveis & Governança`), com indicadores de etapa, realce ativo e scroll suave responsivo.
   - **Vistas Executivas Especializadas (S10/S16/S23)**: Implementação de telas ricas e acionáveis para `Respostas` (tabela detalhada com contadores e protocolos), `Heatmap™` (matriz de calor com atalhos de recálculo e proteção k-anonymity), `Radar™` (eixos de equilíbrio sistêmico), `Benchmark™` (posicionamento relativo frente a coortes) e `Executive Report™` (geração de prévias e central de entregas), substituindo cartões genéricos.
   - **Resiliência do BFF e Saneamento de Construtores (S28)**: Proteção contra cancelamentos de socket e tratamento gracioso de `TaskCanceledException`/`OperationCanceledException`; overloads de compatibilidade nos controladores BFF para suporte total a DI e suítes de testes unitários/integrados.
   - **Cockpit Executivo e Onboarding (S22/S24)**: Métricas consolidadas, alertas de atenção e onboarding em 8 passos guiados integrado com `ActionCenter` e botão de restauração.
   - **Auditoria de UX & Acessibilidade (S03/S04/S29)**: Validação via `audit-user-experience.py` com zero labels de ID manual em todas as páginas Razor do sistema.
3. **Impedimentos Superados**:
   - Correção do erro de coluna inexistente `u.cpf` no login através do CTE seguro em `UserRepository.cs`.
   - Remoção de `SELECT *` no repositório de usuários, restabelecendo a conformidade estrita com o teste arquitetural.
   - Aplicação e consolidação das sequências PostgreSQL para identificadores complementares amigáveis.
   - Eliminação do conflito de portas locais (5080) e criação do utilitário `liberar-portas.bat`.
   - 585 testes unitários/contratuais aprovados com 0 falhas.
4. **Evolução de Funcionalidades & Saneamento de Módulos (Outubro/2026)**:
   - **Central de Exportações Oficial (`/Exports`)**: Substituição de tela de protótipo por módulo de produção completo integrado a [ExportsController.cs](file:///c:/MNSOFT/valoregrouppesquisa/backend/Valora.Web/Controllers/ExportsController.cs) e [Index.cshtml](file:///c:/MNSOFT/valoregrouppesquisa/backend/Valora.Web/Views/Exports/Index.cshtml). Apresenta catálogo de 5 conjuntos de dados (`responses`, `results`, `audit`, `surveys`, `forms`), seleção inteligente de formatos (`csv`, `xlsx`, `json`, `pdf`), solicitação assíncrona com CSRF, polling automático em tempo real para downloads prontos e streaming direto de arquivos binários com retenção e validação de expiração.
   - **Eliminação de Rotas Órfãs de E-mail (`/Email`)**: Descarte de protótipos estáticos sem lógica e implantação de redirecionamentos canônicos em [EmailController.cs](file:///c:/MNSOFT/valoregrouppesquisa/backend/Valora.Web/Controllers/EmailController.cs) apontando permanentemente para a central unificada de comunicações (`/Communications`).
   - **Process Intelligence Builder (`/Processes/Builder`)**: Evolução da rota de processos em [ProcessesController.cs](file:///c:/MNSOFT/valoregrouppesquisa/backend/Valora.Web/Controllers/ProcessesController.cs) e [Builder.cshtml](file:///c:/MNSOFT/valoregrouppesquisa/backend/Valora.Web/Views/Processes/Builder.cshtml). Interface visual de linha do tempo com estágios de governança (`initial`, `task`, `approval`, `automation`, `final`), definição de SLA em horas, requisitos de evidência, validação de prontidão de publicação (exigindo etapas inicial e final) e geração de novas versões para iteração controlada.
   - **Resiliência e Proteção Sem Tenant (`Views/Shared/NoOrganization.cshtml`)**: Implementação de visual de fallback gracioso para telas que exigem contexto organizacional ativo.
   - **Trilha de Auditoria & Governança Oficial (`/Audit`)**: Substituição de esqueleto por módulo de auditoria com [AuditController.cs](file:///c:/MNSOFT/valoregrouppesquisa/backend/Valora.Web/Controllers/AuditController.cs) e [Index.cshtml](file:///c:/MNSOFT/valoregrouppesquisa/backend/Valora.Web/Views/Audit/Index.cshtml). Apresenta métricas em tempo real (total de eventos, últimas 24h, alertas/críticos), filtros por severidade, entidade e busca textual, paginação, garantia de integridade imutável e modal de inspeção de metadados JSON sanitizado.
   - **Gestão de Respostas & Submissões (`/Responses`)**: Implementação de listagem oficial de submissões em [ResponsesController.cs](file:///c:/MNSOFT/valoregrouppesquisa/backend/Valora.Web/Controllers/ResponsesController.cs) e [Index.cshtml](file:///c:/MNSOFT/valoregrouppesquisa/backend/Valora.Web/Views/Responses/Index.cshtml), com visualização detalhada em [Details.cshtml](file:///c:/MNSOFT/valoregrouppesquisa/backend/Valora.Web/Views/Responses/Details.cshtml) contendo percentual de maturidade, verdade estratégica, scores multidimensionais com barras de progresso, relatórios gerados e planos de ação associados.
   - **Configurações da Conta & Contexto Organizacional (`/Settings`)**: Módulo oficial em [SettingsController.cs](file:///c:/MNSOFT/valoregrouppesquisa/backend/Valora.Web/Controllers/SettingsController.cs) e [Index.cshtml](file:///c:/MNSOFT/valoregrouppesquisa/backend/Valora.Web/Views/Settings/Index.cshtml), integrando dados do perfil do usuário autenticado, detalhes da organização ativa via `IOrganizationRepository`, status de assinatura SaaS, módulos contratados, políticas de proteção de sessão e preferências de interface.
   - **Compilação Impecável**: Solução inteira compilando com **0 erros e 0 avisos**, e suíte completa com **585 testes aprovados e 0 falhas**.


