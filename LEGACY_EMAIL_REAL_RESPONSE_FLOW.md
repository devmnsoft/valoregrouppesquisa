# Fluxo legado de e-mail com responseId real

- O participante informa nome e e-mail no formulário público.
- A resposta é persistida com `id` real gerado por `uid('resp')` no legado ou pelo Firestore nas Functions.
- O resultado retorna `responseId` real e `resultToken` real.
- O envio usa `dispatchPostSurveyCommunication(response.id)` e chama `POST /communications/result/{responseId}/send-email`.
- Falha de e-mail não remove resposta, resultado ou certificado; o status fica registrado para reenvio.
- Literais de demonstração (`resp_demo`, `demo_response`, `response_demo`) são bloqueados por validador em código de produção.
