# Auditoria de Migração Firebase/Firestore para PostgreSQL

## Escopo analisado

Arquivos de runtime e integração revisados: `config.js`, `firebase-init.js`, `firebase-repository.js`, `repository.js`, `app.js`, `communication-gateway/src/*`, scripts de bootstrap/validação/importação em `scripts/`, `firestore.rules` e documentos de contratos existentes.

## Diagnóstico executivo

O sistema atual é um frontend web que seleciona `ValoraFirebaseRepository` quando `STORAGE_MODE='firebase'`. A autenticação usa Firebase Auth, perfis vivem em `users/{uid}` e dados operacionais são lidos de coleções Firestore diretamente pelo navegador. O gateway de comunicação já é um backend Node/Express, porém ainda lê Firestore e grava `communications`.

A migração para PostgreSQL é recomendada porque:
- regras comerciais e cálculo de resultado estão muito próximos do frontend;
- Firestore aceita documentos heterogêneos, exigindo normalização antes de relatórios robustos;
- controle de plano e permissões precisa ser transacional e auditável;
- e-mail, certificados e auditoria devem ser backend-first.

## Coleções e modelos implícitos

| Coleção atual | Campos encontrados/esperados | Relacionamentos | Problemas de normalização | Dependência frontend/Auth/Rules | Risco | Ordem |
|---|---|---|---|---|---:|---:|
| `settings` | `id`, `featuredSurveyId`, FAQ, parâmetros públicos | referência a `surveys` | ora objeto por documento, ora objeto público agregado | frontend lê para home e recursos públicos | Médio | 2 |
| `modules` | `id`, `name`, flags, ordem | planos via `enabledModules` | capabilities duplicadas em planos e módulos | frontend decide menus | Médio | 7 |
| `plans` | `id`, `name`, preço, limites, `enabledModules`, flags comerciais | organizações por `planId`; assinaturas embutidas | limites/capabilities em arrays/objetos | frontend e validações de plano | Alto | 2 |
| `organizations`/`companies` | `id`, `name`, `publicName`, `legalName`, `slug`, `planId`, `subscription`, `limitsOverride`, `brand`, `settings`, `status` | usuários, pesquisas, respostas | `companies` é legado/compatível com `organizations` | regras por `companyId`; frontend usa ambos | Alto | 2 |
| `users` | `uid`, `email`, `name`, `role`, `companyId`, `department`, `status`, `preferences` | Firebase Auth uid; organizações | perfil separado do Auth; claims duplicam role/companyId | Firebase Auth obrigatório | Alto | 2 |
| `forms` | `id`, `companyId`, `title`, `questions[]`, dimensões implícitas, flags global | pesquisas e respostas | perguntas embutidas em array; opções variáveis | frontend renderiza e edita | Alto | 3 |
| `surveys` | `id`, `companyId`, `formId`, `title`, `status`, `tokenHash`, datas, LGPD, contadores | forms, responses, invitations | link/token e campanha no mesmo doc | frontend público e admin | Alto | 3 |
| `invitations` | `id`, `surveyId`, `companyId`, destinatário, status, datas | surveys/users | destinatário pode ser string ou objeto | comunicação e painel | Médio | 3 |
| `responses` | `id`, `surveyId`, `formId`, `companyId`, participante, respostas, scores, tokens | surveys/forms/users/certificates | answers/scores podem estar embutidos; resultado calculado no cliente | frontend participante/admin; gateway e-mail | Muito alto | 4 |
| `certificates` | `id`, `responseId`, código, arquivo/url, status | responses/users | pode estar embutido em response ou coleção | frontend baixa/visualiza | Alto | 5 |
| `communications` | `id`, `responseId`, canal, destinatário, status, tentativas, erro | responses/surveys/forms/orgs | gateway também mantém log JSONL | gateway grava e admin lê | Alto | 6 |
| `actionPlans` | `id`, `companyId`, `surveyId`, dimensão, responsável, status, comentários | responses/surveys/users | comentários em array; status livre | menus e relatórios | Médio | 7 |
| `notifications` | `id`, `userId`, `companyId`, tipo, severidade, read/dismissed | users/orgs | payload variável | frontend central de notificações | Médio | 7 |
| `knowledgeBase` | `id`, título, conteúdo, categoria | suporte/chatbot | conteúdo sem taxonomia relacional | chatbot/frontend | Baixo | 7 |
| `supportTickets`/`supportMessages` | tickets, mensagens, SLA, categoria | users/orgs | mensagens separadas, SLA em coleções auxiliares | atendimento | Médio | 7 |
| `units` | `id`, `companyId`, nome, status | organizações/users | opcional por plano | múltiplas unidades | Médio | 2 |
| `serviceDeliverables` | entregáveis contratados | planos/organizações | serviço consultivo mistura billing e operação | plano corporate/enterprise | Médio | 7 |

## Dependências críticas

- `firebase-init.js`: inicializa Auth, Firestore e Functions quando `STORAGE_MODE='firebase'`.
- `repository.js`: escolhe Firebase ou local, portanto é o ponto natural para introduzir `api`/`hybrid` sem quebrar o frontend.
- `firebase-repository.js`: lista coleções obrigatórias e sincroniza alterações do estado para Firestore.
- `communication-gateway`: lê `responses`, `surveys`, `forms`, `organizations` e grava `communications`.
- `firestore.rules`: hoje é a barreira de autorização para leitura/escrita direta do frontend.

## Dados duplicados e formatos instáveis

- `organizations` e `companies` representam a mesma entidade em camadas diferentes.
- `planId` pode existir em organização e dentro de `subscription.planId`.
- `forms.questions[]` mistura formulário e perguntas; deve virar `questions` e `question_options`.
- `responses` pode carregar answers, scores, participante, tokens e certificado no mesmo documento.
- `settings` pode ser documento `public` ou mapa agregado.
- `communications` pode existir no Firestore e em log JSONL do gateway.

## Riscos de migração

1. Perder vínculo entre Firebase Auth uid e `users.id`.
2. Alterar cálculo de resultado sem prova de equivalência.
3. Quebrar links públicos com `survey`/`token` existentes.
4. Duplicar envios de e-mail durante modo híbrido.
5. Migrar answers heterogêneas sem mapear perguntas corretamente.
6. Exportar PII sem política LGPD, retenção e mascaramento.

## Ordem recomendada

1. Criar API e schema sem alterar frontend.
2. Seed de planos e capabilities em PostgreSQL.
3. Exportar Firestore somente leitura e gerar relatório de shapes.
4. Migrar cadastros centrais e validar contagens.
5. Migrar formulários/pesquisas mantendo tokens públicos.
6. Migrar respostas com dupla validação de score.
7. Ativar certificados/e-mails backend-first para respostas novas.
8. Comparar Firebase x PostgreSQL por período.
9. Congelar Firestore após aceite e remover dependências gradualmente.
