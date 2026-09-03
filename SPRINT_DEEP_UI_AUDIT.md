# Auditoria automatizada das views internas

- Views de página verificadas: **190**.
- Views dos módulos prioritários: **66**.
- Razor CSS com diretiva não escapada: **0**.
- Views com `@model dynamic`: **0**.
- Possíveis identificadores técnicos em inputs: **0**.

> O layout interno injeta orientação contextual, contexto de organização, mensagens, loading e confirmação. A classificação abaixo registra também os recursos locais de cada view.

| View | Classificação | Cobertura local | Pontos de atenção |
|---|---|---|---|
| `backend/Valora.Web/Views/Account/ForgotPassword.cshtml` | funcional, cobertura global complementa a tela | title, action, form | feedback apenas global |
| `backend/Valora.Web/Views/Account/Login.cshtml` | funcional e bem desenhada | title, action, form, feedback, responsive | — |
| `backend/Valora.Web/Views/Account/Register.cshtml` | funcional, cobertura global complementa a tela | title, action, form, responsive | feedback apenas global |
| `backend/Valora.Web/Views/Account/ResetPassword.cshtml` | funcional, cobertura global complementa a tela | title, action, form | feedback apenas global |
| `backend/Valora.Web/Views/ActionCenter/CreatePlan.cshtml` | funcional e bem desenhada | title, action, form, feedback, responsive | — |
| `backend/Valora.Web/Views/ActionCenter/Index.cshtml` | funcional, cobertura global complementa a tela | title, action, emptyState, responsive, manual | feedback apenas global |
| `backend/Valora.Web/Views/ActionCenter/ItemDetails.cshtml` | funcional, cobertura global complementa a tela | title, action, form, responsive | feedback apenas global |
| `backend/Valora.Web/Views/ActionCenter/Items.cshtml` | requer revisão específica | title, responsive | sem ação local; feedback apenas global; sem empty state local |
| `backend/Valora.Web/Views/ActionCenter/PlanDetails.cshtml` | requer revisão específica | title, responsive | sem ação local; feedback apenas global; sem empty state local |
| `backend/Valora.Web/Views/ActionCenter/Plans.cshtml` | funcional, cobertura global complementa a tela | title, action, emptyState, responsive | feedback apenas global |
| `backend/Valora.Web/Views/ActionPlans/Index.cshtml` | funcional e bem desenhada | title, action, form, filters, feedback, responsive | — |
| `backend/Valora.Web/Views/AdminHub/Audit.cshtml` | funcional, cobertura global complementa a tela | title, action, form, emptyState, responsive | feedback apenas global |
| `backend/Valora.Web/Views/AdminHub/CreateOrganization.cshtml` | funcional e bem desenhada | title, action, form, feedback, responsive | — |
| `backend/Valora.Web/Views/AdminHub/CreateUser.cshtml` | funcional e bem desenhada | title, action, form, emptyState, feedback, responsive | — |
| `backend/Valora.Web/Views/AdminHub/Index.cshtml` | requer revisão específica | title, responsive | sem ação local; feedback apenas global; sem empty state local |
| `backend/Valora.Web/Views/AdminHub/OrganizationDetails.cshtml` | funcional, cobertura global complementa a tela | title, emptyState, responsive | sem ação local; feedback apenas global |
| `backend/Valora.Web/Views/AdminHub/Organizations.cshtml` | funcional, cobertura global complementa a tela | title, action, form, emptyState, responsive | feedback apenas global |
| `backend/Valora.Web/Views/AdminHub/Roles.cshtml` | requer revisão específica | title, responsive | sem ação local; feedback apenas global; sem empty state local |
| `backend/Valora.Web/Views/AdminHub/Settings.cshtml` | requer revisão específica | title, responsive | sem ação local; feedback apenas global; sem empty state local |
| `backend/Valora.Web/Views/AdminHub/Users.cshtml` | funcional, cobertura global complementa a tela | title, action, form, emptyState, responsive | feedback apenas global |
| `backend/Valora.Web/Views/Administration/Index.cshtml` | funcional, cobertura global complementa a tela | title, action, emptyState, responsive, manual | feedback apenas global |
| `backend/Valora.Web/Views/Administration/Module.cshtml` | funcional e bem desenhada | title, action, filters, emptyState, feedback, responsive | — |
| `backend/Valora.Web/Views/Advisor/Index.cshtml` | funcional, cobertura global complementa a tela | title, action, form, emptyState | feedback apenas global |
| `backend/Valora.Web/Views/Advisor/List.cshtml` | funcional, cobertura global complementa a tela | title, emptyState | sem ação local; feedback apenas global |
| `backend/Valora.Web/Views/Architecture/MissingOrganization.cshtml` | funcional, cobertura global complementa a tela | title, action, responsive | feedback apenas global; sem empty state local |
| `backend/Valora.Web/Views/Architecture/Studio.cshtml` | funcional e bem desenhada | title, action, form, filters, feedback, responsive | — |
| `backend/Valora.Web/Views/AssistedOperations/Index.cshtml` | funcional e bem desenhada | title, action, form, filters, emptyState, feedback, responsive | — |
| `backend/Valora.Web/Views/Audit/Index.cshtml` | funcional, cobertura global complementa a tela | title, action, feedback, responsive | sem empty state local |
| `backend/Valora.Web/Views/Benchmarks/Cohorts.cshtml` | funcional, cobertura global complementa a tela | action, form | sem título local; feedback apenas global |
| `backend/Valora.Web/Views/Benchmarks/Compare.cshtml` | funcional, cobertura global complementa a tela | action, form | sem título local; feedback apenas global |
| `backend/Valora.Web/Views/Benchmarks/Index.cshtml` | requer revisão específica | emptyState | sem título local; sem ação local; feedback apenas global |
| `backend/Valora.Web/Views/Benchmarks/Insights.cshtml` | requer revisão específica | layout global | sem título local; sem ação local; feedback apenas global; sem empty state local |
| `backend/Valora.Web/Views/Benchmarks/NoOrganization.cshtml` | funcional, cobertura global complementa a tela | title, emptyState | sem ação local; feedback apenas global |
| `backend/Valora.Web/Views/Benchmarks/Privacy.cshtml` | requer revisão específica | emptyState | sem título local; sem ação local; feedback apenas global |
| `backend/Valora.Web/Views/Certificates/Details.cshtml` | funcional, cobertura global complementa a tela | title, action, responsive | feedback apenas global; sem empty state local |
| `backend/Valora.Web/Views/Certificates/Index.cshtml` | funcional e bem desenhada | title, action, form, feedback, responsive, manual | — |
| `backend/Valora.Web/Views/Certificates/Validate.cshtml` | funcional, cobertura global complementa a tela | title, action, form | feedback apenas global |
| `backend/Valora.Web/Views/Communications/Index.cshtml` | funcional, cobertura global complementa a tela | title, action, feedback, responsive | sem empty state local |
| `backend/Valora.Web/Views/Dashboard/Index.cshtml` | funcional e bem desenhada | title, action, emptyState, feedback, responsive, manual | — |
| `backend/Valora.Web/Views/DecisionCenter/Alerts.cshtml` | funcional, cobertura global complementa a tela | title, action, form, emptyState, responsive | feedback apenas global |
| `backend/Valora.Web/Views/DecisionCenter/Index.cshtml` | funcional, cobertura global complementa a tela | title, emptyState, responsive | sem ação local; feedback apenas global |
| `backend/Valora.Web/Views/Decisions/Details.cshtml` | requer revisão específica | title | sem ação local; feedback apenas global; sem empty state local |
| `backend/Valora.Web/Views/Decisions/Index.cshtml` | requer revisão específica | title | sem ação local; feedback apenas global; sem empty state local |
| `backend/Valora.Web/Views/Diagnostics/Workspace.cshtml` | funcional, cobertura global complementa a tela | title, action, feedback, responsive | sem empty state local |
| `backend/Valora.Web/Views/Email/Jobs.cshtml` | funcional, cobertura global complementa a tela | title, action, emptyState | feedback apenas global |
| `backend/Valora.Web/Views/Email/Status.cshtml` | funcional, cobertura global complementa a tela | title, action, emptyState | feedback apenas global |
| `backend/Valora.Web/Views/Email/Templates.cshtml` | funcional, cobertura global complementa a tela | title, action, emptyState | feedback apenas global |
| `backend/Valora.Web/Views/Enterprise/Index.cshtml` | funcional e bem desenhada | title, action, emptyState, feedback | — |
| `backend/Valora.Web/Views/EnvironmentStatus/Index.cshtml` | funcional, cobertura global complementa a tela | title, action, feedback, responsive | sem empty state local |
| `backend/Valora.Web/Views/Evolution/Cycles.cshtml` | funcional e bem desenhada | title, action, form, emptyState, feedback, responsive, manual | — |
| `backend/Valora.Web/Views/Evolution/Details.cshtml` | funcional, cobertura global complementa a tela | title, action, form, responsive | feedback apenas global |
| `backend/Valora.Web/Views/Evolution/Index.cshtml` | funcional, cobertura global complementa a tela | title, action, emptyState, responsive, manual | feedback apenas global |
| `backend/Valora.Web/Views/Experience/Campaigns.cshtml` | funcional e bem desenhada | title, action, form, feedback | — |
| `backend/Valora.Web/Views/Experience/Cockpit.cshtml` | funcional, cobertura global complementa a tela | title, action, feedback, responsive | sem empty state local |
| `backend/Valora.Web/Views/Experience/Help.cshtml` | funcional, cobertura global complementa a tela | title, action, filters | feedback apenas global; sem empty state local |
| `backend/Valora.Web/Views/Experience/Templates.cshtml` | funcional, cobertura global complementa a tela | title, action, filters, feedback | sem empty state local |
| `backend/Valora.Web/Views/Exports/Index.cshtml` | funcional, cobertura global complementa a tela | title, action, emptyState | feedback apenas global |
| `backend/Valora.Web/Views/Forms/Builder.cshtml` | funcional e bem desenhada | title, action, form, feedback | — |
| `backend/Valora.Web/Views/Forms/Index.cshtml` | funcional e bem desenhada | title, action, form, filters, emptyState, feedback, responsive, manual | — |
| `backend/Valora.Web/Views/FreeDiagnostics/Index.cshtml` | funcional e bem desenhada | title, action, form, feedback, responsive | — |
| `backend/Valora.Web/Views/Governance/CreateMeeting.cshtml` | funcional, cobertura global complementa a tela | title, action, form | feedback apenas global |
| `backend/Valora.Web/Views/Governance/CycleDetails.cshtml` | requer revisão específica | title | sem ação local; feedback apenas global; sem empty state local |
| `backend/Valora.Web/Views/Governance/Cycles.cshtml` | funcional, cobertura global complementa a tela | title, emptyState, responsive | sem ação local; feedback apenas global |
| `backend/Valora.Web/Views/Governance/Index.cshtml` | requer revisão específica | title | sem ação local; feedback apenas global; sem empty state local |
| `backend/Valora.Web/Views/Governance/MeetingDetails.cshtml` | requer revisão específica | title | sem ação local; feedback apenas global; sem empty state local |
| `backend/Valora.Web/Views/Governance/Meetings.cshtml` | funcional, cobertura global complementa a tela | title, action, emptyState, responsive | feedback apenas global |
| `backend/Valora.Web/Views/Home/Error.cshtml` | funcional, cobertura global complementa a tela | title, action, responsive | feedback apenas global; sem empty state local |
| `backend/Valora.Web/Views/Home/Index.cshtml` | requer revisão específica | title, responsive | sem ação local; feedback apenas global; sem empty state local |
| `backend/Valora.Web/Views/Indicators/Alerts.cshtml` | requer revisão específica | title | sem ação local; feedback apenas global; sem empty state local |
| `backend/Valora.Web/Views/Indicators/Analytics.cshtml` | requer revisão específica | title | sem ação local; feedback apenas global; sem empty state local |
| `backend/Valora.Web/Views/Indicators/Catalog.cshtml` | requer revisão específica | title | sem ação local; feedback apenas global; sem empty state local |
| `backend/Valora.Web/Views/Indicators/Index.cshtml` | requer revisão específica | title | sem ação local; feedback apenas global; sem empty state local |
| `backend/Valora.Web/Views/Indicators/Measurements.cshtml` | requer revisão específica | title | sem ação local; feedback apenas global; sem empty state local |
| `backend/Valora.Web/Views/Indicators/MissingOrganization.cshtml` | funcional, cobertura global complementa a tela | title, action | feedback apenas global; sem empty state local |
| `backend/Valora.Web/Views/Indicators/Scorecards.cshtml` | requer revisão específica | title | sem ação local; feedback apenas global; sem empty state local |
| `backend/Valora.Web/Views/Indicators/Targets.cshtml` | requer revisão específica | title | sem ação local; feedback apenas global; sem empty state local |
| `backend/Valora.Web/Views/Insights/Details.cshtml` | funcional, cobertura global complementa a tela | title, action, emptyState, responsive | feedback apenas global |
| `backend/Valora.Web/Views/Insights/Index.cshtml` | funcional, cobertura global complementa a tela | title, action, emptyState, responsive | feedback apenas global |
| `backend/Valora.Web/Views/Integrations/Index.cshtml` | funcional, cobertura global complementa a tela | title, action, feedback, responsive | sem empty state local |
| `backend/Valora.Web/Views/Intelligence/Action.cshtml` | requer revisão específica | title | sem ação local; feedback apenas global; sem empty state local |
| `backend/Valora.Web/Views/Intelligence/Benchmark.cshtml` | funcional e bem desenhada | title, action, form, emptyState, feedback, responsive | — |
| `backend/Valora.Web/Views/Intelligence/CausalMap.cshtml` | requer revisão específica | title | sem ação local; feedback apenas global; sem empty state local |
| `backend/Valora.Web/Views/Intelligence/Decisions.cshtml` | requer revisão específica | title | sem ação local; feedback apenas global; sem empty state local |
| `backend/Valora.Web/Views/Intelligence/Evidence.cshtml` | requer revisão específica | title | sem ação local; feedback apenas global; sem empty state local |
| `backend/Valora.Web/Views/Intelligence/Evolution.cshtml` | requer revisão específica | title | sem ação local; feedback apenas global; sem empty state local |
| `backend/Valora.Web/Views/Intelligence/ExecutiveReport.cshtml` | requer revisão específica | title | sem ação local; feedback apenas global; sem empty state local |
| `backend/Valora.Web/Views/Intelligence/Generate.cshtml` | funcional e bem desenhada | title, action, form, emptyState, feedback, responsive | — |
| `backend/Valora.Web/Views/Intelligence/Heatmap.cshtml` | funcional e bem desenhada | title, action, form, emptyState, feedback, responsive | — |
| `backend/Valora.Web/Views/Intelligence/Index.cshtml` | funcional e bem desenhada | title, action, form, emptyState, feedback, responsive | — |
| `backend/Valora.Web/Views/Intelligence/Indices.cshtml` | requer revisão específica | title | sem ação local; feedback apenas global; sem empty state local |
| `backend/Valora.Web/Views/Intelligence/Inference.cshtml` | requer revisão específica | title | sem ação local; feedback apenas global; sem empty state local |
| `backend/Valora.Web/Views/Intelligence/Insights.cshtml` | requer revisão específica | title | sem ação local; feedback apenas global; sem empty state local |
| `backend/Valora.Web/Views/Intelligence/Integrations.cshtml` | requer revisão específica | title | sem ação local; feedback apenas global; sem empty state local |
| `backend/Valora.Web/Views/Intelligence/Journey.cshtml` | requer revisão específica | title | sem ação local; feedback apenas global; sem empty state local |
| `backend/Valora.Web/Views/Intelligence/Metrics.cshtml` | requer revisão específica | title | sem ação local; feedback apenas global; sem empty state local |
| `backend/Valora.Web/Views/Intelligence/Module.cshtml` | funcional e bem desenhada | title, action, filters, emptyState, feedback | — |
| `backend/Valora.Web/Views/Intelligence/OneOnOne.cshtml` | requer revisão específica | title | sem ação local; feedback apenas global; sem empty state local |
| `backend/Valora.Web/Views/Intelligence/PlatformGovernance.cshtml` | requer revisão específica | title | sem ação local; feedback apenas global; sem empty state local |
| `backend/Valora.Web/Views/Intelligence/Processing.cshtml` | funcional e bem desenhada | title, action, form, emptyState, feedback, responsive | — |
| `backend/Valora.Web/Views/Intelligence/Radar.cshtml` | requer revisão específica | title | sem ação local; feedback apenas global; sem empty state local |
| `backend/Valora.Web/Views/Intelligence/Recommendations.cshtml` | requer revisão específica | title | sem ação local; feedback apenas global; sem empty state local |
| `backend/Valora.Web/Views/Intelligence/Review.cshtml` | funcional e bem desenhada | title, action, emptyState, feedback, responsive | — |
| `backend/Valora.Web/Views/Journey/Details.cshtml` | requer revisão específica | title, responsive | sem ação local; feedback apenas global; sem empty state local |
| `backend/Valora.Web/Views/Journey/Index.cshtml` | funcional, cobertura global complementa a tela | title, action, form, responsive | feedback apenas global |
| `backend/Valora.Web/Views/Knowledge/Center.cshtml` | funcional, cobertura global complementa a tela | title, action, form, filters, emptyState, responsive | feedback apenas global |
| `backend/Valora.Web/Views/Knowledge/MissingOrganization.cshtml` | funcional, cobertura global complementa a tela | title, action | feedback apenas global; sem empty state local |
| `backend/Valora.Web/Views/Leadership/Details.cshtml` | funcional, cobertura global complementa a tela | title, action, responsive | feedback apenas global; sem empty state local |
| `backend/Valora.Web/Views/Leadership/Index.cshtml` | requer revisão específica | title, responsive | sem ação local; feedback apenas global; sem empty state local |
| `backend/Valora.Web/Views/Lgpd/Index.cshtml` | funcional, cobertura global complementa a tela | title, action | feedback apenas global; sem empty state local |
| `backend/Valora.Web/Views/Lgpd/Requests.cshtml` | funcional, cobertura global complementa a tela | title, action, form | feedback apenas global |
| `backend/Valora.Web/Views/Methodology/CognitiveMap.cshtml` | funcional e bem desenhada | title, action, filters, emptyState, feedback, responsive | — |
| `backend/Valora.Web/Views/Methodology/Dictionary.cshtml` | funcional e bem desenhada | title, action, filters, emptyState, feedback, responsive | — |
| `backend/Valora.Web/Views/Methodology/Mappings.cshtml` | funcional, cobertura global complementa a tela | title, action, responsive | feedback apenas global; sem empty state local |
| `backend/Valora.Web/Views/Methodology/Overview.cshtml` | funcional, cobertura global complementa a tela | title, action | feedback apenas global; sem empty state local |
| `backend/Valora.Web/Views/Methodology/Studio.cshtml` | funcional, cobertura global complementa a tela | title, action | feedback apenas global; sem empty state local |
| `backend/Valora.Web/Views/Migration/Batch.cshtml` | funcional, cobertura global complementa a tela | title, action, emptyState, responsive | feedback apenas global |
| `backend/Valora.Web/Views/Migration/Batches.cshtml` | funcional, cobertura global complementa a tela | title, action, emptyState, responsive | feedback apenas global |
| `backend/Valora.Web/Views/Migration/Conflicts.cshtml` | funcional, cobertura global complementa a tela | title, action, emptyState, responsive | feedback apenas global |
| `backend/Valora.Web/Views/Migration/CutoverReadiness.cshtml` | funcional, cobertura global complementa a tela | title, action, emptyState, responsive | feedback apenas global |
| `backend/Valora.Web/Views/Migration/DryRun.cshtml` | funcional, cobertura global complementa a tela | title, action, emptyState, responsive | feedback apenas global |
| `backend/Valora.Web/Views/Migration/Index.cshtml` | funcional, cobertura global complementa a tela | title, action, emptyState, responsive | feedback apenas global |
| `backend/Valora.Web/Views/Migration/Reconciliation.cshtml` | funcional, cobertura global complementa a tela | title, action, emptyState, responsive | feedback apenas global |
| `backend/Valora.Web/Views/Migration/Rollback.cshtml` | funcional, cobertura global complementa a tela | title, action, emptyState, responsive | feedback apenas global |
| `backend/Valora.Web/Views/Migration/Upload.cshtml` | funcional, cobertura global complementa a tela | title, action, emptyState, responsive | feedback apenas global |
| `backend/Valora.Web/Views/Onboarding/Step.cshtml` | funcional, cobertura global complementa a tela | title, action | feedback apenas global; sem empty state local |
| `backend/Valora.Web/Views/OneOnOne/Create.cshtml` | funcional e bem desenhada | title, action, form, feedback, responsive | — |
| `backend/Valora.Web/Views/OneOnOne/Details.cshtml` | funcional, cobertura global complementa a tela | title, action, form, responsive | feedback apenas global |
| `backend/Valora.Web/Views/OneOnOne/Index.cshtml` | funcional, cobertura global complementa a tela | title, action, responsive | feedback apenas global; sem empty state local |
| `backend/Valora.Web/Views/OneOnOne/Sessions.cshtml` | funcional, cobertura global complementa a tela | title, action, emptyState, responsive | feedback apenas global |
| `backend/Valora.Web/Views/OperationalIntelligence/Comparisons.cshtml` | funcional, cobertura global complementa a tela | title, action, form | feedback apenas global |
| `backend/Valora.Web/Views/OperationalIntelligence/Recommendations.cshtml` | funcional, cobertura global complementa a tela | title, action | feedback apenas global; sem empty state local |
| `backend/Valora.Web/Views/Operations/Index.cshtml` | funcional, cobertura global complementa a tela | title, action, responsive | feedback apenas global; sem empty state local |
| `backend/Valora.Web/Views/Organization/Index.cshtml` | funcional e bem desenhada | title, action, form, filters, feedback, responsive | — |
| `backend/Valora.Web/Views/OrganizationalCenters/Center.cshtml` | funcional, cobertura global complementa a tela | title, action | feedback apenas global; sem empty state local |
| `backend/Valora.Web/Views/People/Competencies.cshtml` | requer revisão específica | layout global | sem título local; sem ação local; feedback apenas global; sem empty state local |
| `backend/Valora.Web/Views/People/Culture.cshtml` | requer revisão específica | emptyState | sem título local; sem ação local; feedback apenas global |
| `backend/Valora.Web/Views/People/DevelopmentPlans.cshtml` | requer revisão específica | layout global | sem título local; sem ação local; feedback apenas global; sem empty state local |
| `backend/Valora.Web/Views/People/Engagement.cshtml` | requer revisão específica | layout global | sem título local; sem ação local; feedback apenas global; sem empty state local |
| `backend/Valora.Web/Views/People/Index.cshtml` | funcional, cobertura global complementa a tela | action, form | sem título local; feedback apenas global |
| `backend/Valora.Web/Views/People/NoOrganization.cshtml` | requer revisão específica | title | sem ação local; feedback apenas global; sem empty state local |
| `backend/Valora.Web/Views/People/Risks.cshtml` | requer revisão específica | layout global | sem título local; sem ação local; feedback apenas global; sem empty state local |
| `backend/Valora.Web/Views/People/Teams.cshtml` | funcional, cobertura global complementa a tela | action, form | sem título local; feedback apenas global |
| `backend/Valora.Web/Views/Plans/Index.cshtml` | funcional, cobertura global complementa a tela | title, action, emptyState, responsive | feedback apenas global |
| `backend/Valora.Web/Views/Processes/Index.cshtml` | funcional, cobertura global complementa a tela | title, emptyState, responsive | sem ação local; feedback apenas global |
| `backend/Valora.Web/Views/Processes/List.cshtml` | funcional, cobertura global complementa a tela | title, emptyState, responsive | sem ação local; feedback apenas global |
| `backend/Valora.Web/Views/Processes/NoOrganization.cshtml` | requer revisão específica | title | sem ação local; feedback apenas global; sem empty state local |
| `backend/Valora.Web/Views/PublicPages/About.cshtml` | requer revisão específica | title | sem ação local; feedback apenas global; sem empty state local |
| `backend/Valora.Web/Views/PublicPages/Contact.cshtml` | funcional e bem desenhada | title, action, form, feedback, responsive | — |
| `backend/Valora.Web/Views/PublicPages/Demo.cshtml` | funcional, cobertura global complementa a tela | title, action, form | feedback apenas global |
| `backend/Valora.Web/Views/PublicPages/FreeDiagnostic.cshtml` | funcional, cobertura global complementa a tela | title, action, emptyState | feedback apenas global |
| `backend/Valora.Web/Views/PublicPages/Methodology.cshtml` | requer revisão específica | title | sem ação local; feedback apenas global; sem empty state local |
| `backend/Valora.Web/Views/PublicPages/Privacy.cshtml` | requer revisão específica | title | sem ação local; feedback apenas global; sem empty state local |
| `backend/Valora.Web/Views/PublicPages/StartDiagnostic.cshtml` | funcional, cobertura global complementa a tela | title, action, form | feedback apenas global |
| `backend/Valora.Web/Views/PublicPages/Terms.cshtml` | requer revisão específica | title | sem ação local; feedback apenas global; sem empty state local |
| `backend/Valora.Web/Views/PublicPages/WhatsApp.cshtml` | funcional, cobertura global complementa a tela | title, action | feedback apenas global; sem empty state local |
| `backend/Valora.Web/Views/PublicSurvey/Respondent.cshtml` | funcional, cobertura global complementa a tela | title, action, form, emptyState | feedback apenas global |
| `backend/Valora.Web/Views/PublicSurvey/RespondentUnavailable.cshtml` | funcional, cobertura global complementa a tela | title, action | feedback apenas global; sem empty state local |
| `backend/Valora.Web/Views/PublicSurvey/Take.cshtml` | funcional, cobertura global complementa a tela | title, action, form, emptyState, responsive | feedback apenas global |
| `backend/Valora.Web/Views/Reports/Index.cshtml` | funcional e bem desenhada | title, action, form, feedback, responsive, manual | — |
| `backend/Valora.Web/Views/Responses/Index.cshtml` | funcional, cobertura global complementa a tela | title, action, feedback, responsive | sem empty state local |
| `backend/Valora.Web/Views/Results/Details.cshtml` | funcional, cobertura global complementa a tela | title, action, emptyState, responsive, manual | feedback apenas global |
| `backend/Valora.Web/Views/Results/Index.cshtml` | funcional e bem desenhada | title, action, form, filters, feedback, responsive, manual | — |
| `backend/Valora.Web/Views/Results/Public.cshtml` | funcional, cobertura global complementa a tela | title, action, responsive | feedback apenas global; sem empty state local |
| `backend/Valora.Web/Views/RiskCompliance/Index.cshtml` | funcional, cobertura global complementa a tela | title, emptyState, responsive | sem ação local; feedback apenas global |
| `backend/Valora.Web/Views/RiskCompliance/NoOrganization.cshtml` | requer revisão específica | title | sem ação local; feedback apenas global; sem empty state local |
| `backend/Valora.Web/Views/RiskCompliance/Section.cshtml` | funcional, cobertura global complementa a tela | title, emptyState | sem ação local; feedback apenas global |
| `backend/Valora.Web/Views/Saas/Dashboard.cshtml` | funcional, cobertura global complementa a tela | title, emptyState | sem ação local; feedback apenas global |
| `backend/Valora.Web/Views/SecurityCompliance/AccessReviews.cshtml` | requer revisão específica | title, responsive | sem ação local; feedback apenas global; sem empty state local |
| `backend/Valora.Web/Views/SecurityCompliance/Audit.cshtml` | requer revisão específica | title, responsive | sem ação local; feedback apenas global; sem empty state local |
| `backend/Valora.Web/Views/SecurityCompliance/DataRequests.cshtml` | requer revisão específica | title, responsive | sem ação local; feedback apenas global; sem empty state local |
| `backend/Valora.Web/Views/SecurityCompliance/Incidents.cshtml` | requer revisão específica | title, responsive | sem ação local; feedback apenas global; sem empty state local |
| `backend/Valora.Web/Views/SecurityCompliance/Index.cshtml` | requer revisão específica | title, responsive | sem ação local; feedback apenas global; sem empty state local |
| `backend/Valora.Web/Views/SecurityCompliance/Privacy.cshtml` | requer revisão específica | title, responsive | sem ação local; feedback apenas global; sem empty state local |
| `backend/Valora.Web/Views/SecurityCompliance/Retention.cshtml` | requer revisão específica | title, responsive | sem ação local; feedback apenas global; sem empty state local |
| `backend/Valora.Web/Views/SecurityCompliance/SensitiveAccess.cshtml` | requer revisão específica | title, responsive | sem ação local; feedback apenas global; sem empty state local |
| `backend/Valora.Web/Views/Settings/Index.cshtml` | funcional, cobertura global complementa a tela | title, action, feedback, responsive | sem empty state local |
| `backend/Valora.Web/Views/SolutionPacks/Builder.cshtml` | funcional e bem desenhada | title, action, form, feedback | — |
| `backend/Valora.Web/Views/SolutionPacks/Details.cshtml` | funcional, cobertura global complementa a tela | title, emptyState | sem ação local; feedback apenas global |
| `backend/Valora.Web/Views/SolutionPacks/Index.cshtml` | funcional, cobertura global complementa a tela | title, action, form | feedback apenas global |
| `backend/Valora.Web/Views/SolutionPacks/Installations.cshtml` | funcional, cobertura global complementa a tela | title, emptyState, responsive | sem ação local; feedback apenas global |
| `backend/Valora.Web/Views/SolutionPacks/NoOrganization.cshtml` | requer revisão específica | title | sem ação local; feedback apenas global; sem empty state local |
| `backend/Valora.Web/Views/SuccessCenter/Article.cshtml` | requer revisão específica | title | sem ação local; feedback apenas global; sem empty state local |
| `backend/Valora.Web/Views/SuccessCenter/CreateSupport.cshtml` | funcional e bem desenhada | title, action, form, feedback, responsive | — |
| `backend/Valora.Web/Views/SuccessCenter/Index.cshtml` | funcional, cobertura global complementa a tela | title, action, form, filters, emptyState, responsive | feedback apenas global |
| `backend/Valora.Web/Views/SuccessCenter/SupportDetails.cshtml` | funcional, cobertura global complementa a tela | title, emptyState | sem ação local; feedback apenas global |
| `backend/Valora.Web/Views/Surveys/Index.cshtml` | funcional e bem desenhada | title, action, form, filters, emptyState, feedback, responsive, manual | — |
| `backend/Valora.Web/Views/Surveys/PublicLinks.cshtml` | funcional, cobertura global complementa a tela | title, action, feedback, responsive | sem empty state local |
| `backend/Valora.Web/Views/SystemHealth/Index.cshtml` | funcional, cobertura global complementa a tela | title, emptyState, responsive | sem ação local; feedback apenas global |
| `backend/Valora.Web/Views/Users/Index.cshtml` | funcional e bem desenhada | title, action, form, filters, feedback, responsive | — |
| `backend/Valora.Web/Views/Workspace/Index.cshtml` | requer revisão específica | title, filters | sem ação local; feedback apenas global; sem empty state local |
