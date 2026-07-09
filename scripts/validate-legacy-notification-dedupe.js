#!/usr/bin/env node
const fs=require('fs');const n=fs.readFileSync('notification-service.js','utf8');function fail(m){console.error('FAIL:',m);process.exit(1)}
for(const x of ['notificationDedupeKey','dedupeKey','dismissedBy','isOfficialValoraCompany','isFreeBeforeRealOnboarding']) if(!n.includes(x))fail('notification safeguard missing: '+x);
if(!/RUNTIME_ENV[\s\S]*production[\s\S]*merge\(state,\[\]\)/.test(n))fail('production must not generate frontend notifications every render');
console.log('legacy notification dedupe: PASS');
