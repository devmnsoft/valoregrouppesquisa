# Functions Node 22 Deploy

As Functions estão configuradas para Node.js 22 em dois pontos obrigatórios:

- `firebase.json`: `functions.runtime = nodejs22`
- `functions/package.json`: `engines.node = 22`

O gate de prontidão valida runtime, dependências Firebase/Admin/Nodemailer e o lint Windows-safe.

```bash
npm run functions:node22-readiness
npm run functions:deploy
```
