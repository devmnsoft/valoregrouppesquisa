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

## Homologação Firebase real — 2026-06-20

| Grupo | Cenário | Resultado esperado | Evidência/Status |
| --- | --- | --- | --- |
| Firebase | Login admin, criação empresa, empresa admin, funcionário, formulário, pesquisa, convite, link, resposta, cálculo, respostas, dashboard e relatório | Fluxo ponta a ponta com `STORAGE_MODE='firebase'` e Functions publicadas | A executar em projeto Firebase real após import do seed |
| Segurança | Empresa A/B isoladas; participante sem admin; analista sem criação; gestor sem financeiro; convidado sem portal; usuário sem alterar próprio `role`/`companyId` | Bloqueio por Rules/Functions | Coberto por `firestore.rules` e testes de emulador; repetir em produção |
| Plano | Limites de pesquisas, respostas, e-mails, módulo desabilitado, plano inativo e empresa suspensa | Bloqueio antes de gravar/enviar | `sendSurveyInvitations` valida limite de e-mails; resposta pública valida limite de respostas; demais limites exigem teste manual UI |
| Convites | Inativo, e-mail vazio/inválido, `receivesEmail=false`, empresa divergente e status | Convite `failed` ou bloqueio seguro | Implementado em Cloud Function `sendSurveyInvitations` |
| Área | `gestor_area` com `department` | Respostas filtradas por departamento | Estrutura implementada; depende de preenchimento de `department` na resposta/convite |

## Testes de onboarding guiado — 2026-06-20

| Área | Cenário | Resultado esperado | Status |
|---|---|---|---|
| Empresa nova | Login como `empresa_admin` e abrir dashboard | Checklist **Primeiros passos da implantação**, percentual e próximo passo aparecem | Validado por inspeção de código |
| Funcionários | Ambiente sem respondentes | Estado vazio orienta cadastrar funcionário | Validado por inspeção de código |
| Wizard | Configurar minha primeira pesquisa | Fluxo cria/atualiza dados, funcionário rápido, questionário essencial quando necessário e pesquisa ativa | Validado por inspeção de código |
| Questionário | Criar primeiro questionário | Checklist passa a considerar formulário concluído e sugere pesquisa | Validado por cálculo dinâmico |
| Pesquisa | Criar pesquisa | Checklist sugere envio de convites | Validado por cálculo dinâmico |
| Convites | Enviar convites | Checklist sugere acompanhar respostas | Validado por cálculo dinâmico |
| Respostas | Primeira resposta recebida | Dashboard mostra média, dimensões, taxa e libera relatório | Validado por cálculo dinâmico |
| Permissões | Analista de resultados | Não recebe wizard de criação habilitado e vê alerta de perfil | Validado por regras de perfil existentes + UI |
| Admin Valora | Dashboard global | Mostra status de implantação do cliente e empresas travadas/novas | Validado por inspeção de código |
| Responsividade | 360px | Checklist e wizard empilham botões/etapas, sem scroll horizontal esperado | Validado por CSS responsivo |

### Comandos executados nesta evolução

- `node --check app.js` — validação sintática do JavaScript principal.
- `npm test -- --runInBand` — não há script `test` genérico neste repositório; usar `npm run test:rules` e `npm run test:functions` com emuladores Firebase quando disponíveis.
## Testes executados — evolução dashboards executivos

Data: 2026-06-20

### Verificações realizadas

- Admin Valora: validação estática do dashboard global com múltiplas métricas comerciais, clientes por plano, MRR estimado, implantação, travados e alertas comerciais.
- Empresa: validação estática de estados vazios, respostas concluídas, taxa de resposta, uso do plano, dimensões, evolução e recomendações.
- Gestor de Pesquisa: ações rápidas preservadas para perfis com permissão (`criar pesquisa`, `enviar convites`, `ver respostas`).
- Analista de Resultados: dashboard mostra leitura analítica sem botões administrativos de criação/envio.
- Mobile: CSS adicionado para gráficos, rankings e alertas não estourarem em 360px.
- Consistência: dashboards, respostas e relatórios passam a consumir `ValoraAnalytics` para respostas concluídas, convites enviados, taxa, médias e dimensões.

### Comandos

- `node --check analytics-service.js` — aprovado.
- `node --check app.js` — aprovado.

### Observações

Não foram executados testes Firebase com emuladores nesta entrega por serem suíte de segurança dependente de ambiente externo/serviços do Firebase. O modo local/demo permanece sem novas dependências pesadas.
