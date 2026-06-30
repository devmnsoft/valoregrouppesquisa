const fs = require('fs');
function fail(m){ console.error('FAIL:', m); process.exitCode = 1; }
function ok(m){ console.log('OK:', m); }
const index = fs.readFileSync('index.html','utf8');
['legacy-admin-mobile-menu-bridge.js','app.js','style.css','config.js'].forEach(asset => index.includes(`${asset}?v=`) ? ok(`${asset} versionado`) : fail(`${asset} sem versão`));
const firebase = JSON.parse(fs.readFileSync('firebase.json','utf8'));
function cacheFor(source){ const item = firebase.hosting.headers.find(h => h.source === source || (source === 'index.html' && h.source.includes('html'))); return (item?.headers||[]).find(h => h.key.toLowerCase()==='cache-control')?.value || ''; }
if(!/no-store/i.test(cacheFor('index.html'))) fail('index.html sem no-store'); else ok('index.html no-store');
if(!/no-store/i.test(cacheFor('config.js'))) fail('config.js sem no-store'); else ok('config.js no-store');
const b = cacheFor('legacy-admin-mobile-menu-bridge.js'); if(!(/no-store/i.test(b)||/max-age=(0|[1-9][0-9]{0,2})\b/.test(b))) fail('bridge sem no-store/max-age baixo'); else ok('bridge cache seguro');
if(process.exitCode) process.exit(process.exitCode);
