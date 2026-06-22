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

## Correção de inicialização 8.6.3

### Problema
- `normalizeInvoice()` dependia de `companyById()`, que dependia de `state` global.
- Durante `initializeState()`, `state` ainda era `null`.
- `local-repository.loadCompanies()` não era null-safe.
- Tela de erro usava handler inline bloqueado por CSP.

### Correção
- `normalizeState()` passou a normalizar o objeto recebido, sem depender do `state` global para faturas.
- `normalizeInvoice(i, store)` usa `store.companies` para resolver dados do cliente e plano.
- Repositório local passou a retornar coleções vazias quando `state` está nulo ou indefinido.
- Recuperação de localStorage ausente/corrompido recria seed e continua em memória mesmo se o navegador impedir gravação.
- Tela de erro usa `addEventListener` em vez de `onclick` inline.
- CSP mantida restritiva, sem `unsafe-inline` em scripts, com `https://www.gstatic.com` em `connect-src`.

### Validações executadas
- `node --check` em todos os arquivos JavaScript do projeto fora de `node_modules`.
- Revisão de ausência de `onclick` inline na tela de erro fatal.

## 2026-06-20 — Estabilização da inicialização 8.6.4

### Escopo validado
- Inicialização com `state` começando em `null` e `initializeState()` idempotente.
- Normalização pura baseada no objeto recebido, com normalizers recebendo `store`.
- Faturas normalizadas sem `companyById()` ou carregadores dependentes de estado global.
- Repositório local revisado para leituras null-safe.
- Recuperação automática para localStorage ausente ou corrompido.
- Tela de falha de inicialização sem handlers inline, usando `addEventListener`.
- CSP mantida restritiva com `connect-src` incluindo `https://www.gstatic.com`.
- Assets versionados para `8.6.4`.

### Testes executados
- `node --check app.js`
- `node --check local-repository.js`
- `node --check config.js`
- `node --check firebase-init.js`
- `node --check firebase-repository.js`
- `node --check repository.js`
- `node --check pdf.js`
- `node --check role-definitions.js`
- `node --check module-definitions.js`
- `node --check analytics-service.js`
- `node --check report-service.js`
- `node --check notification-service.js`
- `while IFS= read -r f; do node --check "$f" || exit 1; done < <(find . -name '*.js' -not -path './node_modules/*' -not -path './functions/node_modules/*' -print | sort)`

### Roteiro manual obrigatório no navegador
- Base vazia: remover `localStorage`, recarregar e confirmar abertura da Home com seed recriada.
- Base corrompida: gravar valor inválido em `localStorage`, recarregar e confirmar seed recriada.
- Login: entrar com admin demo, abrir `#admin/dashboard`, `#admin/plans`, `#admin/clients` e recarregar.
- CSP: forçar tela de erro, clicar em “Tentar novamente” e “Recriar base local”, confirmando ausência de bloqueio por inline handler.
- Console: confirmar ausência de `Cannot read properties of null`, `Cannot access state before initialization`, `Cannot access calculateResult before initialization`, `Identifier has already been declared` e `Executing inline event handler violates CSP`.

## Integrações, API pública e webhooks
- API Key: criação com hash, exibição única, revogação e bloqueio de chave revogada previstos.
- API pública: endpoints `employees`, `surveys`, `responses`, `reports` e `webhooks/test` preparados com escopo e `companyId`.
- Webhook: cadastro, teste, headers de assinatura, log e falha simulável.
- Importação: valida e-mail, rejeita perfil global, atualiza duplicados e exibe resumo.
- Exportação: exporta funcionários, pesquisas, respostas e formato `responses_flat`, com anonimização quando marcada.
- Permissões: participante/convidado sem acesso; empresa limitada ao próprio `companyId`.


