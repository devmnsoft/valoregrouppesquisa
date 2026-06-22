# Valora Group™ 8.6.7 — versão funcional local

Plataforma web para criação, aplicação e análise de diagnósticos, pesquisas e provas de cultura, governança, liderança, pessoas, controladoria e advisory.

## Abrir no Windows

1. Extraia todo o ZIP para uma pasta nova.
2. Abra a pasta extraída.
3. Execute `INICIAR_SITE_WINDOWS.bat`.
4. Mantenha a janela do servidor aberta.
5. O navegador abrirá no endereço local escolhido automaticamente, como `http://127.0.0.1:8095`.

Não abra `index.html` diretamente. O servidor local é necessário para links, consulta de CEP/CNPJ, e-mail de teste e proteção contra bloqueios CORS.

No macOS ou Linux, execute:

```bash
./iniciar-site-mac-linux.sh
```

Requisito: Python 3.9 ou superior. O sistema usa apenas a biblioteca padrão do Python e não exige `npm`, instalação de pacotes ou internet para as funções principais.

## Credenciais locais de demonstração

| Perfil | E-mail | Senha |
|---|---|---|
| Administrador geral Valora | `admin@valoragroup.com` | `Valora@2026` |
| Consultor Valora | `consultor@valoragroup.com` | `Valora@2026` |
| Administrador da empresa | `gestor@empresa.com` | `Empresa@2026` |
| Gestor de pesquisa | `rh@empresa.com` | `Empresa@2026` |
| Participante | `participante@empresa.com` | `123456` |

Estas credenciais existem somente para demonstração local. Em produção, substitua o armazenamento local por Firebase Authentication, redefina todos os acessos e aplique autenticação multifator aos administradores.

## Jornadas implementadas

### Administrador geral Valora

Acesso global a clientes, planos, financeiro, módulos, usuários, formulários, pesquisas, respostas, relatórios, configurações da home, e-mail, LGPD, logs e backup/migração. **Backup e logs são exclusivos deste perfil** e não aparecem nos portais da empresa ou do participante.

### Administrador da empresa e gestor de pesquisa

Acesso isolado ao ambiente da própria empresa, formulários, pesquisas, links seguros, usuários autorizados, respostas, indicadores, relatórios e consumo do plano. Nenhuma empresa visualiza dados de outra organização.

### Participante

Acesso às pesquisas disponíveis, histórico de respostas, resultados, relatórios individuais, certificados e solicitações de privacidade. Links públicos exigem identificação, consentimento LGPD e respeitam validade e status da pesquisa.

## Formulários e provas dinâmicas

Cada pergunta pode ser configurada individualmente como:

- escala de 1 a 5;
- escolha única por radio button;
- múltipla escolha por checkbox;
- resposta curta por text box;
- resposta longa por text area;
- questão de prova com uma única resposta correta.

O editor permite dimensões, ajuda ao respondente, obrigatoriedade, peso, pontuação máxima, nota por alternativa, resposta correta, nota de preenchimento, método de cálculo e faixas de resultado. Há revisão estrutural e campos com `spellcheck` do navegador.

Formulários podem ser criados, editados, clonados, excluídos e usados para criar uma pesquisa instantânea. A pesquisa gerada pode ser compartilhada imediatamente por link, WhatsApp ou e-mail.

## Links seguros e LGPD

Cada pesquisa possui token aleatório, status, data de início e expiração. A jornada do participante valida o token e a validade antes de exibir o questionário. O participante informa pessoa física ou jurídica, nome, e-mail, telefone, preferência de WhatsApp e, quando aplicável, CPF/CNPJ. O aceite LGPD é obrigatório quando configurado.

A versão local demonstra esta jornada. Em produção, use Cloud Functions para validar no servidor o hash do token, registrar tentativas, limitar taxa de acesso e impedir exposição direta dos documentos do Firestore.

## Relatórios e certificados

Exportações disponíveis, conforme perfil e plano:

- PDF nativo;
- Word compatível (`.doc`);
- Excel compatível (`.xls`);
- JSON;
- CSV;
- certificado PDF;
- certificado PNG.

Os PDFs são gerados localmente por `pdf.js`, sem CDN ou biblioteca externa.

## E-mail

O remetente padrão está preenchido como `valoragroup@mnsoft.com.br`.

O modo inicial é **Caixa de saída de teste**. Ao enviar, o servidor cria um arquivo `.eml` em `data/outbox`, permitindo validar assunto, conteúdo, logo, links e destinatário sem disparar mensagem real.

