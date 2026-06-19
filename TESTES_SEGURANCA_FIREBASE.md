# Testes de Segurança Firebase — Valora Pulse

Este documento define a matriz mínima de testes automatizados e manuais para validar isolamento multiempresa, permissões por papel, links públicos e funções críticas antes de produção.

## Como executar

### Pré-requisitos

- Node.js 20.
- Firebase CLI disponível via `npm install` ou `npx firebase`.
- Dependências instaladas na raiz do projeto: `npm install`.
- Dependências das Functions instaladas: `cd functions && npm install`.

### Comandos

- `npm run emulators:start`: inicia Auth, Firestore, Functions e Emulator UI para depuração manual.
- `npm run test:rules`: executa os testes automatizados de Firestore Rules no emulador.
- `npm run test:functions`: executa os testes automatizados de Cloud Functions públicas no emulador.
- `npm run test:security`: executa regras e functions em sequência.

## Matriz automatizada — Firestore Rules

Arquivo de referência: `tests/rules/firestore.rules.test.js`.

| Perfil | Cenário | Resultado esperado |
| --- | --- | --- |
| Admin Valora | Criar empresa em `organizations` | Permitido |
| Admin Valora | Criar usuário | Permitido |
| Admin Valora | Alterar plano/cobrança de empresa | Permitido |
| Admin Valora | Ler logs | Permitido |
| Admin Valora | Ler respostas de qualquer empresa | Permitido |
| Consultor Valora | Ler clientes, pesquisas e respostas | Permitido |
| Consultor Valora | Alterar plano | Negado |
| Consultor Valora | Alterar `settings/private` | Negado |
| Empresa Admin | Ler própria empresa | Permitido |
| Empresa Admin | Criar usuário da própria empresa | Permitido |
| Empresa Admin | Criar `admin_valora` | Negado |
| Empresa Admin | Trocar `companyId` de usuário | Negado |
| Empresa Admin | Alterar `role` para `admin_valora` | Negado |
| Empresa Admin | Ler empresa de terceiros | Negado |
| Empresa Admin | Ler logs globais | Negado |
| Gestor Pesquisa | Criar formulário/pesquisa da própria empresa | Permitido |
| Gestor Pesquisa | Alterar usuários críticos | Negado |
| Gestor Pesquisa | Acessar financeiro global/invoices | Negado |
| Participante | Ler próprio perfil | Permitido |
| Participante | Alterar `name`, `phone`, `preferences` | Permitido |
| Participante | Alterar `role` | Negado |
| Participante | Alterar `companyId` | Negado |
| Participante | Criar resposta direta em empresa alheia | Negado |
| Participante | Ler próprias respostas | Permitido |
| Participante | Ler respostas de terceiros | Negado |
| Não autenticado | Ler surveys diretamente | Negado |
| Não autenticado | Ler `settings/public` | Permitido |
| Não autenticado | Ler users, responses, invoices, logs | Negado |

## Matriz automatizada — Cloud Functions

Arquivo de referência: `tests/functions/public-functions.test.js`.

| Function | Cenário | Resultado esperado |
| --- | --- | --- |
| `validateSurveyLink` | Token válido | Retorna payload público da pesquisa |
| `validateSurveyLink` | Token inválido | Erro `permission-denied` |
| `validateSurveyLink` | Pesquisa expirada | Erro `failed-precondition` |
| `validateSurveyLink` | Pesquisa encerrada | Erro `failed-precondition` |
| `submitSurveyResponse` | Resposta válida | Cria response e retorna `resultToken` |
| `submitSurveyResponse` | `companyId` adulterado no payload | Response fica com `companyId` da pesquisa, não do cliente |
| `submitSurveyResponse` | Pergunta obrigatória sem resposta | Erro `invalid-argument` |
| `submitSurveyResponse` | Duplicado com `allowRepeat=false` | Erro `already-exists` |
| `getPublicResult` | Token válido | Retorna resultado público |
| `getPublicResult` | Token inválido | Erro `permission-denied` |

## Evidências esperadas

Antes de promover para produção, anexar ao registro de release:

1. Saída completa de `npm run test:security`.
2. Capturas do Emulator UI mostrando dados isolados por empresa durante depuração, se aplicável.
3. Resultado da checklist manual em `TESTES_EXECUTADOS.md`.
4. Confirmação de publicação das rules e functions no projeto Firebase correto.

## Observações de segurança

- Respostas públicas não devem ser criadas diretamente pelo frontend no Firestore; o caminho aceito é `submitSurveyResponse`.
- Leitura pública direta de surveys deve permanecer bloqueada; o caminho aceito é `validateSurveyLink`.
- Campos sensíveis de usuário (`role`, `companyId`, `status`, `permissions`, `planId`) não devem ser editáveis pelo próprio usuário.
- Qualquer nova collection com dados multiempresa deve ter teste explícito de acesso cruzado entre empresas.
