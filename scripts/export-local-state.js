#!/usr/bin/env node
'use strict';
const {parseArgs,readJson,writeJson,stamp,normalizeExport}=require('./firebase-seed-utils');
const args=parseArgs();
if(!args.file){console.error('Uso: node scripts/export-local-state.js --file ./local-state.json [--out exports/valora-local-export.json] [--include-responses] [--mask-demo-documents]');process.exit(1);}
const raw=readJson(args.file);
const exported=normalizeExport(raw,{includeResponses:!!args['include-responses'],maskDemoDocuments:!!args['mask-demo-documents']});
const out=args.out||`exports/valora-local-export-${stamp()}.json`;
writeJson(out,exported);
console.log(`Exportação sanitizada gravada em ${out}`);
console.log(Object.fromEntries(Object.entries(exported.data).map(([k,v])=>[k,Array.isArray(v)?v.length:Object.keys(v||{}).length])));
