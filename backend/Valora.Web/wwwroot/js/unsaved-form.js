(() => {
  document.querySelectorAll('[data-unsaved-form]').forEach(form => {
    let dirty = false;
    form.addEventListener('input', () => { dirty = true; });
    form.addEventListener('submit', () => { dirty = false; });
    form.querySelectorAll('a').forEach(link => link.addEventListener('click', event => {
      if (dirty && !window.confirm('Há alterações não salvas. Deseja sair desta página?')) event.preventDefault();
    }));
    window.addEventListener('beforeunload', event => { if (dirty) { event.preventDefault(); event.returnValue = ''; } });
  });
})();
