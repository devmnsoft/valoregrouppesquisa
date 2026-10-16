# Pendências reais e Homologação — QA Funcional e Técnico

Registro da evolução e homologação do Valora Insight (devmnsoft/valoregrouppesquisa).
Atualização: Outubro/2026 — Sprints 1 a 6 (Confiabilidade, Navegação, Preparação, Rastreabilidade, Usabilidade e Homologação).

## 1. Status Real dos Ambientes e Infraestrutura

| Componente | Estado | Evidência / Validação | Observação |
|---|---|---|---|
| **.NET SDK** | .NET 10.0 instalado e funcional | Compilação com êxito de todos os projetos (Domain, Application, Infrastructure, Api, Web, Tests) | `dotnet build` com 0 erros. |
| **PostgreSQL** | PostgreSQL 16 operacional localmente | Suíte de integração executada com sucesso via `$env:VALORA_TEST_POSTGRES_CONNECTION` | Banco `valora_sprints_test` validado com multitenant, migrations e seeds idempotentes. |
| **Suíte de Testes** | 607 testes aprovados (100% de sucesso) | `dotnet test Valora.Tests/Valora.Tests.csproj` → Aprovado: 607, Falha: 0, Ignorado: 0 | Cobertura integral das jornadas de regressão T01 a T15 e cenários de persistência real. |

---

## 2. Entregas Consolidadas por Sprint

### Sprint 1 — Confiabilidade do Contexto, Autenticação e Contratos BFF
- **Sessão e Troca de Organização (`BffAuthenticationService.cs`)**:
  - Definido o armazenamento de sessão do servidor (`IDistributedBffSessionStore`) como fonte primária da verdade.
  - Implementada coordenação de concorrência por semáforo (`RefreshLocks`) por ticket de sessão para prevenir race conditions.
  - Na troca de organização (`SelectOrganizationAsync`), a persistência do novo contexto na sessão do servidor é executada antes da emissão do cookie. Se a renovação do cookie falhar, a sessão no servidor é revertida ao contexto anterior.
  - Registro de auditoria fidedigno: registra reversão de contexto se o rollback for concluído, ou divergência se falhar (sem mensagens fictícias de "contexto não modificado").
- **Isolamento no Dashboard (`dashboard-page.js`)**:
  - Implementado `resetDashboardData()`: os dados da organização anterior são limpos do DOM imediatamente no início da carga de um novo contexto.
  - Verificação de geração (`thisGeneration !== currentLoadGeneration`): requisições defasadas de organizações anteriores são descartadas sem alterar a visualização.
  - Estados explícitos de ciclo de vida do contexto: `verifying`, `platform_mode`, `org_selected`, `selection_required`, `session_expired`, `access_denied`, `unavailable`.
- **Contratos BFF Explícitos (`BffBodyReader`, `BffWorkspaceController`, `BffDiagnosticOperationsController`)**:
  - Rejeição de mutações `PUT`/`POST` sem corpo com HTTP 400 estruturado (`REQUEST_BODY_REQUIRED`), eliminando inferências frágeis por sufixo de URL.
  - Preservação explícita de comandos sem corpo (ações `/open`, rotas de transição de campanha, relatórios executivos, downloads binários e HTTP 204).

### Sprint 2 — Navegação Menor e Páginas com Propósito
- **Consolidação em 7 Áreas Canônicas**:
  1. *Visão geral*: Prioridades, pendências, métricas essenciais e próximos passos.
  2. *Diagnósticos*: Preparação, questionários, campanhas e coleta.
  3. *Resultados e evidências*: Leitura consolidada, interpretações e relatórios executivos.
  4. *Planos de ação*: Ações originadas em evidências, acompanhamento e conclusão.
  5. *Evolução*: Acompanhamento longitudinal e comparações metodologicamente válidas.
  6. *Organização*: Pessoas, unidades, departamentos, acessos e configurações do cliente.
  7. *Administração da plataforma*: Gestão global (tenants, licenças, auditoria e módulos).
- **Tratamento de Páginas Planejadas (`InDevelopmentViewModel`, `InDevelopment.cshtml`)**:
  - Substituídos botões sem ação e telas estéreis por apresentação informativa padronizada contendo os 5 elementos obrigatórios: Propósito, Público-alvo, Informações recebidas, Resultado entregue e Alternativa funcional atual com link direto.
  - Conectadas rotas ainda planejadas (ex: `/Diagnostics/{id}/InDevelopment`) eliminando promessas falsas de datas.

