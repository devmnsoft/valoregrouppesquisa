# Fluxo legado de e-mail de resultado

Após submissão pública, o legado salva a resposta, recebe `responseId` real e `resultToken`, renderiza o resultado, prepara o certificado e chama `sendResultEmailAuto`. O envio tenta API externa e depois Cloud Function `sendResultEmail`; se falhar, exibe mensagem segura e mantém a resposta para reenvio administrativo.
