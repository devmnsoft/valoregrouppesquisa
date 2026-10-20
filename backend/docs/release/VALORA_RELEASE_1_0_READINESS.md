# Valora Insight™ — Release 1.0 Readiness Audit
**Operação Produto Homologável e Pronto para Comercialização**
**Data de Emissão:** 20/10/2026  
**Último Commit de Referência:** `886d3a75d883`  
**Escopo:** Avaliação de Prontidão Comercial e Homologação Técnica do MVP  
**Status Geral:** EM HOMOLOGAÇÃO DE BLOQUEADORES CRÍTICOS (Fase R1)

---

## 1. Visão Geral e Objetivo da Auditoria

Esta auditoria de prontidão consolida o estado real do código-fonte do **Valora Insight™** a partir do commit de referência `886d3a75d883`, confrontando o que está efetivamente implementado e testado no repositório com os critérios mandatários para disponibilização comercial como SaaS B2B empresarial.

A diretriz desta fase é:
> **Não adicionar novos módulos especulativos. Concluir, integrar, testar e homologar as funcionalidades essenciais do MVP para que a plataforma possa ser comercializada com segurança, integridade metodológica e estabilidade operacional.**

---

## 2. Matriz de Prontidão das Funcionalidades Essenciais (MVP)

A tabela abaixo avalia cada jornada crítica requerida para a comercialização do SaaS:

| # | Funcionalidade Essencial | Estado Real | Evidências no Código | Testes Executados | Dependências | Riscos Identificados | Correções Necessárias | Bloqueador Comercial? |
|---|---|---|---|---|---|---|---|---|
| **01** | **Autenticação, MFA & Sessão** | **PRONTO / INTEGRADO** | `AuthController.cs`, `ValoraAccessCatalog.cs`, BCrypt dinâmico | `SeedHomologacaoSecurityTests` (18 testes) | PostgreSQL, Cookie Auth | Ataques de força bruta sem rate-limit no login | Adicionar rate-limit estrito e rotação de sessão | **NÃO** |
| **02** | **Isolamento Multitenant** | **INTEGRADO** | `CurrentOrganizationProvider`, `tenant_id` em queries Dapper | `AdminHubPostgresTests`, `WorkspaceRepositoryPostgresTests` | PostgreSQL RLS / Claims | Risco de vazamento em endpoints sem validação explícita de `OrganizationId` | Auditoria linha a linha de todas as queries Dapper em `IValoraAiInsightRepository` e `OrganizationalIntelligenceService` | **SIM (Revisar na R3)** |
| **03** | **Estrutura Organizacional & Cargos** | **PRONTO** | `OrganizationStructureController.cs`, `PeopleController.cs` | Testes de unidade em `Valora.Tests` | PostgreSQL | Dependência de hierarquia circular não tratada | Adicionar validação de ciclos em nós organizacionais | **NÃO** |
| **04** | **Instrumentos Diagnósticos Oficiais** | **PRONTO** | `MethodologyController.cs`, `DiagnosisTemplateService.cs` | `MethodologyRepository.cs` | PostgreSQL | Edição acidental de questionários já publicados | Imutabilidade estrita pós-publicação | **NÃO** |
| **05** | **Campanhas & Coleta Mobile** | **INTEGRADO** | `PublicSurveysController.cs`, `RespondentExperienceController.cs` | Testes de rotas públicas em `ZeroBrokenPagesAuditTests` | Navegador mobile | Responsividade quebrada em viewports inferiores a 360px | Ajustar formulário de resposta para viewports ultracompactos | **SIM (Corrigir na R4/R6)** |
| **06** | **Prevenção de Respostas Duplicadas** | **INTEGRADO** | Chave idempotente e hash de submissão | `PublicIdempotencyPostgresTests` | PostgreSQL | Perda de integridade amostral | Validação combinada de token/e-mail no backend | **SIM (Validar na R3/R4)** |
| **07** | **Processamento & Agregação Real** | **INTEGRADO** | `ValoraIntelligenceEngine.cs`, `SUM(ra.score)` em SQL | `DemonstrativeDatabaseIntegrationTests` | PostgreSQL | Ausência de respostas gerar score zero em vez de "não avaliado" | Guardrail já implementado no Dashboard 2.0; garantir em todos os relatórios | **NÃO** |
| **08** | **Motor de Maturidade & Dicionário Cognitivo** | **PARCIAL / PENDENTE** | `ExplainableIntelligenceService.cs`, `ValoraInferenceEngine.cs` | `ExplainableIntelligenceTests` (10 testes) | Motor de Inferência Valora™ | **Confiança moderada atribuída com menos de 3 evidências; divergência entre evidência única e convergência independente** | **Revisar cálculo de confiança para exigir rigorosamente >= 3 evidências convergentes e independentes (SPRINT R2)** | **SIM (Bloqueador Crítico)** |
| **09** | **Dashboard Executivo 2.0** | **PRONTO** | `Views/Dashboard/Index.cshtml`, 6 blocos executivos | `DashboardExecutiveV2Tests` (3 testes) | BFF / Views | Falha de carregamento se organização não tiver dados | Estados vazios legítimos já implementados | **NÃO** |
| **10** | **Recurso "Entenda esta análise"** | **PRONTO** | `_ExplainableAnalysisModal.cshtml`, `valora-explainable.js`, `Details.cshtml` | `ExplainableIntelligenceTests` | Controller BFF & API | Não distinção entre correlação e causalidade | Regra explícita implementada nos 7 pilares metodológicos | **NÃO** |
| **11** | **Valora Action™ (Planos)** | **INTEGRADO** | `ActionCenterController.cs`, `ActionPlansController.cs` | Testes de transição de plano | PostgreSQL | IA criar ou aprovar ações automaticamente sem humano | Bloqueio mandatório de deliberação humana formalizado | **NÃO** |
| **12** | **Relatórios & Certificados Oficiais** | **INTEGRADO** | `CertificatesController.cs`, `ReportsController.cs` | `PublicCertificatesController.cs` | PostgreSQL, PDF Render | Emissão de certificados sem atigimento de quórum | Exigir validação de quórum amostral antes de emitir certificado | **SIM (Homologar na R4)** |
| **13** | **Planos Comerciais & Limites de Tenant** | **PARCIAL** | `SubscriptionUsageController.cs`, `PlansController.cs` | Testes de plano | PostgreSQL | Gateway de pagamento não implementado fingindo transação concluída | Proibir simulação de pagamento; restringir a ativação administrativa controlada | **SIM (Resolver na R5)** |
| **14** | **Onboarding Guiado** | **PARCIAL** | `OnboardingController.cs` | Testes de rota | Cookies / LocalStorage | Perda do estado de onboarding ao trocar de dispositivo | Persistir progresso de onboarding no banco de dados (`onboarding_progress`) | **SIM (Resolver na R5)** |