Para SMTP real, entre como Administrador geral e acesse **Home, e-mail e LGPD**. Informe servidor, porta, segurança, usuário e senha. A senha:

- não está no ZIP;
- não é gravada no `localStorage`;
- não entra em backup JSON;
- é salva apenas em `data/email_config.json`, no servidor local;
- também pode ser fornecida pela variável de ambiente `VALORA_SMTP_PASSWORD`.

Nunca publique esse arquivo nem inclua senhas no JavaScript, Firebase Hosting ou repositório.

## Dados e integrações

- CEP: ViaCEP com fallback BrasilAPI, por meio do servidor local.
- CNPJ: BrasilAPI, por meio do servidor local.
- Persistência de demonstração: `localStorage` do navegador.
- Estrutura de produção: Firebase Authentication, Firestore, Functions e Hosting, conforme `FIREBASE_SETUP.md`.

As consultas externas dependem de conexão com a internet e disponibilidade dos provedores.

## Planos iniciais parametrizáveis

| Plano | Valor inicial | Pesquisas ativas | Respostas/mês | Gestores |
|---|---:|---:|---:|---:|
| Gratuito | Grátis | 1 | 30 | 1 |
| Essencial | R$ 590/mês | 3 | 150 | 2 |
| Growth | R$ 1.490/mês | 12 | 1.000 | 8 |
| Enterprise | R$ 3.900/mês | ilimitadas | 10.000 | 50 |

O administrador geral pode editar valores, limites, destaques e funcionalidades.

## Arquivos principais

- `index.html`: estrutura da aplicação.
- `style.css`: identidade visual e responsividade.
- `app.js`: jornadas, permissões, CRUDs, dashboards e regras da demonstração.
- `pdf.js`: gerador local de PDFs.
- `server.py`: servidor local, e-mail, CEP e CNPJ.
- `firestore.rules`: ponto de partida conservador para produção.
- `database.sample.json`: exemplo de coleções sem segredos.
- `TESTES_EXECUTADOS.md`: matriz de validação desta entrega.

## Limite da versão local

Esta entrega é um MVP funcional para validação do produto e da experiência. `localStorage` não substitui uma base multiusuário de produção. Para publicação comercial, implemente Firebase Auth, Firestore, Functions, trilha imutável, hashing de senhas/tokens, controle de sessão, backups gerenciados e monitoramento.

### Cloud Functions para e-mail, CEP e CNPJ em produção

Em `STORAGE_MODE: 'firebase'`, o frontend chama Cloud Functions (`sendEmail`, `getEmailStatus`, `lookupCep` e `lookupCnpj`) e não depende do `server.py`. O `server.py` permanece somente para demonstração local (`STORAGE_MODE: 'local'`).

Configure segredos e variáveis de ambiente antes do deploy:

```bash
firebase functions:secrets:set SMTP_PASSWORD
firebase functions:config:set smtp.host="smtp.seudominio.com.br" # legado, se utilizado no seu projeto
```

Para a implementação atual em Functions v2, defina também as variáveis de ambiente não secretas no ambiente de deploy/console Firebase:

```bash
SMTP_HOST=smtp.seudominio.com.br
SMTP_PORT=587
SMTP_SECURITY=starttls
SMTP_USERNAME=usuario@seudominio.com.br
SMTP_SENDER_EMAIL=nao-responda@seudominio.com.br
SMTP_SENDER_NAME="Valora Group"
```

Regras de segurança implementadas nas Functions:

- `sendEmail` exige usuário autenticado e limita envio a `admin_valora`, `empresa_admin` e `gestor_pesquisa`.
- `empresa_admin` e `gestor_pesquisa` só enviam templates `invite` e `result` vinculados à própria empresa.
- `participante` não tem envio livre.
- `SMTP_PASSWORD` é lido via Secret Manager e nunca enviado ao navegador ou gravado no Firestore.
- `getEmailStatus` retorna somente `configured`, `senderName` e `senderEmail` mascarado.
- `lookupCep` valida 8 dígitos, consulta ViaCEP com fallback BrasilAPI e aplica rate limit.
- `lookupCnpj` valida 14 dígitos, exige autenticação, consulta BrasilAPI e retorna apenas dados cadastrais necessários.

## Versão, cache e modos de execução

A versão canônica desta entrega é **Valora Group™ 8.6.7** e fica centralizada em `config.js` por meio de `APP_VERSION`. O `app.js` consome `window.ValoraConfig.APP_VERSION`; o `index.html` usa a mesma versão nas query strings dos assets (`?v=8.6.7`) para facilitar invalidação de cache e diagnóstico de suporte.

