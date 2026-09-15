'use strict';
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const test = require('node:test');
const vm = require('node:vm');

function api() {
  const context = { window: {}, AjaxClient: {} };
  vm.runInNewContext(fs.readFileSync(path.join(__dirname, '../Valora.Web/wwwroot/js/api/forms-api.js'), 'utf8'), context);
  return context.window.FormsApi;
}

test('normalizes the camelCase paginated envelope serialized by ASP.NET', () => {
  const serialized = '{"items":[{"id":"b761b125-04aa-4415-a054-91b04280b48d","name":"Clima","status":"published","versionNumber":7,"questions":12}],"total":121,"page":2,"pageSize":10,"totalPages":13,"hasPreviousPage":true,"hasNextPage":true,"categories":["Pessoas"],"metrics":{"drafts":1}}';
  const result = api().normalizeList(JSON.parse(serialized));
  assert.equal(result.items[0].versionNumber, 7);
  assert.deepEqual({ total: result.total, page: result.page, pages: result.totalPages }, { total: 121, page: 2, pages: 13 });
  assert.equal(result.hasNextPage, true);
});

test('rejects arrays and malformed envelopes instead of silently returning an empty list', () => {
  assert.throws(() => api().normalizeList([]), /envelope paginado/);
  assert.throws(() => api().normalizeList({ items: [], total: '0', page: 1, pageSize: 10, totalPages: 0 }), /total/);
});
