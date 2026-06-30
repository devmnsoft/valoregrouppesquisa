const fs=require('fs'),path=require('path');
function fail(m){console.error(m);process.exit(1)}
const pkg=JSON.parse(fs.readFileSync('package.json','utf8')); const scripts=pkg.scripts||{};
const required=['security:no-secrets','scripts:required','functions:install','functions:lint','functions:node22-readiness','functions:deploy','hosting:build','hosting:deploy','hosting:dist-build','deploy:firebase','legacy:free-token-never-expires','legacy:plans-tab','legacy:public-submit-flow','functions:public-submit','legacy:result-email-send','legacy:certificate-complete','legacy:admin-mobile-functional','legacy:admin-mobile-runtime','legacy:diagnostics-complete','legacy:cache-busting','e2e:legacy-public-submit','e2e:legacy-result-email','e2e:legacy-certificate','e2e:legacy-plans','e2e:legacy-admin-mobile','prod:legacy-public-flow','prod:email-test'];
for(const r of required)if(!scripts[r])fail(`missing npm script ${r}`);
const refs=[];function walk(d){for(const e of fs.readdirSync(d,{withFileTypes:true})){if(['node_modules','.git','reports','backups','dist'].includes(e.name))continue;const p=path.join(d,e.name); if(e.isDirectory())walk(p); else if(/\.(bat|md)$/i.test(e.name))refs.push(p)}}walk(process.cwd());
const missing=[]; for(const f of refs){const s=fs.readFileSync(f,'utf8'); for(const m of s.matchAll(/npm run ([\w:.-]+)/g)){if(!scripts[m[1]])missing.push(`${path.relative(process.cwd(),f)} -> ${m[1]}`)}}
if(missing.length)fail('Referenced missing scripts:\n'+missing.join('\n'));
console.log('scripts:required OK');