### Modo local/demo

Use `server.py` apenas para demonstração local em `STORAGE_MODE: 'local'`. Ele permanece vinculado a `127.0.0.1`, expõe `/api/health`, proxy local de CEP/CNPJ e caixa de saída de e-mail. Para conferir a versão local:

```bash
curl http://127.0.0.1:8095/api/health
```

A resposta deve conter `"version": "8.6.7"`. Se o servidor escolher outra porta, use a porta exibida no terminal.

### Modo Firebase/produção

Em produção, publique pelo Firebase Hosting com `STORAGE_MODE: 'firebase'`, `FIREBASE_ENABLED: true`, Firestore Rules revisadas e Cloud Functions para e-mail, CEP, CNPJ e links públicos. O Firebase Hosting aplica os headers definidos em `firebase.json`: HTML sem cache, assets versionados com cache longo, `X-Content-Type-Options`, proteção de frame, `Referrer-Policy`, `Permissions-Policy` e CSP inicial.

A CSP permite scripts locais e SDK Firebase em `https://www.gstatic.com`; conexões para `https://www.gstatic.com`, Firebase/Google APIs, Cloud Functions, ViaCEP e BrasilAPI; imagens locais, `data:` e `blob:`. O `style-src` mantém `'unsafe-inline'` temporariamente para evitar quebra visual durante esta etapa de hardening. TODO: remover estilos inline e trocar por classes CSS para retirar `'unsafe-inline'` da política.

### Invalidação de cache

Para invalidar cache em uma nova entrega:

1. Atualize `APP_VERSION` em `config.js`.
2. Atualize as query strings dos assets no `index.html`.
3. Publique novamente no Firebase Hosting.
4. Oriente usuários a recarregar com atualização forçada se ainda houver sessão antiga (`Ctrl+F5`/`Cmd+Shift+R`).

O HTML é servido com `no-store`; JS, CSS e imagens podem ter cache longo porque os caminhos dos assets são versionados.

## Atualização da jornada operacional (2026-06-19)

Esta versão consolida a operação ponta a ponta do Valora Pulse em modo local/demo e deixa pontos de integração preparados para Firebase/produção.

### Funcionários/respondentes

A tela **Empresa › Usuários** passa a operar também como cadastro de funcionários/respondentes. A abordagem escolhida foi manter respondentes com login ou histórico em `users` com `role: participante`, `type: funcionario`, `companyId`, `position` e `department`. Essa escolha evita duplicidade entre cadastro de funcionário e conta de participante no modo local; em produção Firebase, a mesma decisão pode ser materializada em `users/{uid}` quando houver login ou em uma coleção `participants/employees` espelhada quando o convidado não tiver autenticação.

Regras implementadas:

- funcionário sempre vinculado ao `companyId` da empresa atual;
- empresa admin não consegue escolher outra empresa;
- e-mail único na base local;
- campos de cargo/função, área/departamento, telefone e status ativo/inativo;
- pesquisa, filtro de status, seleção múltipla, selecionar todos, limpar seleção e contador;
- desativar/reativar sem excluir histórico.

### Envio de questionários por e-mail

O envio pode partir de **Pesquisas e links › Compartilhar › Enviar para funcionários** ou de **Usuários › Enviar pesquisa aos selecionados**. O sistema cria registros em `state.invitations` com status `pending`, `sent`, `failed`, `answered`, `opened` ou `expired` conforme a evolução disponível. Em modo local/demo, o envio usa `server.py` e gera `.eml` em `data/outbox` quando SMTP real não estiver configurado. Em Firebase/produção, o navegador não deve enviar SMTP diretamente: a integração prevista é por Cloud Functions com secrets/variáveis do backend.

### Cálculo centralizado

O cálculo foi centralizado em `calculateSurveyResult(form, answers)` e mantido compatível com o alias interno `calculateResult`. A regra é:

- escala 1 a 5: `nota / 5 * pontuação máxima * peso`;
- escolha única: pontuação da alternativa selecionada, com peso;
- múltipla escolha: soma das alternativas marcadas limitada à pontuação máxima da pergunta, com peso;
- texto curto/longo: pontua somente quando `scoreWhenFilled` estiver configurado; respostas qualitativas sem pontuação não reduzem a nota;
- resposta correta: acerto recebe pontuação máxima; erro recebe zero ou score configurado na alternativa;
- dimensões recebem apenas perguntas vinculadas à dimensão;
- retorno inclui pontuação bruta, máxima, percentual, nota 0–5, resultado por dimensão, faixa e recomendações.

