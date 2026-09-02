/** Shared progressive enhancement for internal forms. */
window.Form = window.Form || {};

(() => {
  const warningMessage = "Revise os campos destacados antes de continuar.";

  function submitButton(form) {
    return form.querySelector('[data-submit], button[type="submit"], input[type="submit"]');
  }

  function setLoading(form, loading) {
    const button = submitButton(form);
    if (!button) return;

    button.disabled = loading;
    button.setAttribute("aria-busy", String(loading));
    button.classList.toggle("is-loading", loading);
    form.toggleAttribute("data-submitting", loading);
  }

  function announceValidation(form) {
    let summary = form.querySelector("[data-validation-summary]");
    if (!summary) {
      summary = document.createElement("div");
      summary.className = "validation-summary-errors";
      summary.dataset.validationSummary = "true";
      summary.setAttribute("role", "alert");
      summary.setAttribute("tabindex", "-1");
      form.prepend(summary);
    }
    summary.textContent = warningMessage;
    summary.focus({ preventScroll: true });
    form.querySelector(":invalid")?.focus();
  }

  // Native constraint validation can stop a submit event before it reaches the form.
  document.addEventListener("invalid", (event) => {
    const form = event.target instanceof HTMLElement ? event.target.closest("form") : null;
    if (form && !form.dataset.validationAnnounced) {
      form.dataset.validationAnnounced = "true";
      window.setTimeout(() => {
        announceValidation(form);
        delete form.dataset.validationAnnounced;
      });
    }
  }, true);

  document.addEventListener("submit", (event) => {
    const form = event.target;
    if (!(form instanceof HTMLFormElement) || form.method.toLowerCase() === "dialog") return;

    if (!form.checkValidity()) {
      event.preventDefault();
      announceValidation(form);
      return;
    }

    // Let page-specific asynchronous handlers cancel submission before locking controls.
    window.setTimeout(() => {
      if (!event.defaultPrevented) setLoading(form, true);
    });
  });

  document.addEventListener("input", (event) => {
    const field = event.target;
    if (!(field instanceof HTMLElement)) return;
    const form = field.closest("form");
    if (form?.checkValidity()) form.querySelector("[data-validation-summary]")?.remove();
  });

  window.addEventListener("pageshow", () => {
    document.querySelectorAll("form[data-submitting]").forEach((form) => setLoading(form, false));
  });

  window.Form.setLoading = setLoading;
})();
