const { test } = require('@playwright/test');
test('produção mobile final é opt-in', async () => { test.skip(process.env.ALLOW_PRODUCTION_MOBILE_SMOKE !== 'true', 'Defina ALLOW_PRODUCTION_MOBILE_SMOKE=true para rodar contra produção.'); });
