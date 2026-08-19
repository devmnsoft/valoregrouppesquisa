# Bootstrap e login

O bootstrap deve ser explicitamente habilitado apenas em Development. `Demo:SeedEnabled`/`App:EnableDemoSeed` ficam desativados por padrão e produção bloqueia seed demo.

Após aplicar o SQL, confirme organização, assinatura ativa, planos Free/Professional/Enterprise, roles, permissões e associação do administrador. A senha de desenvolvimento deve usar hash bcrypt compatível com `AuthService`; nunca publique a senha ou o hash como credencial de produção.

O login recusa credencial inválida sem revelar a existência da conta, recusa usuário inativo com mensagem própria e informa configuração ausente quando faltam role ou organização. Sessões válidas carregam organização, role e plano e atualizam o último login/auditoria.