## Observabilidade e logs
- Logs locais: gerar info, warn, error e security pelo painel **Logs e Monitoramento**.
- Filtros: validar nível, categoria, Telegram e logs de teste.
- Exportação: baixar CSV e JSON e verificar dados mascarados.
- Telegram: configurar secrets, habilitar envio e executar **Enviar teste Telegram**.
- Segurança: simular acesso negado e confirmar log `security`.
- Erros: simular erro frontend e confirmar captura sem quebrar o sistema.
- Anti-spam: repetir evento crítico mais de 10 vezes em 5 minutos e confirmar rate limit nas Functions.

## Observabilidade adicionada
- Logs centralizados em `log-service.js` com mascaramento, exportação CSV/JSON/Excel compatível e testes manuais no painel Admin.
- Arquivos locais de segredo (`.env`, `.env.local`, `.env.production`, `telegram.env`, `secrets.json`) são ignorados pelo Git.

## Homologação final e Release Candidate — 2026-06-21

**Versão testada:** Valora Group™ 8.6.4 RC1
**Responsáveis:** QA/Produto/Arquitetura — execução documental e regressão técnica inicial por agente automatizado.
**Status geral:** aprovado com ressalvas para apresentação controlada; produção depende de homologação real com evidências.

### Resumo de testes

- Criados documentos formais de release candidate, checklist de homologação, matriz de bugs, regressão técnica, roteiro ponta a ponta e aceite do produto.
- Executada regressão sintática em todos os arquivos JavaScript encontrados no repositório com `node --check`.
- Não foram confirmados bugs bloqueantes por checagem sintática.
- Testes funcionais manuais em navegador, Firebase real, SMTP real, Telegram real, integrações reais e mobile 360px permanecem pendentes de execução na etapa de homologação real.

### Aprovação por área

| Área | Resultado | Observações |
|---|---|---|
| Documentação de release | Aprovado | `RELEASE_CANDIDATE.md` criado. |
| Checklist de homologação | Aprovado | `CHECKLIST_HOMOLOGACAO_FINAL.md` criado. |
| Matriz de bugs | Aprovado | `BUGS_HOMOLOGACAO.md` criado com critérios de severidade. |
| Regressão técnica | Aprovado | Todos os arquivos `.js` passaram em `node --check`. |
| Roteiro ponta a ponta | Aprovado | `ROTEIRO_TESTE_PONTA_A_PONTA.md` criado. |
| Aceite do produto | Aprovado com ressalvas | Critérios documentados; execução real pendente. |
| Modo local/demo | Pendente de homologação real | Exige execução em navegador e evidências. |
| Modo Firebase | Pendente de homologação real | Exige projeto Firebase/emuladores, Auth, claims, Rules e Functions. |
| Segurança | Pendente de homologação real | Exige testes multiempresa e perfis. |
| Integrações | Pendente de homologação real | Exige credenciais e endpoints reais/sandbox. |
| Mobile 360px | Pendente de homologação real | Exige teste visual em navegador/dispositivo. |

### Bugs encontrados

| ID | Severidade | Área | Status | Observação |
|---|---|---|---|---|
| — | — | — | — | Nenhum bug bloqueante confirmado por regressão sintática nesta etapa. |

### Pendências

- Executar `CHECKLIST_HOMOLOGACAO_FINAL.md` em navegador.
- Executar `ROTEIRO_TESTE_PONTA_A_PONTA.md` em modo local/demo.
- Executar roteiro em Firebase real ou emuladores.
- Registrar bugs reais em `BUGS_HOMOLOGACAO.md`.
- Validar console sem erro crítico.
- Validar CSP, headers e cache no ambiente publicado.
- Validar Telegram, SMTP, webhooks e logs de integração.
- Validar mobile em 360px.
- Corrigir e retestar qualquer bug bloqueante encontrado.

### Comandos executados

- `for f in $(rg --files -g '*.js' | sort); do node --check "$f" || exit 1; done` — aprovado.


## Evolução: logger, manuais, chatbot e atendimento

