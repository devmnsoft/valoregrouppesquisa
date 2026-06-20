# Perfis e permissões — Valora Pulse

O Valora Pulse separa usuários da operação Valora, usuários administrativos da empresa cliente e respondentes. A matriz central fica em `ROLE_DEFINITIONS` no frontend e deve ser espelhada em claims/Firestore Rules em produção.

| Perfil | Escopo | Pode cadastrar funcionários | Pode criar questionário | Pode enviar pesquisa | Pode ver respostas | Pode ver financeiro | Pode administrar plataforma |
|---|---|---:|---:|---:|---:|---:|---:|
| `admin_valora` | Valora global | Sim, qualquer empresa | Sim, global e empresa | Sim | Sim, todas as empresas | Sim | Sim |
| `consultor_valora` | Valora leitura/apoio | Não | Não | Não | Sim, conforme apoio | Não | Não |
| `empresa_admin` | Empresa cliente | Sim, somente própria empresa | Sim | Sim | Sim, própria empresa | Não, só plano contratado | Não |
| `gestor_pesquisa` | Empresa cliente | Não | Sim | Sim | Sim, própria empresa | Não | Não |
| `analista_resultados` | Empresa cliente | Não | Não | Não | Sim, própria empresa | Não | Não |
| `gestor_area` | Área/departamento | Não | Não | Não | Sim, somente área quando segmentado | Não | Não |
| `participante` | Área pessoal | Não | Não | Não | Só próprios resultados permitidos | Não | Não |
| `convidado_externo` | Link público | Não | Não | Não | Só resultado da pesquisa, se permitido | Não | Não |

## Produção Firebase

Coleção recomendada: `users/{uid}` com `uid`, `name`, `email`, `phone`, `role`, `companyId`, `department`, `position`, `status`, `receivesEmail`, `portalAccess`, `preferences`, `createdAt`, `updatedAt`, `createdBy`, `updatedBy` e `lastLoginAt`.

Convidados externos podem ficar em `users` com `role: 'convidado_externo'` e `portalAccess: false`; se a jornada pública crescer, mover dados mínimos para `participants/{participantId}` via Cloud Functions.

## Regras de segurança

- Empresas não podem criar `admin_valora` nem `consultor_valora`.
- Usuário não pode alterar o próprio `role`, `companyId`, `status` ou permissões.
- `empresa_admin` só cria/edita usuários da própria empresa.
- Alterações de perfil devem ser registradas em auditoria/Cloud Functions.
- `gestor_area` deve possuir `department`/`areaId` para futura segmentação obrigatória.
