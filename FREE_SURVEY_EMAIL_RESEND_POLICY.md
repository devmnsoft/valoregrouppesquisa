# Política de reenvio de e-mail

O reenvio manual é limitado a 3 reenvios por resposta em janela de 24 horas. `admin_valora` pode forçar reenvio com justificativa. `empresa_admin` opera apenas no tenant da própria empresa. Todo reenvio cria novo `email_job` `pending` com `template_key='result-ready-resend'` e auditoria `free_survey.result_email_resent`.