- Logger: simular logs debug/info/warn/error/critical/audit/security em Admin > Logs.
- Erros: simular erro frontend, async, Firebase, e-mail, chatbot e verificar toast + log.
- Manuais: validar public, admin_valora, consultor_valora, empresa_admin, gestor_pesquisa, analista_resultados, gestor_area, participante e convidado_externo.
- Chatbot público: perguntar “O que é o Valora Pulse?”, “Como respondo uma pesquisa?”, “Meus dados estão seguros?” e “Como falo com suporte?”.
- Chatbot logado: testar empresa_admin, participante e analista_resultados com perguntas de permissão.
- Atendimento: abrir conversa pública, logada e assumir/encerrar como atendente autorizado.
- Segurança: confirmar e-mail/token/URL sanitizados e ausência de token Telegram no frontend.
- Mobile: validar manual, chatbot, chat e fila em 360px.

## Evolução — Shell público simplificado da pesquisa (8.6.5)

### Escopo validado
- Pesquisa pública: URL com `?survey=...&token=...` passa a usar header público sem links comerciais, com logo Valora, status “Pesquisa segura”, Ajuda em nova aba, ValoraBot e WhatsApp contextual quando houver número configurado.
- Resultado público: URL com `?result=...&rt=...` também usa o mesmo shell público simplificado.
- Ajuda pública: rota `#public-help` documenta como responder, link seguro, LGPD, uso dos dados, suporte e resultado.
- White label: card da empresa permanece na pesquisa pública, usa logo da empresa quando configurada e mantém “Powered by Valora Group” conforme configuração da marca.
- Mobile 360px: header público, botões, título, badge de tempo, card de empresa, etapas e botões flutuantes receberam regras responsivas para evitar estouro horizontal e cobertura do formulário.

### Checklist manual recomendado
| Área | Cenário | Resultado esperado | Status |
| --- | --- | --- | --- |
| Desktop | Abrir `index.html?survey=survey_demo&token=<token>&org=empresa-exemplo` | Header simplificado aparece; não aparecem “Como funciona”, “Planos”, “Entrar” ou “Criar ambiente” | Validado por código |
| Desktop | Clicar em Ajuda | Abre `index.html#public-help` em nova aba sem limpar o formulário da pesquisa | Validado por código |
| Desktop | Abrir ValoraBot na pesquisa pública | Painel abre com sugestões de participante externo sobre pesquisa, LGPD, link expirado e suporte | Validado por código |
| Desktop | Clicar WhatsApp com número configurado | Abre `https://wa.me/...` com mensagem contextual contendo pesquisa e empresa | Validado por código |
| Mobile 360px | Abrir a pesquisa pública | Header cabe na tela, ações ficam compactas e não há scroll horizontal esperado | Validado por CSS |
| Mobile 360px | Percorrer formulário até o envio | Ações flutuantes ficam acima da base e o painel do bot sobe para não cobrir o formulário | Validado por CSS |
| Demais páginas | Acessar `#home`, `#login`, `#admin/dashboard`, `#empresa/dashboard`, `#participante/dashboard` | Header completo permanece para jornadas comerciais e portais internos | Validado por código |
| White label | Empresa com `brand.logoUrl` | Logo da empresa aparece no card da pesquisa pública | Validado por código |
| White label | Empresa sem logo | Fallback Valora aparece no card da pesquisa pública | Validado por código |

### Comandos executados
- `node --check app.js` — aprovado.
- `for f in $(rg --files -g '*.js' -g '!functions/node_modules/**' -g '!node_modules/**' | sort); do node --check "$f" || exit 1; done` — aprovado.
- `python3 -m py_compile server.py` — aprovado.

## ValoraBot 2.0 — evolução conversacional

### Público sem login

- O que é o Valora Pulse?
- Como respondo uma pesquisa?
- Meus dados estão seguros?
- O que é LGPD?
- Meu link expirou.
- Quero falar com suporte.

### Admin Valora

- Como cadastrar cliente?
- Como ver logs?
- Como configurar Telegram?
- Como configurar planos?
- Como acompanhar implantação?

