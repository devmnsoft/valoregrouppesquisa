# Contratos de dados Valora

A aplicação normaliza todo estado externo com `data-normalization.js` antes de renderizar ou persistir.

## Tipos esperados

- `settings`: objeto.
- `settings.faq`: array de `{ id, question, answer }`.
- `plans`, `modules`, `companies`, `organizations`, `users`, `forms`, `surveys`, `responses`, `knowledgeBase`, `supportCategories`, `supportSlaPolicies`: arrays.
- `plan.features`, `form.questions`, `survey.questions`: arrays após normalização.
- `response.participant`: objeto com `name` e `email` seguros.

## Regras

- Nunca chamar `.map()` diretamente em `state.settings.faq`.
- Use `normalizeFaqItems()`, `asArray()` ou `normalizeAppState()` antes de renderizar dados externos.
- Use `responseParticipantLabel()`, `responseParticipantEmail()` ou `buildCertificateViewModel()` para respostas/certificados.

## Validação

```bash
node scripts/validate-data-contracts.js
node scripts/validate-render-resilience.js
```
