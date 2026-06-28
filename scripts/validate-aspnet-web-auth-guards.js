const fs = require('fs');
const files = ['backend/Valora.Web/wwwroot/js/core/guards.js','backend/Valora.Web/wwwroot/js/core/auth-session.js','backend/Valora.Web/wwwroot/js/api/ajax-client.js'];
const all = files.map(f => fs.readFileSync(f, 'utf8')).join('\n');
const terms = ['Guards','requireAuth','Session.token','401','logout','sessionStorage'];
const miss = terms.filter(t => !all.includes(t));
if (miss.length) { console.error('missing guard terms', miss); process.exit(1); }
console.log('Auth guards OK.');
