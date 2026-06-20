# White label e assinatura — Valora Pulse

## Modelo `organizations`
A empresa mantém compatibilidade com `state.companies` e, no Firebase, é gravada em `organizations/{id}`. Campos adicionados/normalizados:

- `slug`, `publicName`, `slogan`.
- `brand.logoUrl`, `brand.logoFileName`, `brand.primaryColor`, `brand.secondaryColor`, `brand.accentColor`, `brand.backgroundColor`, `brand.textColor`, `brand.useCompanyBrandOnPublicSurvey`, `brand.useCompanyBrandOnEmails`, `brand.showPoweredByValora`.
- `subscription.planId`, `subscription.status`, datas de trial/início/renovação/cancelamento, `billingStatus`, `paymentMethod`, `externalCustomerId`, `notes`.
- `limitsOverride.maxActiveSurveys`, `maxResponsesMonth`, `maxManagers`, `maxEmployees`, `maxEmailsMonth`.
- `settings.contactEmail`, `supportEmail`, `whatsapp`, `lgpdContact`, `defaultSurveyExpirationDays`, `allowPublicResults`, `allowParticipantCertificates`.

## Permissões
- Empresa Admin edita identidade pública, slogan, URL de logo, cores, contato, WhatsApp, LGPD e preferências de marca.
- Empresa Admin não edita assinatura, plano, status comercial, cobrança ou limites.
- Admin Valora controla plano, status comercial e campos sensíveis em `organizations`.
- Firestore Rules restringem update de empresa admin a `publicName`, `slogan`, `brand`, `settings`, `updatedAt` e `updatedBy`.

## Slug público
- Use `index.html?org=slug` ou `index.html?survey=ID&token=TOKEN&org=slug`.
- `normalizeSlug`, `validateOrganizationSlug` e `findOrganizationBySlug` validam formato localmente.
- Reservados: `admin`, `login`, `valora`, `api`, `firebase`, `suporte`, `app`.
- Em produção, recomenda-se coleção auxiliar `organizationSlugs/{slug}` para unicidade transacional.

## Bloqueios comerciais
Helpers implementados:

- `getSubscriptionStatus(company)`.
- `canCreateSurveyBySubscription(company)`.
- `canSendInvitesBySubscription(company)`.
- `canUseReportsBySubscription(company)`.
- `canUseWhiteLabel(company)`.
- `getEffectiveCompanyLimits(company, plan)`.
- `getCompanyUsage(companyId)`.
- `getLimitStatus(companyId)`.

Status `suspended`, `cancelled` e `inactive` bloqueiam ações operacionais conforme o helper chamado. Limites em 80% geram alerta; limites em 100% bloqueiam ações aplicáveis.

## Como testar
1. Entrar como Admin Valora e editar um cliente.
2. Alterar status para `trial`, `active`, `overdue` e `suspended`.
3. Entrar como empresa admin e abrir **Dados da empresa**.
4. Configurar logo por URL, cores e slug.
5. Abrir uma pesquisa com `?org=slug` e confirmar marca, cores e “Powered by Valora”.
6. Abrir **Plano contratado** e conferir status, uso, limites, módulos liberados/bloqueados.
7. Suspender a empresa e tentar criar pesquisa/enviar convites.

## Riscos restantes
- Upload real de logo via Firebase Storage ainda não foi ativado; usa URL validada.
- Cobrança real e webhooks de assinatura ainda não existem.
- Unicidade forte de slug deve ser transacional via backend/Cloud Function antes de escalar produção multi-tenant.
