# Validação PRD Firebase — Valora Pulse

## Data

2026-06-22

## Projeto Firebase

`gestordepesquisa`

## Domínio

A preencher durante homologação no IIS/PRD.

## Build publicado

A preencher com versão/commit do build publicado.

## Coleções encontradas

A validar com:

```bash
node scripts/validate-prd-bootstrap.js --project gestordepesquisa
```

Coleções mínimas esperadas: `users`, `settings`, `plans`, `modules`, `organizations`, `companies`, `forms`, `surveys`, `knowledgeBase`, `supportCategories`, `supportSlaPolicies`.

## Usuários Auth

- Admin Valora inicial: validar no Firebase Auth após `--apply`.
- E-mail esperado: `admin@valoragroup.com.br` ou valor informado em `--admin-email`.

## Claims

Admin Valora esperado:

```json
{ "role": "admin_valora", "companyId": "" }
```

## Planos

Esperados: Essencial, Profissional, Corporativo, Enterprise / Consultivo.

## Módulos

Esperados: `diagnosticos`, `formularios`, `pesquisas`, `convitesEmail`, `respostas`, `relatorios`, `certificados`, `planoAcao`, `valorabot`, `atendimento`, `notificacoes`, `financeiro`, `integracoes`, `logs`, `whiteLabel`.

## Formulários

Esperado: `Valora Insight™ — Diagnóstico Estratégico de Maturidade Organizacional`.

## Pesquisas

Esperada: `Valora Insight™ — Diagnóstico Essencial`, ativa, vinculada ao formulário e com `tokenHash`.

## Functions

- `getEmailStatus` callable deve ser chamada via Firebase SDK `httpsCallable`.
- Não usar `fetch` direto para `cloudfunctions.net/getEmailStatus`.

## Erros encontrados

A preencher durante homologação.

## Correções aplicadas

Criados scripts de bootstrap e validação; documentação atualizada com fluxo de execução e checklist de primeiro acesso.

## Status final

Pendente de execução real com `--apply` no projeto PRD e validação manual no domínio IIS.
