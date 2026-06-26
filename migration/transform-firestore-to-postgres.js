#!/usr/bin/env node
const fs=require('fs');
const input=process.argv[2]||'migration/firestore-export.json';const output=process.argv[3]||'migration/postgres-import.json';
const src=JSON.parse(fs.readFileSync(input,'utf8'));const c=src.collections||{};
const out={generatedAt:new Date().toISOString(),sourceProject:src.projectId,plans:c.plans||[],organizations:[...(c.organizations||[]),...(c.companies||[])],users:(c.users||[]).map(u=>({...u,passwordHash:null,authProvider:u.uid?'firebase':'unknown'})),forms:c.forms||[],surveys:c.surveys||[],responses:c.responses||[],certificates:c.certificates||[],communications:c.communications||[]};
fs.writeFileSync(output,JSON.stringify(out,null,2));console.log(`transformed ${output}`);