### ValoraBot contextual

O ValoraBot agora considera perfil logado e rota/tela atual nas respostas. Ele cobre funcionários, convites/e-mail, LGPD, cálculo, peso, dimensão, respostas, dashboards, relatórios, tipos de pergunta e próximos passos práticos sem prometer recursos inexistentes.

### Pendências para Firebase/produção

- Implementar Cloud Functions para envio em lote, atualização de status `opened/answered/expired` e reenvio.
- Persistir `invitations` em Firestore com regras por `companyId`.
- Definir se convidados sem login ficam em `participants/employees` ou são provisionados em Auth + `users/{uid}`.
- Criar índices Firestore para respostas por empresa, pesquisa, período e status.
- Validar juridicamente texto LGPD e contratos controlador/operador.

## Perfis de usuário e permissões

A gestão de perfis do Valora Pulse é centralizada em `ROLE_DEFINITIONS` no `app.js`. Os perfis oficiais são: `admin_valora`, `consultor_valora`, `empresa_admin`, `gestor_pesquisa`, `analista_resultados`, `gestor_area`, `participante` e `convidado_externo`.

- O cadastro de cliente permite criar o Administrador da Empresa com perfil fixo `empresa_admin`.
- O cadastro de funcionários permite escolher somente perfis de empresa, com padrão `participante`.
- Menus, filtros, envio de questionários e validações consultam a matriz central sempre que possível.
- Consulte `PERFIS_E_PERMISSOES.md` para a tabela funcional e preparação Firebase.

## Arquitetura comercial SaaS
A documentação comercial central está em `PRODUTO_COMERCIAL_VALORA.md`. A nomenclatura técnica recomendada para produção é `organizations`; a UI usa “Clientes/Empresas” e o modo demo preserva `companies` como adaptador local. Perfis estão centralizados em `role-definitions.js` e módulos em `module-definitions.js`.

## Base comercial e operacional SaaS

A fundação de perfis, permissões, módulos e planos foi centralizada para reduzir regras espalhadas no frontend e manter compatibilidade com modo local/demo.

- Perfis oficiais e helpers: `role-definitions.js`.
- Módulos oficiais e helpers: `module-definitions.js`.
- Planos controlam `enabledModules` e limites como pesquisas ativas, respostas/mês, gestores, funcionários e e-mails/mês.
- Empresas devem possuir ao menos um `empresa_admin`.
- Funcionários/respondentes devem receber perfil explícito.
- Firestore Rules mantêm respostas públicas somente por Cloud Function e reconhecem os perfis empresariais de leitura/área/convidado.

Documentação complementar:

- `PERFIS_E_PERMISSOES.md`
- `MODULOS_E_PLANOS.md`
- `PRODUTO_COMERCIAL_VALORA.md`
- `TESTES_EXECUTADOS.md`

## Modo Firebase real

A aplicação agora mantém dois modos de persistência:

- `STORAGE_MODE: 'local'`: usa `local-repository.js`, `localStorage` e as credenciais de demonstração desta página.
- `STORAGE_MODE: 'firebase'`: usa Firebase Auth para sessão e Cloud Firestore como fonte da verdade por meio de `firebase-repository.js`.

No modo Firebase, o estado que o `app.js` consome continua compatível com os nomes atuais (`state.companies`, `state.users`, `state.plans`, `state.modules`, `state.forms`, `state.surveys`, `state.responses`, `state.invitations`, `state.invoices`, `state.settings` e `state.logs`). A coleção Firestore canônica para empresas é `organizations`; ela é adaptada para `state.companies` para preservar as telas atuais.

Coleções esperadas no Firestore: `organizations`, `users`, `plans`, `modules`, `forms`, `surveys`, `responses`, `invitations`, `invoices`, `settings` e `logs`.

Para ativar o Firebase, configure `config.js` com `STORAGE_MODE: 'firebase'`, `FIREBASE_ENABLED: true` e a configuração pública do app Web Firebase. Não inclua senhas, tokens privados, service accounts ou secrets no frontend.

## Firebase real — jornada SaaS homologável

Para operar em produção, use `STORAGE_MODE='firebase'`, publique Rules/Functions/Hosting, importe `firestore.seed.sample.json` via Admin SDK e crie o primeiro `admin_valora` no Firebase Auth sem senha em arquivos versionados. O envio em lote de convites no Firebase usa a Cloud Function `sendSurveyInvitations`; no modo local/demo permanece usando outbox ou fallback local, preservando a demonstração.

