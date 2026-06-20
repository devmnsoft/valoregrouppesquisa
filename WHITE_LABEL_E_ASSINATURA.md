# White label, personalização e assinatura — Valora Pulse

## Modelo de organização

A coleção `organizations` mantém compatibilidade com `state.companies` no modo local/demo. Campos adicionados:

- `legalName`, `publicName`, `slogan`, `slug`.
- `brand.logoUrl`, `brand.logoFileName`, `brand.primaryColor`, `brand.secondaryColor`, `brand.accentColor`, `brand.backgroundColor`, `brand.textColor`, `brand.useCompanyBrandOnPublicSurvey`, `brand.useCompanyBrandOnEmails`, `brand.showPoweredByValora`.
- `subscription.planId`, `subscription.status`, `subscription.trialStartedAt`, `subscription.trialEndsAt`, `subscription.startedAt`, `subscription.renewedAt`, `subscription.cancelledAt`, `subscription.billingStatus`, `subscription.paymentMethod`, `subscription.externalCustomerId`, `subscription.notes`.
- `limitsOverride.maxActiveSurveys`, `limitsOverride.maxResponsesMonth`, `limitsOverride.maxManagers`, `limitsOverride.maxEmployees`, `limitsOverride.maxEmailsMonth`.
- `settings.contactEmail`, `settings.supportEmail`, `settings.whatsapp`, `settings.lgpdContact`, `settings.defaultSurveyExpirationDays`, `settings.allowPublicResults`, `settings.allowParticipantCertificates`.

## Permissões

Empresa Admin pode editar somente nome público, slogan, slug, URL da logo, cores, e-mail/WhatsApp/contato LGPD e preferências de marca. Plano, assinatura, limites e cobrança permanecem protegidos.

Admin Valora continua responsável por dados cadastrais, plano, limites, status comercial, trial, cobrança e observações comerciais.

## Slug público

O slug aceita letras minúsculas, números e hífen. Slugs reservados: `admin`, `login`, `valora`, `api`, `firebase`, `suporte`, `app`.

URLs suportadas:

- `index.html?org=slug`
- `index.html?survey=ID&token=TOKEN&org=slug`

Para unicidade forte em produção, use a coleção auxiliar `organizationSlugs/{slug}` criada por processo seguro ou Cloud Function transacional.

## Bloqueios comerciais

Status considerados:

- `trial`: uso permitido até o fim do trial, com aviso de vencimento.
- `active`: uso normal.
- `overdue`: leitura permitida; novas ações podem ser restringidas.
- `suspended`: bloqueia novas pesquisas e convites.
- `cancelled`: bloqueia uso operacional.
- `inactive`: não permite acesso operacional.

Helpers implementados em `app.js`:

- `getSubscriptionStatus(company)`
- `canCreateSurveyBySubscription(company)`
- `canSendInvitesBySubscription(company)`
- `canUseReportsBySubscription(company)`
- `canUseWhiteLabel(company)`
- `getEffectiveCompanyLimits(company, plan)`
- `getCompanyUsage(companyId)`
- `getLimitStatus(companyId)`

## Firebase Repository

Métodos adicionados nos repositórios local e Firebase:

- `updateOrganizationBrand(companyId, brand)`
- `updateOrganizationSubscription(companyId, subscription)`
- `updateOrganizationSettings(companyId, settings)`
- `updateOrganizationLimits(companyId, limits)`
- `getOrganizationBySlug(slug)`
- `checkSlugAvailability(slug)`

## Como testar

1. Entrar como Admin Valora e editar um cliente.
2. Entrar como Empresa Admin e abrir **Dados e marca**.
3. Informar `publicName`, `slug`, `logoUrl` e cores.
4. Salvar e abrir um link de pesquisa com `&org=slug`.
5. Validar que e-mail/relatório/portal usam a identidade apenas da empresa atual.
6. Alterar assinatura para `suspended` no dado da organização e confirmar bloqueio de criação de pesquisa e envio de convites.
