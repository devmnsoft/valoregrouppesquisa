# Fluxo de e-mail de resultado

1. Participante conclui a pesquisa.
2. O frontend salva a resposta e chama `dispatchPostSurveyCommunication(response.id)`.
3. O frontend envia ao gateway apenas `responseId`, `resultToken` e canais desejados.
4. O gateway valida origem, payload e resposta no Firestore.
5. O gateway recalcula/monta dados reais a partir do Firestore, envia SMTP e registra comunicação.
6. Falha de SMTP/gateway não quebra a conclusão da pesquisa; o frontend registra `failed` ou `pending-provider`.
7. Admin > Comunicações permite reenviar resultado, copiar link e ver resposta.

Para testar envio real, use `local-firebase`, configure SMTP no gateway local e conclua uma pesquisa com e-mail válido.
