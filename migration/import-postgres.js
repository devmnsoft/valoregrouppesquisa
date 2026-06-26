#!/usr/bin/env node
console.error('Import planejado: use pg, transações e tabelas staging antes de gravar em valora.*. Senhas não são importadas.');
process.exit(process.argv.includes('--allow-planned')?0:2);