Consulte `FIREBASE_SETUP.md`, `CHECKLIST_PRODUCAO.md` e `TESTES_EXECUTADOS.md` para seed, primeiro acesso, SMTP por secrets, jornadas por perfil, limites de plano, resposta pública por token e riscos restantes.

## Onboarding guiado e primeira implantação

O dashboard da empresa agora conduz o cliente pelo fluxo **Primeiros passos da implantação**, do primeiro acesso até a primeira resposta e o primeiro relatório. O progresso é calculado dinamicamente por empresa com os pesos: dados básicos (+10%), plano (+10%), administrador (+10%), ao menos um funcionário (+15%), ao menos um formulário (+15%), ao menos uma pesquisa ativa (+15%), ao menos um convite (+10%), ao menos uma resposta (+10%) e relatório disponível (+5%).

A empresa sai de `novo` para `implantado` conforme completa as etapas. O status intermediário pode ser `configurando`, `funcionarios_cadastrados`, `pesquisa_criada`, `convites_enviados`, `com_respostas` ou `travado` quando permanece sem evolução suficiente por muitos dias.

### Fluxo recomendado em modo local

1. Inicie o servidor local com `python3 server.py` ou use os scripts `iniciar-site-mac-linux.sh` / `INICIAR_SITE_WINDOWS.bat`.
2. Entre como `empresa_admin` de demonstração.
3. Abra **Empresa › Visão geral** e confira o checklist **Primeiros passos da implantação**.
4. Clique em **Configurar minha primeira pesquisa**.
5. Revise dados da empresa, cadastre ao menos um funcionário, escolha ou gere o **Diagnóstico Essencial de Maturidade**, crie a pesquisa e revise o envio dos convites.
6. Abra o link da pesquisa, responda como participante e retorne ao dashboard para ver respostas, taxa de resposta e relatório liberado.

### Fluxo recomendado em Firebase

Em `STORAGE_MODE='firebase'`, o mesmo checklist usa coleções reais (`organizations`, `users`, `forms`, `surveys`, `invitations` e `responses`). O envio em massa continua passando por Cloud Functions; o frontend apenas abre o fluxo e recarrega o estado quando a Function confirma o processamento. Não há exposição de credenciais SMTP no navegador.

## Indicadores executivos e consistência

Os dashboards executivos usam `analytics-service.js` como camada central de indicadores. A regra padrão é contar apenas respostas concluídas e convites efetivamente enviados (`sent`, `opened`, `answered`, `resent`). A taxa de resposta é calculada como respostas concluídas únicas divididas por convites enviados únicos. A média geral usa `normalized5` e o percentual é `normalized5 / 5 * 100`.

A área de respostas e a central de relatórios usam os mesmos filtros e métricas para evitar divergência entre dashboard, relatório e exportação. Em ambientes sem dados, os cards exibem estados vazios com orientação operacional.

## Relatórios executivos consultivos

A versão atual adiciona `report-service.js` como camada central de relatórios. Ela reutiliza `analytics-service.js` para manter os mesmos cálculos dos dashboards em PDF, Word, Excel, CSV e JSON.

Relatórios disponíveis na Central de relatórios:

- Executivo global Valora;
- Executivo da empresa;
- Por pesquisa;
- Individual do participante;
- Por dimensão;
- Implantação;
- Uso do plano.

Cada relatório possui sumário executivo automático, recomendações, plano de ação sugerido, tratamento de estados sem dados e auditoria de exportação.

## Módulo Plano de Ação

O Valora Pulse agora inclui o módulo **Plano de ação**, com criação manual de ações, geração automática por resultados, acompanhamento por status/prioridade/prazo, evidências, comentários e integração com relatórios executivos. A coleção Firestore adotada é `actionPlans`, segregada por `companyId` e protegida pelas regras de perfil.

Consulte `PLANO_DE_ACAO.md` para modelo de dados, permissões, status, prioridades e roteiro de testes.


## Central de Notificações e Alertas Inteligentes

O Valora Pulse agora possui sino global, contador de não lidas, dropdown, tela “Central de notificações”, ações para marcar como lida/dispensar e links rápidos. A documentação completa de tipos, regras, permissões, Firestore e lembretes automáticos está em `NOTIFICACOES_E_ALERTAS.md`.

