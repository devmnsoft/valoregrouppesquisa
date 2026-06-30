const fs=require('fs'),cp=require('child_process');
function fail(m){console.error(m);process.exit(1)}
const firebase=JSON.parse(fs.readFileSync('firebase.json','utf8'));
if(firebase.hosting?.public!=='dist')fail('hosting.public must be dist');
cp.execSync('npm run build:prod',{stdio:'inherit'});
for(const f of ['dist/index.html','dist/config.js']) if(!fs.existsSync(f))fail(`${f} missing`);
if(!fs.existsSync('dist/assets'))fail('dist/assets missing');
const html=fs.readFileSync('dist/index.html','utf8');
if(!/\.js(\?v=|"|')/.test(html)||!/\.css(\?v=|"|')/.test(html))fail('dist/index.html must reference JS and CSS');
const build=fs.readFileSync('scripts/build-production.js','utf8');
if(!/legacy-admin-mobile-menu-bridge\.js/.test(build)&&!/legacy-admin-mobile-menu-bridge\.js/.test(html))fail('legacy bridge not included');
console.log('hosting:dist-build OK');
