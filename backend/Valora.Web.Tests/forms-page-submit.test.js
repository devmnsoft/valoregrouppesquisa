'use strict';

const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const test = require('node:test');
const vm = require('node:vm');

function deferred() {
  let resolve;
  const promise = new Promise(done => { resolve = done; });
  return { promise, resolve };
}

function harness(confirmResult = true) {
  const confirmation = deferred();
  const listeners = {};
  const fields = new Map([
    ['name', 'Diagnóstico seguro'], ['description', 'Descrição preservada'],
    ['category', 'Cultura'], ['estimatedMinutes', '15']
  ]);
  const submit = { disabled: false, textContent: 'Criar formulário' };
  const createError = { textContent: '', classList: { add() {}, remove() {} } };
  const createForm = {
    addEventListener(type, callback) { listeners[type] = callback; },
    querySelector(selector) { return selector === '[data-create-error]' ? createError : submit; },
    querySelectorAll() { return []; }, reportValidity() { return true; }, reset() {}
  };
  const dialog = { querySelector: () => createForm, addEventListener() {}, showModal() {}, close() {} };
  const generic = { value: '', dataset: {}, textContent: '', innerHTML: '', classList: { add() {}, remove() {}, toggle() {} }, addEventListener() {}, querySelector: () => generic, querySelectorAll: () => [] };
  const host = Object.assign({}, generic, {
    querySelector(selector) {
      if (selector === '[data-items]' || selector === '[data-empty]' || selector === '[data-error]') return generic;
      return generic;
    }
  });
  const creates = [];
  const context = {
    console, URLSearchParams, setTimeout, clearTimeout,
    FormData: class { constructor(form) { assert.equal(form, createForm); } get(name) { return fields.get(name); } },
    document: {
      querySelector(selector) { return selector === '[data-page="forms-page"]' ? host : dialog; },
      querySelectorAll() { return []; }, createElement() { return { textContent: '', get innerHTML() { return this.textContent; } }; }
    },
    window: {
      ValoraUI: { confirm: () => confirmation.promise.then(() => confirmResult) },
      location: { search: '', assigned: null, assign(value) { this.assigned = value; } }
    },
    FormsApi: {
      list: async () => ({ items: [], total: 0, page: 1, pageSize: 20, totalPages: 0, hasPreviousPage: false, hasNextPage: false }), normalize: value => value, normalizeList: value => value,
      create: async request => { creates.push(request); return { id: 'form-1' }; }
    },
    sessionStorage: { getItem: () => null, setItem() {} }
  };
  vm.runInNewContext(fs.readFileSync(path.join(__dirname, '../Valora.Web/wwwroot/js/pages/forms-page.js'), 'utf8'), context);
  return { confirmation, listeners, submit, creates, context };
}

test('captures the form before asynchronous confirmation and blocks duplicate submits', async () => {
  const app = harness();
  const event = { currentTarget: {}, preventDefault() {} };
  const first = app.listeners.submit(event);
  event.currentTarget = null;
  const repeated = app.listeners.submit(event);
  assert.equal(app.submit.disabled, true);
  assert.equal(app.submit.textContent, 'Confirmando…');
  app.confirmation.resolve();
  await Promise.all([first, repeated]);
  assert.equal(app.creates.length, 1);
  assert.equal(app.creates[0].name, 'Diagnóstico seguro');
  assert.equal(app.context.window.location.assigned, '/Forms/form-1/Builder');
});

test('cancelling asynchronous confirmation unlocks submission without writing', async () => {
  const app = harness(false);
  const operation = app.listeners.submit({ currentTarget: null, preventDefault() {} });
  app.confirmation.resolve();
  await operation;
  assert.equal(app.creates.length, 0);
  assert.equal(app.submit.disabled, false);
  assert.equal(app.submit.textContent, 'Criar formulário');
});
