const fs=require('fs');const s=fs.readFileSync('functions/index.js','utf8');
function fail(m){throw new Error(m)}
const start=s.indexOf('exports.adminDeleteResponse=onCall');if(start<0)fail('adminDeleteResponse não usa onCall');
const end=s.indexOf('exports.',start+1);const fn=s.slice(start,end>start?end:s.length);
if(/exports\.adminDeleteResponse\s*=\s*(?:onRequest|functions\.https\.onRequest)/.test(s))fail('adminDeleteResponse usa onRequest');
for(const x of ['req.data','requireAdminUser(req)','assertUserCanAccessResponse','deleted:true','deletedAt:TS()','status:\'deleted\'','audit_logs','response_deleted','HttpsError'])if(!fn.includes(x))fail('adminDeleteResponse incompleta: '+x);
console.log('validate-functions-admin-delete-response-callable: PASS');