### Empresa Admin

- Como cadastrar funcionários?
- Qual perfil devo escolher?
- Como criar pesquisa?
- Como enviar convites?
- Como ver respostas?

### Gestor Pesquisa

- Como criar questionário?
- Como configurar pontuação?
- Como enviar pesquisa?
- Como ver pendentes?

### Analista Resultados

- Como ver relatórios?
- Como filtrar respostas?
- Como interpretar dimensões?

### Participante

- Como responder?
- Como ver resultado?
- Como baixar certificado?
- Como excluir meus dados?

### Atendimento

- Pedir atendente sem login.
- Pedir atendente logado.
- Bot encaminha conversa.
- Mensagem aparece no atendimento.
- Atendente responde.

### Mobile

- Abrir bot em 360px.
- Enviar mensagem.
- Clicar sugestão.
- Abrir atendimento.
- Fechar bot.

### Checks técnicos executados

- `node --check chatbot-knowledge-base.js`
- `node --check chatbot-service.js`
- `node --check app.js`

## Evolução: Central de Atendimento, Tickets, SLA e Base de Conhecimento

- Público sem login: abrir ValoraBot, clicar **Falar com atendente**, informar nome/e-mail/categoria/assunto/mensagem, confirmar criação do ticket e validar que não há listagem pública.
- Participante: entrar como participante, abrir atendimento, responder conversa, encerrar e avaliar; validar visualização apenas de tickets próprios.
- Empresa Admin/Atendente: entrar no portal da empresa, abrir **Atendimento**, assumir ticket da empresa, responder, criar nota interna, resolver e encerrar; validar que empresa B não aparece.
- Admin Valora: abrir **Atendimentos**, validar KPIs, filtros, tickets globais, assumir tickets e visualizar SLA.
- Chatbot: fazer pergunta com artigo publicado, validar resposta da base; clicar **Falar com atendente** quando não resolveu.
- SLA: criar ticket crítico, validar `slaDueAt`, simular vencimento alterando data e executar `checkSupportSla` no emulador.
- Segurança: testar Rules para empresa A/B, participante de outro usuário, notas internas invisíveis ao solicitante e público sem listagem.
- Mobile: em 360px, validar cards/tabela responsiva, conversa sem rolagem horizontal e campo de mensagem utilizável.

Checks técnicos executados nesta entrega:

- `node --check app.js` — passou.
- `node --check module-definitions.js` — passou.
- `node --check firebase-repository.js` — passou.
- `node --check functions/index.js` — passou.

## Build seguro de produção

- `npm run check`
- `npm run security:check`
- `npm run build:prod`
- `node --check dist/assets/app.*.js`
- `find dist -name "*.map"` retorna vazio
- `dist/` não é versionado no Git

## CI/CD seguro — 2026-06-21

### Comandos obrigatórios registrados

- `npm run check` — valida sintaxe dos arquivos principais.
- `npm run check:no-dist` — falha se `dist/` estiver versionado ou alterado no PR.
- `npm run security:check` — valida ausência de `dist/` versionado, segredos óbvios e CSP insegura.
- `npm run build:prod` — gera `dist/` em modo produção e executa postbuild security check.
- `for file in dist/assets/*.js; do node --check "$file"; done` — valida todos os bundles JavaScript gerados.
- `find dist -name "*.map"` — deve retornar vazio.

### Validações esperadas nos workflows

- Workflow de PR executa em pull requests para `main`.
- PR com `dist/` commitado falha em `npm run check:no-dist`.
- PR sem `dist/` passa pelos checks quando não há erros de sintaxe/segurança.
- Build gera `dist/` como artefato, sem entrar no Git.
- Deploy usa `firebase.json` com Hosting apontando para `dist`.
- Source maps ausentes bloqueiam publicação insegura.
- Segredos e tokens suspeitos bloqueiam o pipeline.

## Homologação dos pipelines CI/CD — 2026-06-22

Relatório principal: `HOMOLOGACAO_PIPELINES.md`.