## Evolução white label e assinatura

O Valora Pulse possui estrutura de white label por organização, com slug público, URL de logo, cores por empresa, preferências de marca em pesquisa pública/e-mails e portal de assinatura/uso do plano. A documentação completa está em [WHITE_LABEL_E_ASSINATURA.md](WHITE_LABEL_E_ASSINATURA.md).

## Assinaturas e cobrança

A base financeira do SaaS agora inclui assinatura em `organizations.subscription`, coleção `invoices`, painel Admin Valora › Financeiro, visão Empresa › Plano contratado, bloqueios por status comercial e estrutura segura para gateway via Cloud Functions. Detalhes completos em [`ASSINATURAS_E_COBRANCA.md`](ASSINATURAS_E_COBRANCA.md).

## Integrações, API pública e webhooks
O Valora Pulse possui base para integrações corporativas por empresa: API keys com hash, webhooks assinados, importação de funcionários, exportações estruturadas e logs. Consulte `INTEGRACOES_API_WEBHOOKS.md` para endpoints, escopos, eventos, HMAC, importação CSV, exportação BI e cuidados de LGPD.


## Observabilidade operacional
A aplicação possui serviço central em `log-service.js`, painel **Logs e Monitoramento**, exportação CSV/JSON e integração segura com Telegram via Cloud Functions. Consulte `OBSERVABILIDADE_E_LOGS.md` e `TELEGRAM_ALERTAS.md`.

## Status da release

**Versão:** Valora Group™ 8.6.7 RC1
**Data do release candidate:** 2026-06-21
**Status:** aprovado com ressalvas para apresentação controlada e homologação com cliente.

### Modo local

O modo local/demo permanece configurado em `config.js` com `STORAGE_MODE: 'local'` e `FIREBASE_ENABLED: false`. Use este modo para demonstrações controladas, testes de fluxo e validação de experiência sem dependência de infraestrutura externa.

Para testar localmente:

```bash
./iniciar-site-mac-linux.sh
```

No Windows, use `INICIAR_SITE_WINDOWS.bat`. Após abrir o sistema, valide login, base local limpa, recriação da base, questionários, pesquisas, participação, dashboards, relatórios, logs e exportações.

### Modo Firebase

O modo Firebase/produção deve ser habilitado somente após configurar Firebase Auth, Firestore, Cloud Functions, Hosting, Rules, secrets, usuários, claims e dados seed. Altere `STORAGE_MODE` para `'firebase'`, defina `FIREBASE_ENABLED: true` e preencha somente as chaves públicas do projeto em `FIREBASE_CONFIG`.

Antes de deploy produtivo, valide:

- Firestore Rules com usuários e claims reais;
- Cloud Functions com payloads válidos e inválidos;
- secrets via Secret Manager ou variáveis de ambiente seguras;
- SMTP real;
- Telegram real ou sandbox operacional;
- isolamento Empresa A x Empresa B;
- resposta pública via Function;
- logs sem dados sensíveis.

### Como testar a release candidate

1. Ler `RELEASE_CANDIDATE.md` para entender escopo, riscos, pendências e decisão preliminar.
2. Executar `CHECKLIST_HOMOLOGACAO_FINAL.md` item a item.
3. Executar `ROTEIRO_TESTE_PONTA_A_PONTA.md` com evidências.
4. Registrar bugs em `BUGS_HOMOLOGACAO.md`.
5. Validar a regressão de sintaxe e runtime descrita em `CHECKLIST_REGRESSAO_TECNICA.md`.
6. Consolidar o aceite em `ACEITE_PRODUTO.md` e atualizar `TESTES_EXECUTADOS.md`.

### Documentos de homologação

- `RELEASE_CANDIDATE.md` — decisão, escopo, riscos, bloqueantes e pendências da release.
- `CHECKLIST_HOMOLOGACAO_FINAL.md` — checklist funcional completo por área.
- `BUGS_HOMOLOGACAO.md` — matriz de bugs e critérios de severidade.
- `CHECKLIST_REGRESSAO_TECNICA.md` — regressão técnica e checagens JavaScript.
- `ROTEIRO_TESTE_PONTA_A_PONTA.md` — fluxo principal ponta a ponta.
- `ACEITE_PRODUTO.md` — critérios de aceite por produto, técnico, segurança, comercial e operação.
- `TESTES_EXECUTADOS.md` — histórico consolidado de testes executados e pendências.

### Riscos conhecidos

