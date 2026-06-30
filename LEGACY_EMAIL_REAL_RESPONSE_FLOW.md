# Fluxo legado de e-mail com responseId real

Após salvar resposta, o legado usa `responseId` e `resultToken` reais e chama `POST /communications/result/{responseId}/send-email`. Falhas de e-mail não removem resultado/certificado e geram comunicação para reenvio.
