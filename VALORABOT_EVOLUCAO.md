# ValoraBot — Evolução consultiva

## Objetivo
Evoluir o ValoraBot para ser um assistente contextual do Valora Pulse™, com respostas naturais, consultivas e úteis para visitantes, participantes, empresas e administração Valora, sem depender de API externa de IA no frontend.

## Perfis atendidos
- Visitante público e usuário não logado.
- Participante respondendo pesquisa ou vendo resultado.
- Empresa Admin e Gestor de Pesquisa.
- Admin Valora e Consultor Valora.
- Perfis de análise/gestão que consultam relatórios, respostas e planos de ação.

## Intenções implementadas
- `plans_pricing` e `plan_recommendation`.
- `diagnosis` para Valora Insight™, maturidade, dimensões e pontuação 125.
- `result_interpretation` para devolutiva, nível, dimensões e próximos passos.
- `certificate_report` para relatório e certificado.
- `survey_help` para responder pesquisa, link expirado e convite/e-mail.
- `access_help` para login, senha e conta.
- `admin_company` para portal da empresa/administração.
- `lgpd_security` para LGPD, privacidade e dados sensíveis.
- `support_human` para atendimento humano.
- `fallback` inteligente com caminhos de ação.

## Base de conhecimento
A base editorial local fica em `chatbot-knowledge-base.js` e cobre categorias:
- `primeiros_passos`
- `planos_precos`
- `diagnostico_valora_insight`
- `resultado_devolutiva`
- `certificado_relatorio`
- `pesquisa_publica`
- `portal_empresa`
- `portal_participante`
- `suporte`
- `privacidade_lgpd`

O bot também consulta `state.knowledgeBase` e considera apenas artigos publicados.

## Fluxo de recomendação de plano
A função `recommendPlanFromAnswers(answers)` considera volume aproximado, recorrência, número de unidades/áreas e necessidade de relatório executivo/plano de ação. A recomendação pode retornar:
- Grátis
- Essencial
- Profissional
- Corporativo
- Enterprise

## Fluxo de resultado
Quando há `currentResponse` com dados de resultado, o bot usa pontuação, nível de maturidade, dimensões e próximos passos. Quando não há dados completos, explica a leitura de forma segura sem inventar informações.

## Fluxo de suporte humano
- Botão fixo: **Falar com atendente**.
- Usuário logado: cria ticket em `supportTickets` com perfil, empresa e rota.
- Usuário público: direciona ao WhatsApp padrão `+55 91 99254-5353` com mensagem pré-preenchida.
- O bot evita solicitar senhas, tokens, CPF ou documentos completos.

## Limitações no Firebase Spark
- O bot usa motor local por regras, intenções, base de conhecimento e templates.
- `ENABLE_AI_CHATBOT` permanece `false`.
- Cloud Functions não são obrigatórias.
- Se IA futura for ativada, deve passar por backend seguro; nunca chave de IA no frontend.

## Como validar
Execute:

```bash
node scripts/validate-chatbot.js
```

Em produção/homologação, execute:

```bash
node scripts/healthcheck-prd.js --url https://valoragroup.mnsoft.com.br --project gestordepesquisa
```

## Como evoluir para IA futura
1. Manter o motor local como fallback obrigatório.
2. Criar endpoint seguro em backend/Cloud Functions ou outro serviço server-side.
3. Enviar somente contexto mínimo e sanitizado.
4. Registrar consentimento, finalidade e logs sem dados sensíveis.
5. Usar a base de conhecimento publicada como fonte controlada.
