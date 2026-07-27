# Runbook de autenticação

Configure issuer, audience e chave JWT fora do Git. Em incidente, revogue sessões e famílias de refresh token, rotacione a chave conforme janela planejada e audite sem registrar tokens/hashes. Recuperação sempre responde de forma neutra e deve persistir apenas hash do token. O estado atual ainda não implementa refresh/sessões completos; não habilitar produção até testes de rotação, reuse, logout e revogação passarem.
