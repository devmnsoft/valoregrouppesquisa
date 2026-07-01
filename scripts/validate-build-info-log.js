const fs=require('fs');const app=fs.readFileSync('app.js','utf8');const fail=[];
if(/console\.info\('\[Valora Pulse\] build',\s*window\.ValoraBuildInfo\|\|\{/.test(app))fail.push('build ainda pode logar Object genérico.');
if(!/const\s+buildInfo=\{version:window\.ValoraBuildInfo\?\.version\|\|APP_CONFIG\.APP_VERSION,hash:window\.ValoraBuildInfo\?\.hash\|\|'unknown',builtAt:window\.ValoraBuildInfo\?\.builtAt\|\|'',appFile:window\.ValoraBuildInfo\?\.appFile\|\|''\}/.test(app))fail.push('buildInfo não contém version/hash/builtAt/appFile explícitos.');
if(!/console\.info\('\[Valora Pulse\] build',\s*buildInfo\)/.test(app))fail.push('console.info não usa buildInfo explícito.');
if(fail.length){console.error(fail.join('\n'));process.exit(1)}console.log('validate-build-info-log: PASS');
