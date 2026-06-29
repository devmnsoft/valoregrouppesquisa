#!/usr/bin/env node
const fs=require('fs'); const program=fs.readFileSync('backend/Valora.Api/Program.cs','utf8');
for(const token of ['app.MapGet("/"','Valora API operacional.','swagger = "/swagger"','health = "/health"']){if(!program.includes(token)){console.error(`API root ausente: ${token}`);process.exit(1);}}
console.log('validate-api-root-and-swagger: PASS');
