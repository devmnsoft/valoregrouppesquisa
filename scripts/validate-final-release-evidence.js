#!/usr/bin/env node
const {has}=require('./sprint48-validator-lib');
has('FINAL_RELEASE_EVIDENCE.md',['commit','data/hora','ambiente','gates executados','gates aprovados','gates falhos','riscos residuais','decisão']);
console.log('validate-final-release-evidence: PASS');
