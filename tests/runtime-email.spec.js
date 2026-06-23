const {test,expect}=require('@playwright/test');
test('PRD Spark não chama endpoints locais nem Cloud Functions de e-mail/log',async({page})=>{
  const calls=[];page.on('request',r=>calls.push(r.url()));const errors=[];page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{const t=m.text();if(/Unexpected token '<'|CORS policy|system\.legacy_run|\[Valora debug\]|\[Valora info\]/i.test(t))errors.push(t);});
  await page.goto('file://'+process.cwd()+'/index.html',{waitUntil:'domcontentloaded'}).catch(()=>{});
  await page.waitForTimeout(500);
  expect(calls.some(u=>/\/api\/email\/|\/api\/outbox|cloudfunctions\.net\/(getEmailStatus|logServerEvent)/.test(u))).toBeFalsy();
  expect(errors).toEqual([]);
});
