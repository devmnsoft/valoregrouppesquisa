const { test, expect } = require('@playwright/test');

const roles = [
  ['admin_valora','admin/dashboard'],
  ['consultor_valora','admin/dashboard'],
  ['empresa_admin','empresa/dashboard'],
  ['gestor_pesquisa','empresa/dashboard'],
  ['analista_resultados','empresa/dashboard'],
  ['gestor_area','empresa/dashboard'],
  ['participante','participante/dashboard'],
  ['convidado_externo','participante/dashboard'],
];

test.describe('mobile auth routing regression', () => {
  for (const [role, target] of roles) {
    test(`${role} leaves #login and opens ${target}`, async ({ page }) => {
          await page.goto('about:blank');
          await page.setContent(`<!doctype html><button id="app"></button><script>
            window.currentRoute='login'; window.state={}; window.__valoraLoginInProgress=false;
            window.location.hash='login';
            window.getRoleDefinition=(role)=>({admin_valora:{scope:'valora'},consultor_valora:{scope:'valora'},empresa_admin:{scope:'empresa'},gestor_pesquisa:{scope:'empresa'},analista_resultados:{scope:'empresa'},gestor_area:{scope:'empresa'},participante:{scope:'participante'},convidado_externo:{scope:'externo'}}[role]||{scope:'unknown'});
            window.currentUserSafe=()=>({uid:'uid-${role}',role:'${role}',status:'active'});
            window.route=(path)=>{window.currentRoute=path;document.body.dataset.route=path;};
            ${/const PUBLIC_ROUTE_HASHES[\s\S]*?function isAdminRoute/.exec(require('fs').readFileSync('app.js','utf8'))[0].replace('function isAdminRoute','function __stop')}
            setTimeout(()=>navigateAfterLogin(currentUserSafe()),80);
          </script>`);
          await expect.poll(() => page.evaluate(() => location.hash)).not.toBe('#login');
          await expect.poll(() => page.evaluate(() => document.body.dataset.route)).toBe(target);
    });
  }
});
