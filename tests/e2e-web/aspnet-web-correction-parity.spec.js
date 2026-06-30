const { test, expect } = require('@playwright/test');
const fs = require('fs'); const path = require('path');
function walk(d){ return fs.existsSync(d) ? fs.readdirSync(d,{withFileTypes:true}).flatMap(e => e.isDirectory()?walk(path.join(d,e.name)):[path.join(d,e.name)]) : []; }
test('Valora.Web retains ASP.NET/jQuery/mobile parity markers', async () => {
  const all = walk('backend/Valora.Web').filter(f => /\.(cshtml|js|css|cs)$/.test(f)).map(f => fs.readFileSync(f,'utf8')).join('\n');
  expect(all).toMatch(/publicToken/i);
  expect(all).toMatch(/mobile/i);
});
