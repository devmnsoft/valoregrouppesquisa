const fs=require('fs'); const sql=fs.readFileSync('scriptbd_completo.sql','utf8');
const checks=['ON CONFLICT','CREATE INDEX IF NOT EXISTS','CREATE TRIGGER','schema_migrations','public_token_hash','validation_code'];
for (const c of checks) if(!sql.includes(c)) throw new Error(`Bootstrap incompleto: ${c}`);
console.log('Bootstrap PostgreSQL local validado estaticamente. Execute: psql -U postgres -d postgres -f scriptbd_completo.sql');
