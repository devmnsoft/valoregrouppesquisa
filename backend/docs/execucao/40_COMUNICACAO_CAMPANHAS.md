# Comunicação e campanhas

## Implementado nesta execução

- A criação de campanha aceita os canais `manual`, `whatsapp_manual` e `email`.
- Campanhas de coleta somente são criadas para diagnósticos publicados/ativos.
- O canal de e-mail é recusado com mensagem operacional explícita quando a configuração SMTP não é válida; nenhum envio é simulado.
- Nome, assunto, corpo, canal e usuário criador passam a compor o registro canônico da campanha.
- Destinatários continuam armazenados somente por hash e identificação mascarada.
- A rota plural `/api/v1/diagnostics/{id}/campaigns` é um alias compatível da rota usada pelo BFF.
- O formulário do Workspace permite escolher o canal e informa o requisito de SMTP.

## Limites reais

O processamento de destinatários por e-mail continua indisponível porque o cadastro não guarda endereço reversível. O compartilhamento manual do link e da mensagem permanece o fluxo seguro disponível.