- O modo `localStorage` é adequado para demonstração, mas não para produção multiusuário.
- A homologação Firebase real depende de ambiente externo completo, credenciais, claims e dados seed.
- SMTP, Telegram, webhooks e integrações dependem de chaves e disponibilidade de serviços externos.
- A CSP ainda possui exceção temporária para estilos inline, que deve ser removida em etapa futura de hardening visual.
- A publicação em produção exige execução manual da homologação real, evidências e correção dos bloqueantes encontrados.

### Próximos passos

1. Executar homologação real com QA e responsável de produto.
2. Corrigir bugs bloqueantes encontrados.
3. Retestar os bloqueantes corrigidos.
4. Registrar evidências finais.
5. Obter aceite formal do responsável.
6. Preparar deploy controlado com plano de rollback.


## Confiabilidade, manuais e suporte

Esta versão adiciona logger global sanitizado, manuais por perfil, ValoraBot contextual e atendimento humano local/demo com preparação Firebase. Consulte `LOGGER_E_TRY_CATCH.md`, `MANUAIS_POR_PERFIL.md`, `CHATBOT_E_ATENDIMENTO.md` e `SUPORTE_HUMANO.md`.


## ValoraBot 2.0

O Valora Pulse inclui o ValoraBot 2.0, assistente de produto contextual baseado em regras, perfil logado, rota atual, manual por perfil e base de conhecimento interna. O bot responde visitantes públicos, participantes e perfis administrativos sem depender de API externa nem chave de IA no frontend.

Documentação relacionada:

- `CHATBOT_VALORABOT.md` — arquitetura, intenções, contexto, logs e evolução futura.
- `MANUAIS_POR_PERFIL.md` — manuais usados pelo bot por perfil.
- `SUPORTE_HUMANO.md` — fluxo de atendimento humano e segurança.
- `TESTES_EXECUTADOS.md` — roteiro de testes por perfil, atendimento e mobile.

## Central de Atendimento, Tickets, SLA e Base de Conhecimento

Esta evolução adiciona o módulo `support` com fila de atendimentos, criação de tickets para usuários logados e públicos, mensagens, notas internas, categorias, SLA, avaliação, base de conhecimento e preparação Firebase/Cloud Functions. Consulte `CENTRAL_DE_ATENDIMENTO.md`, `BASE_DE_CONHECIMENTO.md` e `SLA_ATENDIMENTO.md`.

## Build e deploy Firebase Hosting

O Firebase Hosting de produção publica a pasta `dist/`. Essa pasta é gerada pelo build e não deve ser commitada no repositório.

Antes de publicar, execute:

```bash
npm run build:prod
firebase deploy --only hosting
```

Não commitar a pasta `dist/`, arquivos hashados de build, source maps ou binários copiados para `dist/assets/`.

## CI/CD seguro

O repositório possui workflows de CI/CD para proteger o build de produção:

- PRs para `main` executam validação sintática, bloqueio de `dist/`, checks de segurança, build de produção, validação dos bundles JavaScript e bloqueio de source maps.
- Builds seguros em `main` geram artefato `dist/` no GitHub Actions sem commitar a pasta.
- Preview Firebase pode publicar canal `pr-<numero>` em PRs internos quando os secrets de homologação estiverem configurados.
- Deploy de produção é controlado por `workflow_dispatch` ou tags `v*`, usando environment `production` para aprovação manual.

Consulte `GITHUB_ACTIONS.md`, `AMBIENTES_E_DEPLOY.md`, `GITHUB_SECRETS.md`, `CHECKLIST_RELEASE.md` e `ROLLBACK_PRODUCAO.md` antes de publicar.

## QA visual com Playwright

O projeto possui uma etapa de QA visual em `tests/visual/` para gerar evidências antes de produção, cobrindo Home, Planos, pesquisa pública, resultado/certificado, downloads PDF/PNG e ValoraBot em jornadas públicas e logadas.

Comandos principais:

```bash
npm install
npx playwright install chromium
npm run test:visual
npm run test:visual:headed
```

Por padrão, o Playwright sobe `VALORA_PORT=8095 python server.py`. Para reaproveitar um servidor local já aberto, use `VISUAL_SKIP_WEBSERVER=1 VISUAL_BASE_URL=http://127.0.0.1:8095 npm run test:visual`.

Screenshots e traces ficam em `tests/visual/screenshots/`, `tests/visual/output/`, `test-results/` e `playwright-report/`; esses artefatos são ignorados pelo Git e não devem ser commitados.

## Migração localStorage → Firebase

