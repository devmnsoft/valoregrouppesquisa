# Módulos e planos — Valora Pulse

A fonte técnica central é `module-definitions.js`. Cada módulo declara `id`, `label`, `description`, `scope`, `requiredPermission`, `defaultEnabled`, `commercialFeature` e `route`.

## Módulos oficiais

`clientes`, `financeiro`, `planos`, `modulos`, `usuarios`, `funcionarios`, `formularios`, `pesquisas`, `convitesEmail`, `respostas`, `relatorios`, `certificados`, `valorabot`, `lgpd`, `exportacoes`, `benchmark`, `whiteLabel`, `backup` e `logs`.

## Impacto nos menus e ações

- O menu usa permissões do perfil e disponibilidade do módulo.
- Botões e ações críticas também validam permissão antes de executar.
- Módulos globais (`financeiro`, `planos`, `backup`, `logs`) ficam restritos à Valora conforme permissão.
- Módulos comerciais de empresa dependem do plano contratado.

## Estrutura mínima do plano

```js
{
  id, name, description, price, status, default, highlight,
  maxActiveSurveys, maxResponsesMonth, maxManagers,
  maxEmployees, maxEmailsMonth, features, enabledModules,
  createdAt, updatedAt
}
```

## Limites aplicados

- `maxActiveSurveys`: bloqueia criação de novas pesquisas ativas.
- `maxResponsesMonth`: validado no backend no envio público de respostas.
- `maxManagers`: bloqueia criação acima do limite de gestores.
- `maxEmployees`: acompanha consumo de funcionários/respondentes.
- `maxEmailsMonth`: bloqueia envio em lote quando atingido.

## Regras comerciais

- Apenas `admin_valora` gerencia planos.
- Plano inativo não pode ser padrão ou destaque.
- Plano inativo não deve aparecer para novos clientes.
- Plano vinculado a clientes não pode ser excluído.
- Módulos liberados no plano controlam menu, botões e ações.

## Seed comercial mínimo

`firestore.seed.sample.json` define os módulos base `clientes`, `usuarios`, `funcionarios`, `formularios`, `pesquisas`, `convitesEmail`, `respostas`, `relatorios`, `certificados`, `financeiro`, `planos`, `modulos`, `valorabot`, `lgpd`, `exportacoes`, `backup` e `logs`, além dos planos Gratuito, Essencial, Growth e Enterprise com limites de pesquisas, respostas, gestores, funcionários e e-mails mensais.

## actionPlans — Plano de ação

Módulo comercial para gestão de ações de melhoria. Pode ser habilitado por plano, aparece no menu administrativo para perfis autorizados e pode receber limites comerciais por quantidade de ações no plano gratuito ou em contratos específicos.


## Central de Notificações e Alertas Inteligentes

O Valora Pulse agora possui sino global, contador de não lidas, dropdown, tela “Central de notificações”, ações para marcar como lida/dispensar e links rápidos. A documentação completa de tipos, regras, permissões, Firestore e lembretes automáticos está em `NOTIFICACOES_E_ALERTAS.md`.
