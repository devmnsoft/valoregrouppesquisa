# Assinaturas e Cobrança — Valora Pulse

## Modelo de assinatura

A assinatura fica consolidada em `organizations.subscription` para manter compatibilidade com clientes existentes:

```js
{
  planId, planName, status, billingStatus, billingCycle,
  trialStartedAt, trialEndsAt,
  currentPeriodStart, currentPeriodEnd, nextBillingAt,
  cancelledAt, suspendedAt,
  paymentProvider, externalCustomerId, externalSubscriptionId,
  notes
}
```

Status aceitos: `trial`, `active`, `past_due`, `overdue`, `suspended`, `cancelled`, `inactive`.
Ciclos aceitos: `monthly`, `quarterly`, `semiannual`, `annual`, `manual`.

## Faturas

A coleção `invoices` registra faturas manuais ou retornadas futuramente por gateway. Campos principais: `companyId`, `organizationId`, `planId`, `subscriptionId`, `number`, `description`, `amount`, `currency`, `status`, `dueDate`, datas de pagamento/cancelamento/estorno, período de cobrança, dados do provedor, cliente e `items`.

Status aceitos: `draft`, `open`, `pending`, `paid`, `overdue`, `cancelled`, `refunded`, `failed`.

## Bloqueios comerciais

Helpers centrais no frontend:

- `getTrialStatus(company)`, `isTrialExpired(company)`, `getTrialDaysLeft(company)`.
- `canUseCommercialFeature(company, feature)`.
- `canCreateSurveyByBilling(company)`.
- `canSendInvitesByBilling(company)`.
- `canGenerateReportsByBilling(company)`.
- `canAccessPortalByBilling(company)`.

Regras: `active` usa normalmente; `trial` usa enquanto não expirar; `past_due/overdue` mantém login e leitura, mas bloqueia criação/envio; `suspended` mantém acesso mínimo a dados/faturas; `cancelled/inactive` bloqueia operação.

## Fluxo manual

Até ativar gateway real, o Admin Valora cria faturas manuais, marca como paga, cancela, adiciona link externo de pagamento e observa histórico financeiro. Empresa Admin apenas visualiza plano e faturas, abre link de pagamento e solicita segunda via/upgrade.

## Gateway futuro

Nenhuma chave deve ficar no frontend. A base de integração está em Cloud Functions:

- `createCheckoutSession`
- `createPaymentLink`
- `handlePaymentWebhook`
- `syncSubscriptionStatus`
- `cancelSubscription`
- `upgradeSubscription`
- `downgradeSubscription`

A abstração fica em `functions/payments/payment-provider.js`, com adaptadores `manual`, `stripe` e `mercadopago`. Provedores reais devem usar secrets (`firebase functions:secrets:set ...`) e validação criptográfica de webhooks.

## Webhooks

Eventos esperados: `invoice.created`, `invoice.paid`, `invoice.payment_failed`, `invoice.cancelled`, `subscription.created`, `subscription.updated`, `subscription.cancelled`, `subscription.past_due`, `customer.updated`.

Requisitos obrigatórios antes de produção: validar assinatura do provedor, idempotência por ID de evento, auditoria, atualização de fatura/assinatura e notificação financeira. O modo manual aceita simulação controlada por callable autenticada/admin.

## Permissões e Firestore Rules

- `admin_valora`: lê e altera financeiro.
- `empresa_admin`: lê faturas da própria empresa.
- Demais perfis: sem acesso financeiro.
- Webhooks atualizam via Admin SDK em Cloud Functions.

## Como testar local

1. Entrar como `admin@valoragroup.com`.
2. Acessar Admin Valora › Financeiro.
3. Criar fatura manual, marcar paga e cancelar.
4. Entrar como `gestor@empresa.com` e acessar Empresa › Plano contratado.
5. Conferir faturas, status, botão de pagamento externo e bloqueios por assinatura.

## Como testar Firebase

1. Publicar regras (`firebase deploy --only firestore:rules`).
2. Publicar functions (`firebase deploy --only functions`).
3. Criar fatura em `invoices` ou pela UI Admin.
4. Confirmar que Empresa Admin da empresa A não lê faturas da empresa B.
5. Confirmar que perfis `gestor_pesquisa`, `analista_resultados`, `participante` e `convidado_externo` não leem financeiro.