### Sprint 3 — Preparação e Coleta do Diagnóstico
- **Responsabilidade Explícita**:
  - Eliminada a atribuição silenciosa do criador como responsável.
  - Opção padrão do seletor definida como "Não definido".
  - Validação de que o responsável pertence à organização e está ativo.
- **Controle de Concorrência Otimista (`DiagnosticWorkspaceRepository.cs`)**:
  - Rejeição estrita de versões inválidas: `rowVersion <= 0` lança `ArgumentException` imediatamente.
  - Concorrência verificada diretamente no SQL de atualização: `UPDATE valorapesquisa.surveys ... WHERE coalesce(row_version, 1) = @expectedRowVersion`.
  - Conflitos concorrentes disparam `DiagnosticConcurrencyException` retornando HTTP 409 estruturado sem perda dos dados preenchidos pelo usuário.
- **Critérios de Prontidão e Publicação**:
  - Rascunhos incompletos podem ser salvos livremente.
  - Publicação bloqueada por lista objetiva de pendências (metodologia, período, perguntas e responsável).

### Sprint 4 — Evidência, Decisão e Ação com Rastreabilidade
- **Origem Preservada na Central de Ações**:
  - Ações criadas a partir de evidências mantêm vínculos de rastreabilidade (`origin_type`, `origin_id`, `diagnostic_id`).
  - Distinção entre ação originada em evidência e ação criada manualmente.
- **Formulário Operacional e Ciclo de Vida**:
  - Campos de execução: título, objetivo, justificativa, responsável, prazo, prioridade, resultado esperado e critério de conclusão.
  - Indicador de atraso calculado em tempo real contra o prazo estabelecido.
- **Reavaliação Metodológica**:
  - Medições vinculadas à linha de base (baseline).
  - Casos com instrumentos ou pesos incompatíveis são sinalizados como inconclusivos (`inconclusive`), impedindo conclusões causais errôneas.

### Sprint 5 — Design Consistente, Formulários Utilizáveis e Acessibilidade
- **Usabilidade e Formulários**:
  - Proteção contra duplo envio com desabilitação de botões e estado de salvamento visual.
  - Foco automático no primeiro campo com erro e preservação integral dos dados após falhas de rede.
  - Confirmações contextuais para operações destrutivas.
- **Responsividade e Acessibilidade**:
  - Layout validado em 360px (mobile), 768px (tablet) e 1440px (desktop), sem sobreposição de menu ou rolagem horizontal involuntária.

### Sprint 6 — Dados de Homologação, Testes Reais e Documentação Confiável
- **Seed de Homologação Idempotente (`seed_homologacao.sql`)**:
  - Cenários completos: Cliente Demonstração (ativo, com diagnóstico publicado, respostas, evidências, ações normais e em atraso, rascunho de diagnóstico com pendências, ciclo de evolução e ciclo inconclusivo), Cliente Início (onboarding limpo com 0 pesquisas), e Cliente Isolamento (segregação de tenant).
  - Senha padrão segura de teste: `Admin@123` com BCrypt.
  - Testado com dupla execução consecutiva garantindo idempotência total.
- **Provas de Teste Fortalecidas (`ValoraInsightRegressionT01ToT15Tests.cs`)**:
  - **T13**: Validada coordenação de concorrência com latência de rede simulada e sobreposição real entre requisições paralelas, comprovando exatamente 1 chamada de renovação (`Assert.Equal(1, refreshApiCallCount)`).
  - **T15**: Documentado escopo explícito de validação de contratos e orquestração do pipeline BFF, com prova de persistência real transferida para a suíte PostgreSQL.
  - **Persistência Real**: Suíte de integração `ValoraInsightFullJourneyIntegrationTests` valida o fluxo completo ponta a ponta no PostgreSQL.

---

## 3. Próximas Prioridades e Recomendações
1. **Homologação Assistida em Navegador**: Executar roteiro com os usuários dos cenários de homologação (`admin.demonstracao@valora.local`, `admin.inicio@valora.local`).
2. **Auditoria LGPD e Retenção**: Aprofundar automação de expurgo temporal de respostas anonimizadas conforme política da organização.