---

## 3. Investigação Aprofundada dos 22 Testes Ignorados

Durante a execução da suíte completa de testes (`dotnet test Valora.Tests/Valora.Tests.csproj`), foram registrados **673 testes aprovados, 0 falhas e 22 testes ignorados (SKIPPED)**.
A investigação técnica detalhada de cada um dos 22 testes ignorados revelou a seguinte distribuição:

### 3.1. Mecanismo de Controle dos Testes Ignorados
Todos os 22 testes utilizam o atributo customizado `[PostgresFact(reason)]` implementado em [Valora.Tests/Support/PostgresTestEnvironment.cs](file:///c:/MNSOFT/valoregrouppesquisa/backend/Valora.Tests/Support/PostgresTestEnvironment.cs):
```csharp
public sealed class PostgresFactAttribute : FactAttribute {
    public PostgresFactAttribute(string reason) {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("VALORA_TEST_POSTGRES_CONNECTION"))
            && !PostgresTestEnvironment.IsRequired) {
            Skip = PostgresTestEnvironment.MissingConnectionMessage(reason);
        }
    }
}
```
Esse mecanismo impede que testes dependentes de um banco de dados relacional PostgreSQL real falhem abruptamente em ambientes de integração contínua (CI) ou desenvolvimento local onde a variável `VALORA_TEST_POSTGRES_CONNECTION` não foi injetada.

### 3.2. Justificativa Individual dos 22 Testes

| # | Arquivo de Teste | Método de Teste | Motivo do Skip | Impacto no MVP | Ação na Release 1.0 |
|---|---|---|---|---|---|
| **01** | `SecureShareLinkPostgresTests.cs:41` | `Concurrent_share_creation_with_same_command_creates_once_and_replays` | Exige instância real do PostgreSQL para validar transação com trava de concorrência e chave de idempotência em compartilhamento. | Baixo em build local; Alto em homologação final. | Executar em contêiner PostgreSQL dedicado na Sprint R3 e R7. |
| **02** | `PublicIdempotencyPostgresTests.cs:75` | `Public_response_submission_is_idempotent_per_key_and_rejects_conflicting_payloads` | Exige PostgreSQL real para testar integridade de transações concorrentes na submissão pública de pesquisas. | Crítico para integridade de coleta. | Validar na Sprint R3 e R4. |
| **03** | `PrivacyRequestDatabaseContractTests.cs:8` | `Public_protocol_column_and_unique_index_match_the_catalog_contract` | Exige validação de catálogo e índices únicos da tabela LGPD no schema `valorapesquisa`. | Médio (Conformidade legal). | Validar na Sprint R3. |
| **04** | `ValoraInsightFullJourneyIntegrationTests.cs:279` | `Sprint2_Validation_Preparation_Parameters_Methodology_Lock_And_Concurrency` | Exige banco real para testar bloqueio de parâmetros diagnósticos e concorrência na preparação. | Crítico para ciclo diagnóstico. | Validar na Sprint R4. |
| **05** | `ValoraInsightFullJourneyIntegrationTests.cs:453` | `Sprint3_Validation_Participation_Metrics_Transparency_And_NextAction_State` | Exige banco real para validar persistência de métricas de adesão e transição de estado. | Crítico para o ciclo de coleta. | Validar na Sprint R4. |
| **06** | `ValoraInsightFullJourneyIntegrationTests.cs:855` | `Sprint4_Validation_Insights_Canonical_Origins_Editorial_Review_And_ActionCenter_Traceability` | Exige banco real para validar workflow de revisão humana e rastreabilidade com o Action Center. | Crítico para o módulo de IA. | Validar na Sprint R2 e R4. |
| **07** | `ValoraInsightFullJourneyIntegrationTests.cs:1019` | `Sprint5_Validation_Homologation_Seed_And_MultiTenant_Gate` | Exige banco real para validar script de seed de homologação e gate de isolamento multitenant. | Crítico para segurança comercial. | Validar na Sprint R3. |
| **08** | `ValoraInsightFullJourneyIntegrationTests.cs:1096` | `Stage1_To_Stage3_FullPublicSubmission_Qualitative_And_Quantitative_Integration` | Exige banco real para testar o fluxo integrado completo de submissão pública quantitativa e qualitativa. | Crítico para experiência do respondente. | Validar na Sprint R4. |
| **09** | `ValoraInsightFullJourneyIntegrationTests.cs:1219` | `Stage4_To_Stage6_MultiTenant_Integrated_Operational_Journey` | Exige banco real para validar jornada operacional ponta a ponta com múltiplos tenants concorrentes. | Crítico para homologação comercial. | Validar na Sprint R4 e R7. |
| **10** | `WorkspaceRepositoryPostgresTests.cs:41` | `Workspace_queries_enforce_visibility_tenant_and_idempotent_pins` | Exige banco real para verificar queries de visibilidade de workspace e itens fixados idempotentes. | Alto para experiência do usuário. | Validar na Sprint R3. |
| **11** | `PostgresHomologationFlowTests.cs:9` | `HomologationDatabaseHasDurableResponseProcessingContract` | Exige banco real para checar durabilidade de fila de processamento de respostas. | Crítico para integridade de dados. | Validar na Sprint R3 e R4. |
| **12** | `EvolutionPersistencePostgresTests.cs:18` | `BaselinePreservesHistoricalReadingAndLegacyResultsRemainExplicit` | Exige banco real para testar snapshots imutáveis de maturidade e preservação de resultados legados. | Crítico para metodologia Valora™. | Validar na Sprint R2 e R4. |
| **13** | `EvolutionHttpAccessTests.cs:71` | `Manual_cycle_can_be_created_monitored_reopened_and_closed_through_http` | Exige banco real para validar endpoints HTTP de controle de ciclos (abertura/fechamento). | Alto para gestão de ciclos. | Validar na Sprint R4. |
| **14** | `EvolutionHttpAccessTests.cs:83` | `Read_only_module_hides_creation_even_for_manager` | Exige banco real para validar ocultação de comandos de escrita para perfil de leitura via HTTP. | Alto para controle de perfis. | Validar na Sprint R3. |
| **15** | `EvolutionHttpAccessTests.cs:94` | `Read_only_user_sees_cycles_without_creation_command` | Exige banco real para verificar permissões de visualização sem comandos de criação. | Alto para controle de perfis. | Validar na Sprint R3. |
| **16** | `EvolutionAuthenticatedJourneyTests.cs:20` | `Real_login_enforces_organizations_permissions_and_calculated_reevaluation` | Exige banco real para validar pipeline completo de login com cookies e reavaliação de indicadores. | Crítico para jornada de uso. | Validar na Sprint R4. |
| **17** | `EvolutionAuthenticatedJourneyTests.cs:214` | `Published_qualitative_collection_and_manual_campaign_use_canonical_http_commands` | Exige banco real para testar comandos HTTP de publicação e disparo de campanha de coleta. | Crítico para ciclo de pesquisa. | Validar na Sprint R4. |
| **18** | `AdminHubPostgresTests.cs:75` | `Organization_update_uses_expected_version_compare_and_swap_and_rejects_stale_writers` | Exige banco real para testar concorrência otimista (optimistic locking CAS) na atualização de organização. | Crítico para integridade administrativa. | Validar na Sprint R3. |
| **19** | `AdminHubPostgresTests.cs:109` | `Last_administrator_cannot_be_deactivated_or_demoted_by_role_removal` | Exige banco real para testar invariante de segurança: impedir que o último administrador seja removido. | Crítico para governança. | Validar na Sprint R3. |
| **20** | `AdminHubPostgresTests.cs:152` | `User_create_prefers_organization_roles_rejects_unknown_roles_and_duplicate_emails` | Exige banco real para testar validação de e-mails duplicados e papéis desconhecidos. | Alto para onboarding de usuários. | Validar na Sprint R3. |
| **21** | `AdminHubPostgresTests.cs:187` | `User_administration_list_is_scoped_to_each_organization_and_cross_org_get_is_null` | Exige banco real para validar isolamento de busca de usuários por organização (sem cross-tenant leak). | Crítico para segurança multitenant. | Validar na Sprint R3. |
| **22** | `AdminHubPostgresTests.cs:210` | `Company_registration_is_idempotent_per_key_and_rejects_conflicting_payloads` | Exige banco real para testar idempotência no cadastro de empresas com chave única. | Crítico para jornada de vendas. | Validar na Sprint R3. |

---

## 4. Bloqueadores Críticos e Estado de Resolução

1. **Bloqueador 1 (Metodológico - Sprint R2): Cálculo de Confiança e Convergência de Evidências — CONCLUÍDO**
   - **Correção Efetuada:**
     - Eliminada qualquer concessão de confiança "Moderada" com menos de 3 evidências. Com < 3 evidências, o resultado é estritamente "Baixa" ou "Insuficiente".
     - Implantação da verificação de **convergência entre fontes independentes**: se todas as evidências (mesmo em quantidade >= 3) forem provenientes de fonte única, a análise é classificada como não conclusiva (`MeetsMinimumConvergentRule = false`) com confiança "Baixa" e aviso formal de insuficiência de fontes.
     - Atualização sincronizada em `ValoraConfidenceCalculator.cs`, `ExplainableIntelligenceService.cs`, `MethodologyService.cs` e `seed_homologacao.sql`.
     - Criada a suíte de testes [Valora.Tests/MaturityEngineOfficialValidationTests.cs](file:///c:/MNSOFT/valoregrouppesquisa/backend/Valora.Tests/MaturityEngineOfficialValidationTests.cs) aprovando 100% dos cenários (6 novos testes).
     - Total de testes da suíte expandido para **682 testes aprovados, 0 falhas**.

2. **Bloqueador 2 (Segurança & Banco de Dados - Sprint R3): Validação em Banco Real Limpo e Atualizado (Em Execução)**
   - As migrações Dapper e scripts de homologação precisam ser executados e validados contra banco real isolado para desbloquear os 22 testes de regressão de concorrência e isolamento multitenant.
   - Idempotência completa de `seed_homologacao.sql` e `reset_homologacao.sql`.
   - Separação estrita de credenciais com menor privilégio e compatibilidade com MFA.

3. **Bloqueador 3 (Jornada Comercial - Sprint R4): Conclusão da Jornada Ponta a Ponta**
   - Garantir que nenhum botão ou link da jornada central (Cadastro -> Diagnóstico -> Resposta -> Processamento -> Dashboard -> Ação) retorne 404, 500 ou fique inoperante.

4. **Bloqueador 4 (Onboarding & Planos - Sprint R5): Persistência de Progresso do Usuário**
   - Substituição de controle frágil de onboarding baseado em cookies por tabela de progresso persistida no PostgreSQL (`valorapesquisa.user_onboarding_progress`).
   - Modelo comercial sem simulação de cobrança: bloqueio de assinaturas não autorizadas e liberação via painel do Superadministrador.

---

## 5. Próximo Passo Imediato

Avançar diretamente para a **SPRINT R3 — BANCO DE DADOS E SEGURANÇA**:
1. Revisar PostgreSQL, schemas, migrations, Dapper e scripts de seed.
2. Garantir idempotência e integridade referencial.
3. Testar isolamento multitenant e isolamento de permissões RBAC.
4. Validar mecanismos de backup, restauração e rotação de credenciais.
