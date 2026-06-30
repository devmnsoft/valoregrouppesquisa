# Fluxo seguro de criação de usuário Firebase Auth

Usuários corporativos são criados por `createCompanyUser` em Cloud Functions/Admin SDK. A função valida permissões, cria/recupera Auth user, grava `users/{uid}`, define custom claims `role/companyId`, gera link de reset e audita a operação. O front não deve gravar usuário novo com ID aleatório sem Auth.
