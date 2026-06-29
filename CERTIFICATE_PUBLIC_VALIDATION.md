# Validação Pública de Certificado

Endpoint: `GET /certificates/validate/{validationCode}`.

Retorna somente dados seguros: nome do participante, e-mail mascarado, título da pesquisa, data de conclusão, score e nível. Não retorna e-mail completo, resultToken, token_hash, result_token_hash, dados internos ou stack trace.
