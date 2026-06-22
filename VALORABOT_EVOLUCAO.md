# ValoraBot — Evolução consultiva

## Intenções cobertas
O ValoraBot cobre planos/preços, recomendação de plano, Valora Insight™, pontuação 125, cinco dimensões, devolutiva, certificado/relatório, pesquisa pública, link expirado, acesso/login, portais, administração, LGPD, contratação e suporte humano.

## Contexto
`getValoraBotContext()` retorna estado de login, perfil, empresa, rota, pesquisa/resposta atual, plano, planos, formulários, pesquisas, respostas, base de conhecimento e modo Firebase.

## Fluxo de recomendação de plano
`recommendPlanFromAnswers(answers)` usa volume de respondentes, recorrência, unidades, relatório executivo, plano de ação e consultoria para indicar Grátis, Essencial, Profissional, Corporativo ou Enterprise.

## Suporte humano
Todas as respostas oferecem ação de atendimento. Usuários logados podem gerar ticket local em `supportTickets`; visitantes podem seguir para WhatsApp com a mensagem padrão.

## LGPD e segurança
O bot orienta a não enviar senhas, tokens ou documentos completos e evita depender de IA externa no frontend.

## Como testar
Execute `node scripts/validate-chatbot.js` e teste manualmente perguntas como “qual plano é melhor para 500 respostas?” e “como interpreto meu resultado?”.
