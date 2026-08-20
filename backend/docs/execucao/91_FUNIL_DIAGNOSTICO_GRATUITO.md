# Funil do diagnóstico gratuito

Foi implementada a etapa de identificação e consentimento anterior ao questionário. LGPD é obrigatória e comunicação comercial é uma opção independente. E-mail, telefone, IP e user-agent são normalizados para hash/máscara antes da persistência; o navegador recebe somente os identificadores técnicos do lead e da sessão. O BFF público encaminha a criação à API e evita acesso direto do JavaScript ao host da API.

A execução das perguntas continua reutilizando o fluxo público de diagnóstico existente. A integração completa da sessão comercial com a resposta e o resultado oficial permanece pendente; nenhum resultado ou benchmark foi simulado.
