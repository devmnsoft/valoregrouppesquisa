(() => {
  document.querySelectorAll('[data-unsaved-form]').forEach(form => {
    let baseline = new URLSearchParams(new FormData(form)).toString();
    let submitting = false;
    const dirty = () => new URLSearchParams(new FormData(form)).toString() !== baseline;
    form.addEventListener('submit', event => {
      // A validator can cancel later in the same dispatch; only commit the state afterwards.
      queueMicrotask(() => { submitting = !event.defaultPrevented && form.checkValidity(); });
    });
    form.addEventListener('invalid', () => { submitting = false; }, true);
    form.querySelectorAll('a').forEach(link => link.addEventListener('click', event => {
      if (!submitting && dirty() && !window.confirm('Há alterações não salvas. Deseja sair desta página?')) event.preventDefault();
    }));
    window.addEventListener('beforeunload', event => { if (!submitting && dirty()) { event.preventDefault(); event.returnValue = ''; } });
    window.addEventListener('pageshow', event => { submitting = false; if (event.persisted) baseline = new URLSearchParams(new FormData(form)).toString(); });
  });
})();
