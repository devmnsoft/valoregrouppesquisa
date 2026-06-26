#!/usr/bin/env node
'use strict';
const {parseArgs,reportForData,loadFirestore,writeAudit}=require('./data-shape-common');
(async()=>{const args=parseArgs();let data={};if(args.file){data=JSON.parse(require('fs').readFileSync(args.file,'utf8')).data||JSON.parse(require('fs').readFileSync(args.file,'utf8'));}else if(args.project){({data}=await loadFirestore(args.project));}const report=reportForData(data);writeAudit(report);console.log(JSON.stringify({status:report.errors.length?'WARN':'PASS',errors:report.errors,report:'reports/data-shape-valorapesquisa.json'},null,2));process.exit(0);})().catch(e=>{console.error(e);process.exit(1);});
