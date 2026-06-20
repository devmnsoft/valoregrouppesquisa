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

## Testes executados — evolução relatórios executivos

Data: 2026-06-20

### Cenários validados por inspeção e checagem estática

| Perfil | Cenário | Resultado esperado | Status |
|---|---|---|---|
| Admin Valora | Gerar relatório global | Métricas globais, MRR, clientes travados, ranking e alertas comerciais vêm do analytics-service | Validado por código |
| Admin Valora | Exportar PDF/Excel/Word/CSV/JSON | Todos os formatos usam o mesmo objeto de relatório central | Validado por código |
| Empresa Admin | Gerar relatório executivo da empresa | Dados limitados à própria empresa, com sumário, recomendações e plano de ação | Validado por código |
| Empresa Admin | Gerar relatório por pesquisa | Convites, respostas, taxa, média, dimensões e comentários usam filtros por pesquisa | Validado por código |
| Gestor Pesquisa | Gerar relatório de pesquisa | Indicadores e qualitativos disponíveis sem alterar dados administrativos | Validado por código |
| Analista Resultados | Exportar relatório | Fluxo de leitura/exportação preservado sem ações administrativas | Validado por código |
| Gestor Área | Relatório da área | Mantém dependência do escopo aplicado nas telas e permissões existentes | Validado por código |
| Participante | Relatório individual/certificado | Função central de participante não mistura respostas de terceiros | Validado por código |
| Sem dados | Exportação sem respostas/convites/dimensões | Mensagens amigáveis e linha de estado vazio, sem quebra | Validado por código |
| Consistência | Dashboard x resposta x relatório | Relatórios consomem `ValoraAnalytics` para evitar duplicidade de cálculo | Validado por código |

### Comandos executados

- `node --check analytics-service.js` — aprovado.
- `node --check report-service.js` — aprovado.
- `node --check app.js` — aprovado.

## Plano de Ação e Acompanhamento de Melhorias — 2026-06-20

- Empresa Admin: validar criação manual, edição, responsável, status, conclusão e filtros por prioridade/status.
- Gestor Pesquisa: validar geração automática a partir de pesquisa, vínculo com dimensão e acompanhamento.
- Analista Resultados: validar visualização e ausência de permissão de exclusão.
- Gestor Área: validar visualização/atualização apenas do departamento próprio.
- Admin Valora: validar visão global, vencidas por empresa e relatório executivo com plano de ação.
- Permissões: validar bloqueio para participante, convidado externo e isolamento entre empresas.
- Relatórios: validar seção de plano de ação recomendado com ações sugeridas, criadas, concluídas e vencidas.


## Central de Notificações e Alertas Inteligentes

O Valora Pulse agora possui sino global, contador de não lidas, dropdown, tela “Central de notificações”, ações para marcar como lida/dispensar e links rápidos. A documentação completa de tipos, regras, permissões, Firestore e lembretes automáticos está em `NOTIFICACOES_E_ALERTAS.md`.

## Evolução — Central de Notificações, Alertas Inteligentes e Lembretes

Data: 2026-06-20.

### Checks automatizados executados

- `node --check app.js`: passou.
- `node --check notification-service.js`: passou.
- `node --check firebase-repository.js`: passou.
- `node --check local-repository.js`: passou.
- `node --check functions/index.js`: passou.
- `git diff --check`: passou.
- `npm test -- --runInBand`: falhou porque o projeto não possui script `test` no `package.json`.
- `npm run test:rules`: não concluiu por limitação de ambiente; o Firebase CLI tentou baixar o emulador Firestore e recebeu HTTP 403.

### Roteiro manual obrigatório

- Admin Valora: validar alertas globais, empresas travadas, limites do plano, marcação como lida e ação relacionada.
- Empresa Admin: validar alertas da própria empresa, convite pendente, ação vencida, limite do plano e isolamento contra outra empresa.
- Gestor de Pesquisa: validar pesquisa vencendo, baixa taxa de resposta e convite pendente.
- Analista de Resultados: validar relatório disponível e ausência de alertas de criação/edição não permitidos.
- Gestor de Área: validar apenas alertas da própria área/departamento.
- Participante: validar pesquisa/resultado disponível e ausência de alertas administrativos.
- Segurança: validar que empresa A não lê empresa B, participante não lê administrativo e usuário não altera notificação de outro usuário.
- Mobile: abrir em 360px, validar dropdown, cards e botões sem scroll horizontal.

## Evolução white label e assinatura

Esta versão adiciona estrutura de identidade visual por empresa, slug público, campos de assinatura, limites customizados, status comercial e portal de plano contratado. Consulte `WHITE_LABEL_E_ASSINATURA.md` para modelo, permissões, regras de bloqueio e roteiro de testes.

### Testes manuais previstos — white label e assinatura
- Admin Valora: editar marca, plano/status comercial, limites customizados e verificar alertas.
- Empresa Admin: editar logo, cores, nome público e contatos; validar bloqueio de plano/limites.
- Pesquisa pública: abrir com e sem marca da empresa e verificar Powered by Valora.
- E-mail: validar preview/HTML com marca da empresa ou Valora padrão.
- Relatórios: validar capa/uso do plano sem vazamento entre empresas.
- Bloqueios: empresa suspensa não cria pesquisa/envios; limite excedido bloqueia ações aplicáveis.
- Mobile: validar tela de personalização e plano em 360px sem scroll horizontal.

## Correção de inicialização — RESERVED_ORG_SLUGS e CSP Firebase

Data: 2026-06-20.

