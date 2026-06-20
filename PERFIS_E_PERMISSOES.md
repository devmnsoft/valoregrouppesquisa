# Perfis e permissões — Valora Pulse

A fonte técnica central é `role-definitions.js`. Todas as telas e ações devem consultar `can(user, permission)` antes de executar operações sensíveis.

| Perfil | Escopo | Cadastra funcionários | Cria questionário | Envia pesquisa | Vê respostas | Vê financeiro | Administra plataforma |
|---|---|---:|---:|---:|---:|---:|---:|
| admin_valora | Valora/global | Sim | Sim | Sim | Sim | Sim | Sim |
| consultor_valora | Valora/leitura operacional | Não | Não | Não | Sim | Não | Não |
| empresa_admin | Própria empresa | Sim | Sim | Sim | Sim | Plano contratado | Não |
| gestor_pesquisa | Própria empresa | Não | Sim | Sim | Sim | Não | Não |
| analista_resultados | Própria empresa | Não | Não | Não | Sim | Não | Não |
| gestor_area | Departamento/área | Não | Não | Não | Apenas área | Não | Não |
| participante | Área pessoal | Não | Não | Não | Próprias respostas quando liberado | Não | Não |
| convidado_externo | Link seguro | Não | Não | Não | Não | Não | Não |

## Regras de criação de perfis

- `admin_valora` pode criar qualquer perfil.
- `empresa_admin` pode criar apenas: `empresa_admin`, `gestor_pesquisa`, `analista_resultados`, `gestor_area`, `participante` e `convidado_externo`.
- Empresas nunca podem criar `admin_valora` ou `consultor_valora`.
- `gestor_area` exige departamento.
- `convidado_externo` pode existir sem acesso ao portal e responder por link seguro.

## Mensagem padrão de bloqueio

Quando uma ação não é permitida, o produto deve mostrar: “Seu perfil não possui permissão para executar esta ação.”
