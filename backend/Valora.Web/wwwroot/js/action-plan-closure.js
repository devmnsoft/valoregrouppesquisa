(() => {
  'use strict';

  const storage = {
    get(key) { try { return sessionStorage.getItem(key); } catch { return null; } },
    set(key, value) { try { sessionStorage.setItem(key, value); return true; } catch { return false; } },
    remove(key) { try { sessionStorage.removeItem(key); } catch { /* Storage is optional. */ } }
  };

  document.querySelectorAll('[data-closure-clear-key]').forEach(marker => {
    storage.remove(marker.dataset.closureClearKey);
  });

  const page = document.querySelector('[data-closure-page]');
  const form = page?.querySelector('[data-closure-form]');
  if (!page || !form) return;

  const key = page.dataset.draftKey;
  const result = form.querySelector('textarea[name="Command.Result"]');
  const evidence = form.querySelector('textarea[name="Command.Evidence"]');
  const confirmation = form.querySelector('input[type="checkbox"][name="Command.Confirmed"]');
  const version = form.querySelector('input[name="Command.Version"]');
  const warning = form.querySelector('[data-draft-version-warning]');
  const fields = [result, evidence].filter(Boolean);
  let submitted = false;
  let safeNavigation = false;

  const hasText = () => fields.some(field => field.value.trim().length > 0);
  const save = () => {
    if (!key) return;
    storage.set(key, JSON.stringify({
      result: result?.value ?? '',
      evidence: evidence?.value ?? '',
      version: version?.value ?? ''
    }));
  };

  if (key && !hasText()) {
    const raw = storage.get(key);
    if (raw) {
      try {
        const draft = JSON.parse(raw);
        if (typeof draft.result === 'string' && result) result.value = draft.result;
        if (typeof draft.evidence === 'string' && evidence) evidence.value = draft.evidence;
        confirmation && (confirmation.checked = false);
        if (draft.version && draft.version !== version?.value && warning) warning.hidden = false;
      } catch {
        storage.remove(key);
      }
    }
  }

  fields.forEach(field => field.addEventListener('input', save));
  form.querySelector('[data-discard-draft]')?.addEventListener('click', () => {
    storage.remove(key);
    fields.forEach(field => { field.value = ''; });
    if (confirmation) confirmation.checked = false;
    if (warning) warning.hidden = true;
    result?.focus();
  });
  document.querySelectorAll('[data-safe-navigation], .action-filter-links a, .pagination a').forEach(link => {
    link.addEventListener('click', () => { safeNavigation = true; save(); });
  });
  form.addEventListener('submit', event => {
    save();
    if (submitted) { event.preventDefault(); return; }
    if (!form.checkValidity()) return;
    submitted = true;
    const button = form.querySelector('[data-submit-once]');
    if (button) button.disabled = true;
    const label = form.querySelector('[data-submit-label]');
    const progress = form.querySelector('[data-submit-progress]');
    if (label) label.hidden = true;
    if (progress) progress.hidden = false;
  });
  window.addEventListener('pageshow', () => {
    if (!submitted) return;
    submitted = false;
    const button = form.querySelector('[data-submit-once]');
    if (button) button.disabled = button.dataset.canSubmit !== 'true';
    const label = form.querySelector('[data-submit-label]');
    const progress = form.querySelector('[data-submit-progress]');
    if (label) label.hidden = false;
    if (progress) progress.hidden = true;
  });
  window.addEventListener('beforeunload', event => {
    if (submitted || safeNavigation || !hasText()) return;
    save();
    event.preventDefault();
    event.returnValue = '';
  });

  const summary = page.querySelector('[data-validation-summary]:not(:empty)');
  if (summary) summary.focus();
})();
