# Módulos e planos

A matriz técnica dos módulos está em `module-definitions.js`; a matriz comercial dos planos está no catálogo de `plans`.

## Regras comerciais
- Apenas `admin_valora` gerencia planos e módulos.
- Deve existir um único plano padrão, ativo.
- Plano inativo não pode ser destacado nem selecionado por cliente novo.
- Plano vinculado a empresa não deve ser excluído.
- `enabledModules` habilita funcionalidades e menus.
- Limites comerciais devem aparecer no dashboard e bloquear criação quando excedidos.

## Módulos oficiais
Clientes, usuários, funcionários, formulários, pesquisas, convites por e-mail, respostas, relatórios, certificados, financeiro, planos, módulos, ValoraBot, LGPD, exportações, benchmark, white label, backup e logs.

## Status de empresa
`trial`, `active`, `suspended`, `overdue`, `cancelled`, `inactive`. Suspensa/inadimplente mantém leitura mínima e bloqueia novas pesquisas/envios.
