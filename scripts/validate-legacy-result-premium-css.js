#!/usr/bin/env node
const fs=require('fs');const css=fs.readFileSync('style.css','utf8');function fail(m){console.error('FAIL:',m);process.exit(1)}
for(const x of ['.result-kicker-premium','.result-hero-grid','.result-score-panel-premium','.result-card-premium','.result-actions-card','.certificate-preview-card-premium','.result-dimension-grid-premium','.result-dimension-card-premium']) if(!css.includes(x)) fail('classe CSS premium ausente: '+x);
if(!/\.result-hero-premium \*[\s\S]*color:\s*inherit/.test(css))fail('neutralização scoped do hero premium ausente');
console.log('legacy premium result css: PASS');
