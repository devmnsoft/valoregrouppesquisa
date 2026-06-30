# Reparo de perfil de usuário

`repairUserProfile` consulta Firebase Auth por UID, cria ou atualiza `users/{uid}`, preenche role, companyId e status, define custom claims e registra auditoria. Mensagens de login para perfil ausente/inativo são amigáveis e evitam loop silencioso.