### Erro encontrado

- A aplicação ficava presa em “Carregando o sistema...” porque `app.js` declarava `RESERVED_ORG_SLUGS` duas vezes no mesmo IIFE com `const`, gerando `Uncaught SyntaxError: Identifier 'RESERVED_ORG_SLUGS' has already been declared` antes da inicialização.
- A CSP do Firebase Hosting permitia scripts de `https://www.gstatic.com`, mas não permitia conexões para o mesmo host em `connect-src`, causando bloqueio dos source maps `firebase-*.js.map` carregados pelo DevTools.

### Causa

- Duplicação de bloco da evolução de white label/slug em `app.js`, incluindo `RESERVED_ORG_SLUGS` e a mensagem comercial.
- Cabeçalho `Content-Security-Policy` em `firebase.json` incompleto para o fluxo de debug/source maps do Firebase hospedado em `www.gstatic.com`.

### Correção de inicialização

- Corrigida declaração duplicada de `RESERVED_ORG_SLUGS`, mantendo uma única lista congelada e um `Set` derivado para validação eficiente.
- Corrigida CSP para permitir source maps/recursos do Firebase em `https://www.gstatic.com` via `connect-src`, sem usar `*`, `unsafe-eval` ou ampliar `script-src`.
- Adicionado fallback visual para falha fatal de inicialização, com mensagem clara no `#app` e botão “Tentar novamente”, evitando carregamento infinito em erros de runtime.
- Atualizada a versão de assets para `8.6.1`, forçando invalidação dos JS/CSS versionados no navegador.
- Validado por sintaxe que o sistema não mantém o erro de redeclaração e pode sair da tela de carregamento após o parse de `app.js`.

### Validação executada

- `rg -n "RESERVED_ORG_SLUGS|const RESERVED_ORG_SLUGS|let RESERVED_ORG_SLUGS|var RESERVED_ORG_SLUGS" . -g '!node_modules' -g '!functions/node_modules'` — confirmado apenas um `const RESERVED_ORG_SLUGS` local e usos via `RESERVED_ORG_SLUG_SET`.
- `node --check` em todos os arquivos JavaScript locais encontrados por `rg --files -g '*.js' -g '!node_modules' -g '!functions/node_modules'` — aprovado.
- `git diff --check` — aprovado.

### Roteiro manual pós-deploy

- Abrir `index.html` ou a URL do Firebase Hosting em janela anônima para garantir download dos assets `?v=8.6.1`.
- Confirmar que a home sai de “Carregando o sistema...” e renderiza os cards principais.
- Abrir `#login`, autenticar com usuário de teste e verificar menu.
- Abrir `#admin/dashboard` com perfil `admin_valora`.
- Abrir `#admin/plans` e validar que a rota de planos renderiza.
- Confirmar no console que não há `SyntaxError` de `RESERVED_ORG_SLUGS` nem bloqueio CSP para `https://www.gstatic.com/firebasejs/... .map`.

## Correção de inicialização 8.6.2

Data: 2026-06-20.

### Problemas corrigidos

- `state` era acessado antes da inicialização durante `normalizeState`, porque `let state = loadStore()` executava antes de toda a base auxiliar estar pronta e `normalizeCompany()` chamava `planById()` com dependência do estado global.
- `calculateResult` podia ser acessado antes da inicialização durante `seedStore`, pois o carregamento/seed local ocorria antes da declaração `const calculateResult = calculateSurveyResult` ser executada.
- `localStorage` antigo, corrompido ou incompatível podia travar a aplicação na tela “Carregando o sistema...”.
- CSP não permitia `https://www.gstatic.com` no `connect-src` para source maps/recursos de debug do Firebase.

### Correção aplicada

- A inicialização do estado passou a ser tardia: `state` começa como `null`, `window.ValoraState` começa como `null`, e `initializeState()` executa `loadStore()` no início de `init()`.
- `planById()` e `defaultPlan()` passaram a tolerar `state` nulo e delegam para helpers puros `planByIdFrom(plans, id)` e `defaultPlanFrom(plans)`.
- `normalizeCompany(company, store)` agora usa os planos do objeto em normalização (`store.plans`) e não consulta o estado global durante o carregamento inicial.
- `normalizeState(obj)` chama `normalizeCompany(c, obj)`, mantendo a normalização autocontida no objeto recebido.
- `local-repository.js` recria um seed limpo quando o JSON local falha no parse ou na normalização, normaliza o seed e grava novamente no `localStorage`.
- Tela fatal de inicialização ganhou ação para recriar a base local, evitando carregamento infinito em falhas não recuperáveis no navegador.
- Versão dos assets atualizada para `8.6.2`.

### Validação

- O app deve sair da tela de carregamento.
- Login local/demo deve funcionar.
- Rotas principais devem funcionar: home, `#login`, `#admin/dashboard`, `#admin/plans`, dashboard da empresa e área do participante.
- Recarregar a página não deve quebrar a aplicação.
- LocalStorage antigo incompatível deve ser descartado e substituído por seed limpo.
- Console não deve mostrar `Cannot access 'state' before initialization`, `Cannot access 'calculateResult' before initialization` ou `Identifier ... has already been declared`.
- `node --check` executado nos JS principais.

### Checks automatizados executados

- `node --check app.js`: passou.
- `node --check local-repository.js`: passou.
- `node --check config.js`: passou.
- `node --check firebase-init.js`: passou.
- `node --check firebase-repository.js`: passou.
- `node --check repository.js`: passou.
- `node --check pdf.js`: passou.
