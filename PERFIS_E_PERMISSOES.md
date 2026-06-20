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

## Validação operacional por perfil — Firebase real

- `admin_valora`: acesso global a organizações, planos, módulos, formulários globais, pesquisas, respostas, relatórios e logs permitidos.
- `empresa_admin`: acesso somente à própria `companyId`; pode gerir funcionários, perfis empresariais, formulários, pesquisas, convites e relatórios da empresa.
- `gestor_pesquisa`: cria formulários/pesquisas e envia convites da própria empresa; não altera plano, financeiro global, backup ou perfis Valora.
- `analista_resultados`: leitura de pesquisas, respostas, dashboards e relatórios permitidos; não cria formulários, pesquisas, convites ou usuários.
- `gestor_area`: leitura da própria empresa filtrada por `department` quando preenchido em usuário/resposta/convite; limitação atual documentada até coleta obrigatória de área em todos os formulários.
- `participante` e `convidado_externo`: resposta por link seguro e resultado quando configurado, sem portal administrativo completo.