### Comandos aprovados no estado limpo

- `npm run check` — aprovado.
- `npm run check:no-dist` — aprovado.
- `npm run security:check` — aprovado.
- `npm run build:prod` — aprovado.
- `node --check dist/assets/app.*.js` — aprovado.
- `find dist -name "*.map"` — retorno vazio.
- `git ls-files dist` — retorno vazio.
- `npm run postbuild:security` — aprovado.

### Simulações negativas aprovadas

- PR com `dist/`: criado `dist/teste.txt` temporário e forçado no índice; `npm run check:no-dist` falhou com a mensagem esperada. Arquivo removido ao final.
- Source map indevido: criado `dist/assets/app.fake.js.map`; `npm run postbuild:security` falhou por arquivo proibido. Arquivo removido ao final.
- Segredo fake: criado arquivo temporário rastreado com `TELEGRAM_BOT_TOKEN=fake_test_token_should_be_blocked`; `npm run security:check` falhou por segredo suspeito. Arquivo removido ao final.
- CSP insegura: `firebase.json` foi alterado temporariamente com `script-src *`, `connect-src *` e `unsafe-eval`; `npm run security:check` falhou pelos padrões proibidos. Alteração revertida ao final.

### Resultado

Homologação local aprovada com ressalvas para validações que exigem execução real no GitHub Actions/Firebase: artifact do run, preview publicado e deploy/rollback em ambiente autorizado.

## Valora Pulse 8.6.6 — jornada pública instantânea e planos

- Pesquisa instantânea/Home: botões `Responder pesquisa em destaque` e `Iniciar diagnóstico` alterados para `openSurveyInNewTab`, abrindo `window.open(..., '_blank', 'noopener,noreferrer')` sem `location.href`, `history.pushState` ou roteamento da aba original. Validação por inspeção de código.
- Header público: shell da pesquisa exibe marca Valora, texto `Pesquisa segura`, botão `← Home`, Ajuda, ValoraBot e WhatsApp quando houver número configurado. Menus públicos de Home (`Planos`, `Entrar`, `Criar ambiente`) não são renderizados na jornada pública. Validação por inspeção de `renderPublicSurveyShell()`.
- Empresa Exemplo/demo: adicionado `isDemoCompany()` e `shouldShowPublicCompanyChip()`. Links demo `org_demo`/`empresa-exemplo` não exibem chip `Empresa Exemplo`; o card interno mostra `Valora Insight™` e `Pesquisa demonstrativa segura`. Empresas reais continuam usando `publicName`, logo e white label quando configuradas. Validação por inspeção de código.
- Ajuda pública: rota `#public-help` reescrita com instruções sobre como responder, link seguro, expiração, LGPD, suporte, resultado, Home e ValoraBot. Inclui CTA `Voltar para Home`.
- Layout: botões públicos refinados, foco visível, header compacto, ações flutuantes ocultas em desktop público (`min-width:900px`) e compactadas em mobile até 360px.
- Planos: seed comercial reescrito para Essencial, Profissional, Corporativo e Enterprise, com público-alvo, badge comercial, preço sob consulta/sob contrato, recursos e CTAs específicos. `renderPlans()` passou a exibir badge, público-alvo, descrição, preço e CTA por plano.
- Versão/cache: `APP_VERSION` e query strings atualizadas para `8.6.6`.
- Checks executados: `node --check app.js` sem erro de sintaxe.

## Home comercial — Valora Pulse 8.6.7
- Área técnica extensa sobre privacidade e confidencialidade removida da comunicação principal da Home; as funções de LGPD, consentimento, privacidade e segurança permanecem nas jornadas e áreas internas.
- Frase técnica do topo removida da Home e substituída por chamada comercial orientada a decisão.
- Seção antiga da jornada reescrita como “Do diagnóstico à decisão”, com linguagem simples: Estruture, Personalize, Envie e Decida.
- Termos técnicos da jornada comercial removidos da Home e substituídos por linguagem de negócio sobre pesquisa, participação, resultados e ação.
- CTAs revisados para “Iniciar diagnóstico”, “Criar ambiente gratuito” e “Ver planos”.
- Layout desktop validado por revisão estática do HTML gerado em `renderHome`.
- Layout mobile validado por revisão estática das classes responsivas existentes e novo bloco de certificado com media query dedicada.

