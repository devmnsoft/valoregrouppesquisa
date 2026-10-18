/**
 * ValoraForms — Enterprise Form Validation & Checkbox List Management
 * Provides accessible HTML5 inline validation, double-submit protection,
 * and standardized checkbox lists that display names, show incremental numbering, and submit IDs.
 */
(function (global) {
  'use strict';

  function getErrorMessage(field) {
    if (!field || !field.validity) return 'Campo inválido.';
    const v = field.validity;
    if (v.valueMissing) {
      if (field.type === 'checkbox' || field.type === 'radio') {
        return 'Esta seleção é obrigatória.';
      }
      return 'Preencha este campo obrigatório.';
    }
    if (v.typeMismatch) {
      if (field.type === 'email') return 'Informe um endereço de e-mail válido (ex: nome@empresa.com.br).';
      if (field.type === 'url') return 'Informe um endereço web válido iniciado por http:// ou https://.';
      return 'Formato do valor informado não é válido.';
    }
    if (v.tooShort) {
      return `Mínimo de ${field.minLength} caracteres necessários (atualmente com ${field.value.length}).`;
    }
    if (v.tooLong) {
      return `Máximo de ${field.maxLength} caracteres permitidos.`;
    }
    if (v.rangeUnderflow) {
      return `O valor mínimo permitido é ${field.min}.`;
    }
    if (v.rangeOverflow) {
      return `O valor máximo permitido é ${field.max}.`;
    }
    if (v.stepMismatch) {
      return 'O valor digitado não corresponde ao incremento aceito.';
    }
    if (v.patternMismatch) {
      return field.title || 'O formato do campo não atende ao padrão esperado.';
    }
    if (v.customError && field.validationMessage) {
      return field.validationMessage;
    }
    return 'Revise o conteúdo deste campo.';
  }

  function showFieldError(field, message) {
    if (!field || !(field instanceof HTMLElement)) return;
    field.classList.add('is-invalid');
    field.setAttribute('aria-invalid', 'true');

    // Locate or create accessible inline error element
    let parent = field.closest('.valora-field, .form-group, .action-field--full, label, div') || field.parentElement;
    let errorElem = parent.querySelector('.valora-field-error, .invalid-feedback');
    if (!errorElem) {
      errorElem = document.createElement('div');
      errorElem.className = 'valora-field-error';
      errorElem.setAttribute('role', 'alert');
      errorElem.setAttribute('aria-live', 'polite');
      if (field.id) {
        errorElem.id = `${field.id}-error`;
        const existingDescribed = field.getAttribute('aria-describedby') || '';
        if (!existingDescribed.includes(errorElem.id)) {
          field.setAttribute('aria-describedby', (existingDescribed + ' ' + errorElem.id).trim());
        }
      }
      field.insertAdjacentElement('afterend', errorElem);
    }
    errorElem.textContent = message || getErrorMessage(field);
  }

  function clearFieldError(field) {
    if (!field || !(field instanceof HTMLElement)) return;
    field.classList.remove('is-invalid');
    field.removeAttribute('aria-invalid');

    const parent = field.closest('.valora-field, .form-group, .action-field--full, label, div') || field.parentElement;
    const errorElem = parent?.querySelector('.valora-field-error, .invalid-feedback');
    if (errorElem) {
      errorElem.textContent = '';
    }
  }

  function validateForm(form) {
    if (!(form instanceof HTMLFormElement)) return true;
    const elements = [...form.querySelectorAll('input, select, textarea')].filter(el => !el.disabled && el.type !== 'hidden');
    let firstInvalid = null;
    let isValid = true;

    for (const field of elements) {
      if (!field.checkValidity()) {
        isValid = false;
        showFieldError(field);
        if (!firstInvalid) firstInvalid = field;
      } else {
        clearFieldError(field);
      }
    }

    if (!isValid) {
      form.classList.add('was-validated');
      if (firstInvalid) {
        firstInvalid.scrollIntoView({ behavior: 'smooth', block: 'center' });
        firstInvalid.focus({ preventScroll: true });
      }
      global.ValoraUI?.announce('Revise os campos destacados antes de continuar.', 'error');
    }

    return isValid;
  }

  function resetSubmit(form) {
    if (!form) return;
    form.removeAttribute('data-submitting');
    const buttons = form.querySelectorAll('button[type="submit"], input[type="submit"]');
    buttons.forEach(btn => {
      global.ValoraUI?.setBusy(btn, false);
      btn.disabled = false;
      btn.removeAttribute('aria-busy');
    });
  }

  // --- Checkbox List Management ---
  function initCheckboxList(container) {
    if (!container || container.dataset.checkboxListInitialized === 'true') return;
    container.dataset.checkboxListInitialized = 'true';

    const items = [...container.querySelectorAll('.valora-checkbox-item, [data-checkbox-item]')];
    const counter = container.querySelector('[data-checkbox-count]');
    const selectAllBtn = container.querySelector('[data-action="select-all"]');
    const clearAllBtn = container.querySelector('[data-action="clear-all"]');
    const filterInput = container.querySelector('[data-checkbox-filter]');

    function updateState() {
      let selectedCount = 0;
      let totalVisible = 0;

      items.forEach((item, idx) => {
        const checkbox = item.querySelector('input[type="checkbox"], input[type="radio"]');
        if (!checkbox) return;

        // Ensure incremental index badge exists and is correctly numbered
        let idxBadge = item.querySelector('.valora-checkbox-item__idx, .valora-idx-badge');
        if (!idxBadge) {
          idxBadge = document.createElement('span');
          idxBadge.className = 'valora-checkbox-item__idx';
          item.prepend(idxBadge);
        }
        idxBadge.textContent = `#${idx + 1}`;

        const isChecked = checkbox.checked;
        item.classList.toggle('is-selected', isChecked);
        if (isChecked) selectedCount++;
        if (!item.hidden) totalVisible++;
      });

      if (counter) {
        counter.textContent = `${selectedCount} de ${items.length} selecionado(s)`;
      }
    }

    // Bind individual checkbox changes
    container.addEventListener('change', event => {
      const target = event.target;
      if (target instanceof HTMLInputElement && (target.type === 'checkbox' || target.type === 'radio')) {
        updateState();
      }
    });

    // Select all action
    if (selectAllBtn) {
      selectAllBtn.addEventListener('click', e => {
        e.preventDefault();
        items.forEach(item => {
          if (!item.hidden) {
            const checkbox = item.querySelector('input[type="checkbox"]');
            if (checkbox && !checkbox.disabled) checkbox.checked = true;
          }
        });
        updateState();
        container.dispatchEvent(new CustomEvent('valora:checkbox-list:changed', { bubbles: true }));
      });
    }

    // Clear all action
    if (clearAllBtn) {
      clearAllBtn.addEventListener('click', e => {
        e.preventDefault();
        items.forEach(item => {
          const checkbox = item.querySelector('input[type="checkbox"]');
          if (checkbox && !checkbox.disabled) checkbox.checked = false;
        });
        updateState();
        container.dispatchEvent(new CustomEvent('valora:checkbox-list:changed', { bubbles: true }));
      });
    }

    // Search / filter by name action
    if (filterInput) {
      filterInput.addEventListener('input', () => {
        const query = filterInput.value.trim().toLowerCase();
        items.forEach(item => {
          const text = (item.textContent || '').toLowerCase();
          const matches = !query || text.includes(query);
          item.hidden = !matches;
        });
        updateState();
      });
    }

    // Initial state calculation
    updateState();
  }

  function getSelectedIds(container) {
    if (!container) return [];
    return [...container.querySelectorAll('input[type="checkbox"]:checked, input[type="radio"]:checked')]
      .map(cb => cb.value)
      .filter(Boolean);
  }

  function scan(root = document) {
    root.querySelectorAll('.valora-checkbox-list, [data-checkbox-list]').forEach(initCheckboxList);
  }

  // --- Global Event Listeners ---
  document.addEventListener('submit', event => {
    const form = event.target;
    if (!(form instanceof HTMLFormElement) || form.method?.toLowerCase() === 'dialog') return;

    if (!validateForm(form)) {
      event.preventDefault();
      event.stopImmediatePropagation();
      return;
    }

    const button = event.submitter;
    if (button instanceof HTMLButtonElement) {
      global.ValoraUI?.setBusy(button, true, button.dataset.loadingText || 'Salvando…');
      // Safety release after 15 seconds in case of unhandled errors
      window.setTimeout(() => resetSubmit(form), 15000);
    }
  }, true);

  document.addEventListener('input', event => {
    const field = event.target;
    if (field instanceof HTMLInputElement || field instanceof HTMLSelectElement || field instanceof HTMLTextAreaElement) {
      if (field.validity.valid) {
        clearFieldError(field);
      } else if (field.closest('form')?.classList.contains('was-validated') || field.classList.contains('is-invalid')) {
        showFieldError(field);
      }
    }
  });

  document.addEventListener('change', event => {
    const field = event.target;
    if (field instanceof HTMLInputElement || field instanceof HTMLSelectElement || field instanceof HTMLTextAreaElement) {
      if (field.validity.valid) {
        clearFieldError(field);
      } else if (field.closest('form')?.classList.contains('was-validated')) {
        showFieldError(field);
      }
    }
  });

  document.addEventListener('DOMContentLoaded', () => {
    scan(document);
  });

  // Export public API
  global.ValoraForms = Object.freeze({
    validate: validateForm,
    showFieldError,
    clearFieldError,
    resetSubmit,
    initCheckboxList,
    getSelectedIds,
    scan
  });

})(typeof window !== 'undefined' ? window : this);
