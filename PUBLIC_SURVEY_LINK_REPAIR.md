# Reparo de link público da pesquisa gratuita

A correção impede que `tokenHash` seja exposto como token público, prioriza `publicToken`, gera token seguro para a pesquisa gratuita ativa sem token e monta `index.html?survey={id}&token={token}&org={slug}`. Use `npm run home:repair-free-survey-link -- --dry-run` para validar e `-- --apply` para aplicar no Firestore com backup em `backups/`.
