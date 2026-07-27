# RBAC e escopos

A autorização deve combinar permission explícita e escopo de recurso. Roles apenas agregam permissions; `empresa_admin` não recebe acesso implícito. Toda operação empresarial exige `organization_id` do contexto autenticado e, conforme o recurso, valida grupo, pessoa jurídica, unidade e setor em `user_scopes`. Policies do ASP.NET protegem a borda, mas o serviço repete a validação antes do repository. A negação é o padrão, e consultas nunca aceitam um tenant fornecido pelo cliente como fonte de autoridade.
