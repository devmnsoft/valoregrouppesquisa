# Legacy Result Email Flow

Após submit público com sucesso, o front chama `sendResultEmailAuto` com `responseId`, `resultToken`, destinatário, nome e certificado. O envio tenta Cloud Function primeiro e API externa depois. Se ambos falharem, a conclusão da pesquisa não é bloqueada e a UI informa que o resultado foi registrado para reenvio administrativo.
