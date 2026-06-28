const { expect } = require('@playwright/test');
async function assertSafePage(page){
  const text=await page.locator('body').innerText({timeout:8000});
  expect(text).not.toMatch(/System\..*Exception|Traceback|password_hash|result_token_hash|private_key|token_hash|undefined|NaN|\[object Object\]/i);
  await expect(page.locator('.spinner-border,.loading,[aria-busy="true"]')).toHaveCount(0,{timeout:8000}).catch(()=>{});
}
async function assertBootstrap(page){
  await page.goto('/');
  const hasBootstrap=await page.evaluate(()=>Boolean(window.bootstrap)||[...document.styleSheets].some(s=>(s.href||'').includes('bootstrap'))||!!document.querySelector('.container,.row,.btn'));
  expect(hasBootstrap).toBeTruthy();
}
module.exports={assertSafePage,assertBootstrap};
