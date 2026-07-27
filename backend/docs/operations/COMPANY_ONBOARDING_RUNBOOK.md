# Runbook de onboarding empresarial

O fluxo alvo valida CNPJ, aceites, e-mail e senha antes de abrir uma única transação. A transação deve criar organização, pessoa jurídica/endereço, unidade principal, admin/role, assinatura free, módulos, settings, branding, onboarding, consentimentos, uso, auditoria e outbox. Qualquer erro exige rollback. O fluxo atual ainda não é atômico e não deve ser considerado homologado.
