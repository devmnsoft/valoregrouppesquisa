# Auditoria de handlers de ações e formulários

Esta auditoria cobre `app.js`, `createActions`, `createFormHandlers`, `registerGlobalHandlers`, `handleDocumentClick`, `handleDocumentSubmit`, `bootstrapApp`, atributos `data-action` e `data-form` renderizados no HTML do app.

## Fluxo global

| Área | Implementação | Status |
|---|---|---|
| `bootstrapApp` | Cria `actions`, cria `formHandlers`, registra handlers globais e inicia `init` dentro de bloco protegido. | Corrigida |
| `registerGlobalHandlers` | Registra `handleDocumentClick`, `handleDocumentChange`, `handleDocumentInput`, `handleDocumentSubmit` e `handleDocumentKeydown`. | OK |
| `handleDocumentClick` | Resolve `data-action` em `actions[actionName]` e chama `missingAction` quando não existe. | OK |
| `handleDocumentSubmit` | Resolve `data-form` em `formHandlers[formName]` e chama `missingFormHandler` quando não existe. | OK |
| Ruído externo | `isExternalBrowserNoise` ignora rejeição/extensão sem stack do bundle do app. | Corrigida |

## Ações registradas em `createActions`

A lista completa é validada automaticamente por `npm run actions:handlers`, que cruza todos os `data-action` estáticos com as chaves registradas e verifica referências de função. Status geral: **OK**.

| Grupo | Ações principais | Função chamada | Uso no HTML | Status |
|---|---|---|---|---|
| Navegação pública | `goHome`, `goHomeFromPublicSurvey`, `scrollTo`, `selectPlan`, `openManual`, `publicHelpClick`, `publicWhatsappClick` | Funções homônimas ou handlers inline | Home, header, ajuda pública, botões públicos | OK |
| Autenticação | `forgotPassword`, `logout`, `goMyArea` | Handler inline / `logoutUser` / `navigate` | Login e topbar autenticada | OK |
| Portal e UI | `portalTab`, `pageHelp`, `toggleMenu`, `toggleAdminMobileMenu`, `closeAdminMobileMenu`, `toggleBot` | Handlers inline e funções declaradas | Menus, abas, ValoraBot | OK |
| Notificações | `toggleNotifications`, `openNotificationsCenter`, `markNotificationRead`, `dismissNotification`, `markAllNotificationsRead`, `actNotification` | Funções declaradas | Sino/central de notificações | OK |
| Formulários | `newForm`, `editForm`, `cloneForm`, `deleteForm`, `shareForm`, `previewForm`, `addDimension`, `removeDimension`, `addQuestion`, `removeQuestion`, `moveQuestion`, `duplicateQuestion`, `addOption`, `removeOption`, `addBand`, `removeBand`, `reviewBuilder`, `previewBuilder`, `saveBuilder` | Funções declaradas ou handlers inline | Admin › formulários e editor dinâmico | `addDimension` e funções de builder corrigidas |
| Pesquisas | `newSurvey`, `editSurvey`, `deleteSurvey`, `featureSurvey`, `shareSurvey`, `quickSurvey`, `renewSurvey`, `openSurvey`, `closeLinkedSurvey`, `archiveLinkedSurvey`, `closeSurveysByForm`, `archiveFormAfterClose`, `createLinkedFormVersion` | Funções declaradas/Repository | Admin › pesquisas e modal de formulário vinculado | OK |
| Resultados/certificados | `viewResponse`, `emailResult`, `certificatePdf`, `certificatePng`, `reportResponsePdf`, `deleteResponse`, `anonymizeResponse`, `resendResultEmail` | Funções declaradas | Resultado público/admin, comunicações | OK |
| Operação/e-mail | `processEmailQueue`, `resendPendingResults`, `promptResendResultEmail`, `viewEmailJobs`, `resolveCommunication` | Handlers inline seguros ou funções declaradas | Operação assistida e comunicações | Corrigida |
| Cadastros admin | `newUser`, `editUser`, `deleteUser`, `newCompany`, `editCompany`, `deleteCompany`, `newPlan`, `editPlan`, `newInvoice`, `editInvoice`, `editModule` | Funções declaradas/handlers inline | Admin › clientes, usuários, planos, faturas | OK |
| Integrações/exportações | `openApiKeyModal`, `revokeApiKey`, `openWebhookModal`, `testWebhook`, `openEmployeeImport`, `openDataExport`, `copyApiSecret`, `exportReport`, `exportBackup`, `openImport` | Funções declaradas | Integrações e operações | OK |
| LGPD/conta | `exportMyData`, `requestDeletion` | Funções declaradas | Área do participante/empresa | OK |

## Formulários registrados em `createFormHandlers`

| `data-form` | Função chamada | Onde é usado | Status |
|---|---|---|---|
| `login` | `login` | Tela de login | OK |
| `signup` | `signup` | Cadastro público | OK |
| `bot` | `sendBot` | ValoraBot | OK |
| `supportMessage` | `sendSupportMessageForm` | Suporte | OK |
| `supportRating` | `saveSupportRating` | Avaliação de suporte | OK |
| `certificateValidation` | `validateCertificatePublic` | Validação pública de certificado | OK |
| `resendResultEmail` | `resendResultEmail` | Modal de e-mail de resultado | OK |
| `apiKey`, `webhook`, `employeeImport`, `dataExport` | `saveApiKey`, `saveWebhook`, `saveEmployeeImport`, `exportData` | Integrações | OK |
| `user`, `company`, `invoice`, `plan`, `module`, `survey`, `quickSurvey` | Funções `save*` declaradas | Admin | OK |
| `takeSurvey`, `inviteEmail`, `bulkInvite`, `resultEmail` | Funções declaradas | Jornada de pesquisa/convites | OK |
| `settings`, `companySettings`, `onboardingWizard`, `importBackup` | Funções declaradas | Configurações e importação | OK |

## Validação automatizada

- `scripts/validate-action-handlers.js` falha se `createActions`/`createFormHandlers` referenciarem função inexistente ou se houver `data-action`/`data-form` sem chave registrada.
- `scripts/validate-public-boot-no-reference-error.js` valida sintaxe com `new Function(app)`, boot, factories e handlers globais.
- `scripts/validate-dist-action-handlers.js` repete a validação no bundle final em `dist/app*.js` após o build.