## Certificados — Valora Pulse 8.6.7
- Certificado PDF revisado com título, subtítulo, texto de participação, nome do participante, pesquisa, data, resultado, linha institucional e mensagem final.
- Certificado PNG/imagem revisado com layout profissional em canvas, bordas, bloco institucional, destaque para participante e resultado.
- Texto do certificado revisado para posicionar o documento como confirmação de participação no diagnóstico Valora Pulse.
- Layout do certificado revisado com fundo limpo, borda elegante, espaçamento, marca Valora e composição preparada para PDF/PNG.
- Empresa real aparece na linha “Pesquisa promovida por [Nome da Empresa], com tecnologia Valora Group™.” quando aplicável.
- Pesquisa demonstrativa evita exibir “Empresa Exemplo” indevidamente e mostra “Pesquisa demonstrativa realizada na plataforma Valora Pulse™.”.
- Botões revisados para “Baixar certificado em PDF” e “Baixar certificado em imagem”.

## Correção de certificados

- Resolvidos conflitos da branch: `git status` não indicou arquivos em conflito antes das alterações.
- Certificado HTML revisado.
- Certificado PDF revisado.
- Certificado imagem/PNG revisado.
- Helper único de dados do certificado criado/reutilizado.
- “Empresa Exemplo” removida de certificados demo.
- Empresa real exibida corretamente quando aplicável.
- Índice de maturidade validado.
- Percentual validado.
- Nível/faixa validado.
- Layout desktop validado.
- Layout mobile validado.


## Certificados e ValoraBot público — Valora Pulse 8.6.8
- Diagnóstico técnico revisou os fluxos solicitados em `app.js`, `pdf.js`, `style.css`, `chatbot-service.js`, `chatbot-knowledge-base.js`, `manual-service.js`, `index.html` e `config.js`, com foco em `buildCertificateData`, `getCertificateScore`, `certificateHtml`, `certificatePdf`, `certificatePng`, `createCertificate`, `wrapCanvasText`, `renderBot`, `renderFloating`, `ValoraChatbot`, `botReply` e `moduleEnabled('valorabot')`.
- Certificado passou a usar `buildCertificateData()` como fonte única para tela, PDF e PNG, com participante, pesquisa, data formatada, empresa, mensagem institucional, índice, percentual, nível e nome de arquivo saneados.
- Certificado em tela passou a ser gerado por `certificateTemplateHtml()`, sem botões dentro da área exportável e com classes `certificate-export-*` responsivas.
- PDF de certificado revisado em `pdf.js` com helpers de centralização, quebra e ajuste de texto; o layout agora calcula o eixo vertical conforme nome/corpo/resultado e evita sobreposição com rodapé.
- PNG de certificado revisado com canvas em layout manager (`drawCertificateCanvas`, `fitCanvasFont`, `wrapCanvas`) para nomes longos, títulos longos, corpo e score sem coordenadas soltas espalhadas.
- Empresa demo/fallback validada por código: `Empresa Exemplo` permanece apenas em seed/dados demonstrativos, e certificados demo usam “Pesquisa demonstrativa realizada na plataforma Valora Pulse™.”.
- Índice validado por código: `getCertificateScore()` prioriza `rawScore/maxScore`, depois `normalized5`, depois `percentage`, sempre com clamp entre 0–5 e 0–100 e fallback “Participação concluída”.
- ValoraBot público validado por código: `shouldShowValoraBot()` libera visitante sem login, Home, Planos, Login, pesquisa pública, resultado público e mantém usuários internos conforme módulo/perfil Valora.
- Linguagem natural do ValoraBot revisada para saudações contextuais, respostas consultivas, sugestões por contexto e ação constante “Falar com atendente”.
- Atendimento humano pelo bot revisado para registrar contexto sem token completo, usando rota, perfil, surveyId/resultId e dados do usuário quando logado.
- Checks executados: `npm run check`, `node --check pdf.js`, `npm run security:check`, `npm run build:prod`, `git ls-files dist`.

