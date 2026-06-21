# Checklist de Homologação Final — Valora Pulse

**Versão:** Valora Group™ 8.6.4 RC1
**Data de abertura:** 2026-06-21
**Objetivo:** validar se o produto está pronto para demonstração comercial, homologação com cliente, deploy controlado e preparação de produção.

Use os status: `Não testado`, `Aprovado`, `Reprovado`, `Bloqueado`, `Não aplicável`.

## Inicialização

| Item | Status | Evidência | Observações |
|---|---|---|---|
| Sistema abre sem erro | Não testado |  |  |
| Não fica em “Carregando o sistema” | Não testado |  |  |
| `localStorage` vazio funciona | Não testado |  |  |
| `localStorage` corrompido recria base | Não testado |  |  |
| Botão “Recriar base local” funciona | Não testado |  |  |
| Sem erro crítico no console | Não testado |  |  |

## Login

| Perfil | Usuário/Cenário | Status | Evidência | Observações |
|---|---|---|---|---|
| `admin_valora` | Login e dashboard global | Não testado |  |  |
| `consultor_valora` | Login e escopo permitido | Não testado |  |  |
| `empresa_admin` | Login e portal da empresa | Não testado |  |  |
| `gestor_pesquisa` | Login e gestão de pesquisas | Não testado |  |  |
| `analista_resultados` | Login e leitura analítica | Não testado |  |  |
| `gestor_area` | Login e escopo de área | Não testado |  |  |
| `participante` | Login/portal participante | Não testado |  |  |
| `convidado_externo` | Acesso restrito esperado | Não testado |  |  |

## Segurança

| Item | Status | Evidência | Observações |
|---|---|---|---|
| Usuário não altera próprio perfil | Não testado |  |  |
| Usuário não altera `companyId` | Não testado |  |  |
| Empresa não vê outra empresa | Não testado |  |  |
| Participante não acessa admin | Não testado |  |  |
| Convidado não acessa portal administrativo | Não testado |  |  |
| Firestore Rules protegem dados | Não testado |  |  |
| Functions validam payload | Não testado |  |  |

## Fluxo comercial

| Item | Status | Evidência | Observações |
|---|---|---|---|
| Criar empresa | Não testado |  |  |
| Definir plano | Não testado |  |  |
| Criar administrador da empresa | Não testado |  |  |
| Configurar marca | Não testado |  |  |
| Configurar assinatura | Não testado |  |  |
| Visualizar limites | Não testado |  |  |
| Testar bloqueios comerciais | Não testado |  |  |

## Funcionários

| Item | Status | Evidência | Observações |
|---|---|---|---|
| Cadastrar funcionário | Não testado |  |  |
| Editar funcionário | Não testado |  |  |
| Escolher perfil | Não testado |  |  |
| Bloquear perfil global | Não testado |  |  |
| Importar funcionários | Não testado |  |  |
| Filtrar por departamento | Não testado |  |  |

## Questionários

| Item | Status | Evidência | Observações |
|---|---|---|---|
| Criar questionário | Não testado |  |  |
| Criar perguntas | Não testado |  |  |
| Configurar pesos | Não testado |  |  |
| Configurar dimensões | Não testado |  |  |
| Configurar faixas | Não testado |  |  |
| Validar perguntas obrigatórias | Não testado |  |  |
| Clonar questionário | Não testado |  |  |

## Pesquisas

| Item | Status | Evidência | Observações |
|---|---|---|---|
| Criar pesquisa | Não testado |  |  |
| Gerar link | Não testado |  |  |
| Renovar token | Não testado |  |  |
| Configurar validade | Não testado |  |  |
| Encerrar pesquisa | Não testado |  |  |
| Enviar convites | Não testado |  |  |

## Participação

| Item | Status | Evidência | Observações |
|---|---|---|---|
| Abrir link | Não testado |  |  |
| Aceitar LGPD | Não testado |  |  |
| Responder | Não testado |  |  |
| Bloquear duplicidade quando aplicável | Não testado |  |  |
| Mostrar resultado | Não testado |  |  |
| Emitir certificado | Não testado |  |  |

## Respostas e cálculo

| Item | Status | Evidência | Observações |
|---|---|---|---|
| Resposta aparece na área de respostas | Não testado |  |  |
| Cálculo bate | Não testado |  |  |
| Dimensões batem | Não testado |  |  |
| Dashboard bate | Não testado |  |  |
| Relatório bate | Não testado |  |  |

## Dashboards

| Dashboard | Status | Evidência | Observações |
|---|---|---|---|
| Admin global | Não testado |  |  |
| Empresa | Não testado |  |  |
| Gestor pesquisa | Não testado |  |  |
| Analista resultados | Não testado |  |  |
| Gestor área | Não testado |  |  |
| Participante | Não testado |  |  |

## Relatórios

| Relatório/Exportação | Status | Evidência | Observações |
|---|---|---|---|
| PDF | Não testado |  |  |
| Word | Não testado |  |  |
| Excel | Não testado |  |  |
| CSV | Não testado |  |  |
| JSON | Não testado |  |  |
| Relatório executivo | Não testado |  |  |
| Relatório individual | Não testado |  |  |
| Relatório por pesquisa | Não testado |  |  |
| Relatório por dimensão | Não testado |  |  |

## Plano de ação

| Item | Status | Evidência | Observações |
|---|---|---|---|
| Gerar ação automática | Não testado |  |  |
| Criar ação manual | Não testado |  |  |
| Editar responsável | Não testado |  |  |
| Alterar status | Não testado |  |  |
| Concluir ação | Não testado |  |  |
| Ver ações vencidas | Não testado |  |  |

## Notificações

| Item | Status | Evidência | Observações |
|---|---|---|---|
| Pesquisa vencendo | Não testado |  |  |
| Convite pendente | Não testado |  |  |
| Nova resposta | Não testado |  |  |
| Ação vencida | Não testado |  |  |
| Limite de plano | Não testado |  |  |
| Alerta financeiro | Não testado |  |  |

## Logs e Telegram

| Item | Status | Evidência | Observações |
|---|---|---|---|
| Gerar log teste | Não testado |  |  |
| Gerar erro simulado | Não testado |  |  |
| Enviar teste Telegram | Não testado |  |  |
| Validar mascaramento | Não testado |  |  |
| Validar que token não está no frontend | Não testado |  |  |
| Validar anti-spam | Não testado |  |  |

## Integrações

| Item | Status | Evidência | Observações |
|---|---|---|---|
| API key | Não testado |  |  |
| Webhook | Não testado |  |  |
| Importação | Não testado |  |  |
| Exportação | Não testado |  |  |
| Logs de integração | Não testado |  |  |

## Financeiro

| Item | Status | Evidência | Observações |
|---|---|---|---|
| Criar fatura | Não testado |  |  |
| Marcar paga | Não testado |  |  |
| Marcar vencida | Não testado |  |  |
| Suspender empresa | Não testado |  |  |
| Testar bloqueio | Não testado |  |  |
| Visualizar fatura como empresa | Não testado |  |  |

## Mobile — 360px

| Tela/Fluxo | Status | Evidência | Observações |
|---|---|---|---|
| Login | Não testado |  |  |
| Menu | Não testado |  |  |
| Dashboard | Não testado |  |  |
| Modais | Não testado |  |  |
| Tabelas | Não testado |  |  |
| Formulários | Não testado |  |  |
| Pesquisa pública | Não testado |  |  |
| Relatórios básicos | Não testado |  |  |