Produção Firebase exige importação/seed inicial. Os dados testados no modo local ficam no navegador (`localStorage`, chave `valoraPulseFinal800`) e não aparecem automaticamente no Firestore ao mudar `STORAGE_MODE` para `firebase`. Use o painel **Admin Valora › Backup e migração** para exportar JSON sanitizado e depois rode `scripts/import-firestore-seed.js` com `--dry-run` antes de `--apply`. Veja `MIGRACAO_LOCAL_FIREBASE.md`.

## Bootstrap inicial PRD Firebase

Use os scripts de bootstrap somente a partir de uma estação segura com credenciais administrativas via Application Default Credentials ou ambiente controlado de CI/CD. Não versione service accounts, exports, backups ou senhas.

### Dry-run obrigatório

```bash
node scripts/bootstrap-firebase-prd.js --project gestordepesquisa --dry-run
```

O dry-run lista as criações/mesclagens planejadas e não grava no Firebase. Sem `--apply`, o script nunca escreve dados.

### Aplicar bootstrap

```bash
node scripts/bootstrap-firebase-prd.js --project gestordepesquisa --apply --admin-email admin@valoragroup.com.br --admin-name "Admin Valora"
```

Parâmetros principais: `--project`, `--dry-run`, `--apply`, `--admin-email`, `--admin-name`, `--admin-password`, `--seed-demo-survey`, `--seed-demo-response`, `--merge` e `--overwrite`. O padrão é `--merge`; `--overwrite` exige `--confirm-overwrite gestordepesquisa`. A senha nunca é exibida em log. Se `--admin-password` não for informado, uma senha forte temporária é gerada e deve ser substituída por fluxo seguro de redefinição.

O bootstrap cria/atualiza de forma idempotente: Admin Valora em Auth e `users/{uid}`, custom claims `{ role: 'admin_valora', companyId: '' }`, `settings/global`, planos comerciais, módulos, `organizations/org_valora_prd`, compatibilidade em `companies/org_valora_prd`, formulário Valora Insight™, pesquisa ativa com `tokenHash`, base de conhecimento, categorias de atendimento e políticas de SLA. Resposta demo só é criada com `--seed-demo-response`.

### Validar pós-bootstrap

```bash
node scripts/validate-prd-bootstrap.js --project gestordepesquisa
```

A validação confirma Admin em Auth/Firestore, custom claims, planos, módulos, `settings/global`, organização, formulário, pesquisa ativa, vínculo survey→form e referências plan→modules. Depois, validar manualmente no IIS: login admin, portal admin, planos, empresa/plano contratado, perguntas, pesquisa pública, envio de resposta, resultado, certificado e ValoraBot.

### getEmailStatus em PRD

Em modo Firebase, `getEmailStatus` deve continuar sendo chamada por `httpsCallable('getEmailStatus')`, nunca por `fetch` direto para `cloudfunctions.net/getEmailStatus`. Se o e-mail não estiver configurado, a aplicação deve mostrar status amigável e não quebrar o carregamento.

## Publicador IIS PRD

O deploy em produção no IIS deve usar o publicador Node:

```powershell
npm run publish:iis:dry -- --iis-path C:\inetpub\wwwroot\valoragroup
npm run publish:iis -- --iis-path C:\inetpub\wwwroot\valoragroup
npm run package:iis
```

Veja `PUBLICADOR_IIS_PRD.md` para publicação com dados locais, backup, rollback e validações.

## Health Check PRD pós-publicação

Use o script `scripts/healthcheck-prd.js` para validar IIS, HTML, assets JS/CSS, MIME, Firebase, Functions, Firestore opcional, pesquisa pública opcional e ValoraBot público após publicar.

```bash
node scripts/healthcheck-prd.js --url https://valoragroup.mnsoft.com.br --project gestordepesquisa --check-firebase --check-functions
```

Integrado ao publicador IIS:

```bash
node scripts/publish-iis-prd.js --iis-path C:\inetpub\wwwroot\valoragroup --mode firebase --apply --health-url https://valoragroup.mnsoft.com.br --project gestordepesquisa
```

Relatórios são gerados em `publish/reports/` e não devem ser commitados.

## Publicador Windows PRD/IIS

Para publicar no IIS sem decorar comandos, use no Windows Server:

```text
tools\windows\Publicar-Valora-PRD.bat
```

Também há atalhos para simular, gerar pacote, rodar health check e restaurar backup em `tools/windows/`. Veja `PUBLICADOR_WINDOWS_PRD.md`.
