# Testes executados

Data: 2026-06-20

## Checagens automatizadas executadas nesta entrega

- `node --check app.js` — passou.
- `node --check role-definitions.js` — passou.
- `node --check module-definitions.js` — passou.
- `node --check functions/index.js` — passou.

## Roteiro funcional obrigatório

### Admin Valora
- Criar empresa.
- Criar administrador principal `empresa_admin`.
- Criar plano.
- Ativar módulos.
- Criar usuário de cada perfil permitido.

### Empresa Admin
- Cadastrar `participante`, `gestor_pesquisa`, `analista_resultados`, `gestor_area` e `convidado_externo`.
- Tentar criar `admin_valora`; deve ser bloqueado.

### Gestor Pesquisa
- Criar questionário.
- Criar pesquisa.
- Enviar convite.
- Ver respostas.
- Confirmar que não vê financeiro global.

### Analista Resultados
- Ver respostas e relatórios.
- Confirmar bloqueio para criar questionário.
- Confirmar bloqueio para enviar pesquisa.

### Gestor Área
- Ver apenas área/departamento.
- Confirmar bloqueio para alterar funcionários.
- Confirmar bloqueio para criar pesquisa.

### Participante
- Responder pesquisa.
- Confirmar ausência de administração.

### Convidado Externo
- Responder via link.
- Confirmar ausência de portal administrativo.

### Plano e módulo
- Plano sem módulo de relatórios bloqueia relatórios.
- Plano sem envio de e-mail bloqueia convite.
- Limite de respostas/mês é respeitado pela Cloud Function.
- Limite de pesquisas ativas é respeitado no frontend/demo.
