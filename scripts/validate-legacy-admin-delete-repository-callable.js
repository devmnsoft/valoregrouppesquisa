const fs=require('fs');const repo=fs.readFileSync('firebase-repository.js','utf8'),facade=fs.readFileSync('repository.js','utf8');
for(const x of ["callFunction('adminDeleteResponse'",'async function adminDeleteResponse','adminDeleteResponse,'])if(!repo.includes(x))throw new Error('firebase-repository sem '+x);
for(const x of ['provider?.adminDeleteResponse','provider?.deleteResponse','delete_response_unavailable','adminDeleteResponse:(responseId)=>adminDeleteResponse(responseId)'])if(!facade.includes(x))throw new Error('repository sem '+x);
console.log('validate-legacy-admin-delete-repository-callable: PASS');
