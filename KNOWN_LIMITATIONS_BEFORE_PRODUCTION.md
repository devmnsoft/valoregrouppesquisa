# Known Limitations Before Production

- Não há promessa de zero bug.
- E2E live exige API e PostgreSQL vivos, migrations aplicadas e fixture de pesquisa pública.
- Smoke tests de navegador validam segurança visual básica, mas não substituem bug bash manual completo.
- Cutover real depende de aprovação e export Firestore confirmado.
