const {read,assert,pass}=require('./validate-sprint27-utils');
for (const f of ['backend/Valora.Api/Controllers/PublicController.cs','backend/Valora.Api/Controllers/AdminController.cs']) { const s=read(f); assert(s.includes('[Obsolete('),`${f} missing Obsolete marker`); assert(s.includes('LegacyEnabled'),`${f} missing environment/config legacy guard`); assert(!s.includes('NotSupportedException'),`${f} calls NotSupportedException`); }
pass('validate-no-legacy-production-routes');
