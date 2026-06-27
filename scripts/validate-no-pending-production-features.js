const {read,assert,pass}=require('./validate-sprint27-utils');
const forbidden=['será integrado ao e-mail transacional na próxima fase','StatusCode(501'];
for (const f of ['backend/Valora.Api/Controllers/AuthController.cs','backend/Valora.Api/Controllers/AdminController.cs','backend/Valora.Api/Controllers/PublicController.cs']) { const s=read(f); for(const x of forbidden) assert(!s.includes(x),`${f} contains pending marker ${x}`); }
pass('validate-no-pending-production-features');
