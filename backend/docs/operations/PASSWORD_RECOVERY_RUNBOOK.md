# Runbook de recuperação de senha

Responder sempre de forma neutra. Gerar token criptográfico, persistir somente o hash e enfileirar o link completo para o destinatário solicitado. Nunca registrar token ou link em logs. Consumir uma única vez e revogar sessões após troca. Em incidente, pausar o worker, revogar tokens pendentes e auditar eventos por correlação. A implementação integral ainda precisa ser concluída e homologada.