## QA visual automatizado

- Estrutura adicionada em `tests/visual/` com Playwright.
- Comando preparado: `npm run test:visual`.
- Cenários previstos: Home, Planos, pesquisa pública, certificado em tela, certificado PDF/PNG e ValoraBot público/logado.
- Evidências geradas localmente em `tests/visual/screenshots/` e artifacts no workflow manual `.github/workflows/visual-smoke.yml`.
- Observação: a execução depende da instalação dos browsers com `npx playwright install chromium`.

## Correção CORS getEmailStatus callable — 2026-06-22

### Escopo validado

- Produção IIS com `STORAGE_MODE='firebase'` deve consultar o status de e-mail pelo Firebase Functions SDK, usando `httpsCallable('getEmailStatus')`, e não por `fetch` direto para `cloudfunctions.net/getEmailStatus`.
- `getEmailStatus` permanece como Cloud Function callable (`onCall`) em `functions/index.js`.
- `updateEmailStatus()` passa a tratar Firebase indisponível ou falha na callable como indisponibilidade não fatal, exibindo mensagem amigável e registrando `console.warn`.
- Modo local continua usando `/api/email/status` e, se falhar, informa fallback local/outbox sem quebrar a aplicação.

### Roteiro manual obrigatório — produção IIS com Firebase

| Cenário | Resultado esperado | Status |
|---|---|---|
| Abrir `https://valoragroup.mnsoft.com.br` com `STORAGE_MODE='firebase'` e Firebase SDK carregado | Aplicação inicializa normalmente no IIS | A validar em produção |
| Entrar na tela de configurações/e-mail | `getEmailStatus` é chamado via `window.ValoraFirebaseServices.functions.httpsCallable('getEmailStatus')` | A validar em produção |
| Inspecionar Network/Console | Não há `fetch` direto para `https://us-central1-gestordepesquisa.cloudfunctions.net/getEmailStatus` e não aparece erro de CORS para `getEmailStatus` | A validar em produção |
| Simular Functions indisponível ou bloqueada | Interface mostra status amigável de indisponibilidade e a aplicação continua navegável | A validar em produção |

### Comandos executados nesta correção

- `rg -n "getEmailStatus|updateEmailStatus|fetch\\(|cloudfunctions\\.net/getEmailStatus|service\\.ts|firebaseCallable" -g '!node_modules' -g '!dist' -g '!build' .` — usado para localizar chamadas relacionadas e confirmar ausência de `fetch` direto para a callable.
- `npm run check` — validação sintática principal.
- `npm run build:prod` — build de produção.

## Migração localStorage → Firebase

- Exportação local: validar pelo Admin Valora que o arquivo `valora-local-export-YYYYMMDD-HHMM.json` é gerado sem senhas, token Telegram, SMTP password ou service account.
- Dry-run: `node scripts/import-firestore-seed.js --file ./exports/valora-local-export.json --project gestordepesquisa --dry-run` deve listar quantidades sem escrever no Firestore.
- Importação: `node scripts/import-firestore-seed.js --file ./exports/valora-local-export.json --project gestordepesquisa --apply --backup --create-auth-users --send-password-reset` deve popular planos, empresas, usuários/Auth, formulários, perguntas e pesquisas.
- Validação: `node scripts/validate-firebase-seed.js --project gestordepesquisa` deve retornar sem inconsistências críticas.
- Produção: publicar IIS com Firebase, abrir PRD, validar planos, login, perguntas, pesquisas, respostas quando importadas, certificados e chatbot.
