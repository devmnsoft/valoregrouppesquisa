# Modelo de autorização multi-tenant

A organização é o tenant contratante. Grupo econômico, pessoa jurídica, unidade e setor pertencem ao tenant e toda consulta deve receber o identificador derivado do usuário autenticado, nunca confiar em `organizationId` do cliente. Policies resolvem role, permission, override e escopo; services repetem a validação. Capabilities e limites complementam autorização, mas não concedem permissão. A matriz completa ainda precisa ser implementada e testada contra IDOR.
