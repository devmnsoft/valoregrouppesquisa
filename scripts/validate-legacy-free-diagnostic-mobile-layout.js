const { read, ok, done } = require('./validate-helper');
const css = read('style.css');
ok(/@media\s*\(max-width:\s*760px\)\s*\{[\s\S]*?\.free-diagnostic-hero\s*\{\s*padding:\s*42px 0/.test(css), 'Mobile precisa ter media query max-width 760px para a seção.');
ok(/@media\s*\(max-width:\s*760px\)\s*\{[\s\S]*?\.free-diagnostic-hero__inner\s*\{[\s\S]*?grid-template-columns:\s*1fr/.test(css), 'Mobile precisa empilhar em uma coluna.');
ok(/@media\s*\(max-width:\s*760px\)\s*\{[\s\S]*?\.free-diagnostic-hero__copy\s*\{[\s\S]*?text-align:\s*center/.test(css), 'Mobile precisa centralizar a copy somente no media query.');
ok(/@media\s*\(max-width:\s*760px\)\s*\{[\s\S]*?\.free-diagnostic-preview-card\s*\{[\s\S]*?width:\s*min\(100%,\s*340px\)/.test(css), 'Mobile precisa usar card compacto de 340px.');
ok(/@media\s*\(max-width:\s*760px\)\s*\{[\s\S]*?\.free-diagnostic-hero__actions\s*\{[\s\S]*?grid-template-columns:\s*1fr/.test(css), 'Mobile precisa deixar CTAs em coluna.');
done('validate-legacy-free-diagnostic-mobile-layout');
