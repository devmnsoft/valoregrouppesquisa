const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const source = fs.readFileSync('backend/Valora.Web/wwwroot/js/responsible-selector.js', 'utf8');
const flush = () => new Promise(resolve => setTimeout(resolve, 0));

class Control {
  constructor(value = '') { this.value = value; this.dataset = {}; this.hidden = false; this.disabled = false; this.listeners = {}; this.options = []; this.textContent = ''; }
  addEventListener(name, callback) { this.listeners[name] = callback; }
  fire(name, extra = {}) { this.listeners[name]?.({ key: '', preventDefault() {}, ...extra }); }
  focus() {}
  replaceChildren(...items) { this.options = items; this.value = items.find(x => x.selected)?.value || ''; }
  add(option) { this.options.push(option); }
  get selectedOptions() { return this.options.filter(x => x.value === this.value); }
}
function component(selected = '') {
  const controls = { search: new Control(), select: new Control(), status: new Control(), more: new Control() };
  controls.select.dataset.emptyLabel = 'Todos';
  return { dataset: { selectedResponsible: selected, responsibleEndpoint: '/options' }, controls,
    querySelector(selector) { return controls[{ '[data-responsible-search]': 'search', '[data-responsible-select]': 'select', '[data-responsible-status]': 'status', '[data-responsible-more]': 'more' }[selector]]; } };
}
function boot(roots, fetch) {
  const context = { document: { querySelectorAll: () => roots }, fetch, Option: class { constructor(text, value) { this.text = text; this.value = value; } },
    URLSearchParams, AbortController, setTimeout, clearTimeout, location: { pathname: '/ActionCenter/Items', search: '', assign() {} } };
  vm.runInNewContext(source, context); return context;
}
const response = data => ({ ok: true, status: 200, json: async () => data });

test('preserva a seleção atual durante pesquisa e restauração server-side', async () => {
  const root = component('user-25');
  let call = 0;
  boot([root], async () => response(call++ ? { items: [{ id: 'user-01', title: 'Ana' }], total: 1, hasMore: false } : { items: [{ id: 'user-25', title: 'Zoe' }], total: 25, hasMore: true }));
  await flush(); await flush();
  assert.equal(root.controls.select.value, 'user-25');
  root.controls.search.value = 'Ana'; root.controls.search.fire('input');
  await new Promise(resolve => setTimeout(resolve, 310)); await flush();
  assert.equal(root.controls.select.value, 'user-25');
  assert.equal(root.controls.select.options.find(x => x.value === 'user-25').text, 'Zoe');
  assert.match(root.controls.status.textContent, /^1 de 1/);
});

test('repete exatamente a página posterior que falhou', async () => {
  const root = component(); let calls = 0; const pages = [];
  boot([root], async url => { const page = new URL('http://local' + url).searchParams.get('page'); pages.push(page); calls++; if (calls === 2) throw new Error('network'); return response({ items: [{ id: `u${calls}`, title: `Pessoa ${calls}` }], total: 40, hasMore: calls < 3 }); });
  await flush(); await flush(); root.controls.more.fire('click'); await flush(); await flush();
  assert.match(root.controls.status.textContent, /carregar mais/);
  root.controls.more.fire('click'); await flush(); await flush();
  assert.deepEqual(pages, ['1', '2', '2']);
});

test('componentes mantêm estado e requisições independentes', async () => {
  const first = component('a'); const second = component('b');
  boot([first, second], async url => { const include = new URL('http://local' + url).searchParams.get('includeId'); return response({ items: [{ id: include, title: include.toUpperCase() }], total: 1, hasMore: false }); });
  await flush(); await flush();
  assert.equal(first.controls.select.value, 'a'); assert.equal(second.controls.select.value, 'b');
});
