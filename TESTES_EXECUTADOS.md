# Testes executados

Data: 2026-06-20

## Checagens automatizadas executadas nesta entrega

- `node --check firebase-repository.js` — passou.
- `node --check app.js` — passou.
- `node --check role-definitions.js` — passou.
- `node --check module-definitions.js` — passou.
- `node --check functions/index.js` — passou.

## Roteiro funcional obrigatório — modo local

- Abrir o sistema com `STORAGE_MODE: 'local'`.
- Login demo com `admin@valoragroup.com` / `Valora@2026`.
- Confirmar clientes em `state.companies`.
- Confirmar usuários em `state.users`.
- Confirmar planos em `state.plans`.
- Confirmar pesquisas em `state.surveys`.
- Confirmar respostas em `state.responses`.
- Salvar alteração e recarregar para confirmar persistência no `localStorage`.

## Roteiro funcional obrigatório — modo Firebase

- Configurar `STORAGE_MODE: 'firebase'`, `FIREBASE_ENABLED: true` e `FIREBASE_CONFIG` pública.
- Publicar `firestore.rules`.
- Criar seed mínimo com base em `firestore.seed.sample.json`.
- Login Firebase com `admin_valora`.
- Confirmar carregamento de `organizations` em `state.companies`.
- Confirmar carregamento de planos, módulos, usuários, formulários, pesquisas, respostas, convites, faturas, settings e logs somente leitura.
- Criar organização pelo portal admin e confirmar documento em `organizations/{id}`.
- Criar usuário `empresa_admin` com Auth/claim/perfil Firestore coerentes.
- Login como `empresa_admin` e confirmar que apenas a própria empresa aparece.
- Login como `gestor_pesquisa` e confirmar formulários/pesquisas/respostas da própria empresa.
- Login como `participante` e confirmar somente pesquisas/respostas próprias.
- Login com usuário sem `users/{uid}` e confirmar bloqueio amigável.

## Roteiro funcional obrigatório — segurança

- Empresa A não lê `organizations`, `users`, `surveys`, `responses`, `invitations` ou `invoices` da Empresa B.
- Participante não lê resposta de outro participante.
- Empresa não edita `plans` nem `modules`.
- `analista_resultados` não cria pesquisa nem formulário.
- `logs` não aceitam escrita direta pelo frontend.
- Respostas públicas são criadas somente via Cloud Function.

## Observações desta entrega

Os testes automatizados acima validam sintaxe JavaScript. A validação Firebase completa exige projeto Firebase real ou emuladores com usuários, claims e dados seed.
