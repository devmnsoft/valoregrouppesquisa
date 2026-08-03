(() => {
  'use strict';
  const sidebar = document.querySelector('[data-admin-sidebar]');
  const mobile = document.querySelector('#mobileSidebar');
  const toggle = document.querySelector('[data-action="toggle-navigation"]');
  const collapse = document.querySelector('[data-action="collapse-navigation"]');
  if (!sidebar || !mobile || !toggle) return;

  const links = document.querySelectorAll('.valora-sidebar-nav .nav-link');
  links.forEach((link) => {
    link.addEventListener('click', () => closeMobile());
  });

  document.querySelectorAll('[data-navigation-section]').forEach((button) => {
    const content = document.getElementById(button.getAttribute('aria-controls'));
    const storageKey = `valora-navigation-${button.dataset.navigationSection}`;
    const stored = localStorage.getItem(storageKey);
    if (stored !== null && !content.querySelector('[aria-current="page"]')) {
      button.setAttribute('aria-expanded', stored);
      content.classList.toggle('is-collapsed', stored !== 'true');
    }
    button.addEventListener('click', () => {
      const expanded = button.getAttribute('aria-expanded') === 'true';
      button.setAttribute('aria-expanded', String(!expanded));
      content.classList.toggle('is-collapsed', expanded);
      localStorage.setItem(storageKey, String(!expanded));
    });
  });

  const setMobileState = (open) => {
    toggle.setAttribute('aria-expanded', String(open));
    document.body.classList.toggle('navigation-open', open);
  };
  const openMobile = () => {
    bootstrap.Offcanvas.getOrCreateInstance(mobile).show();
    setMobileState(true);
  };
  const closeMobile = () => {
    bootstrap.Offcanvas.getOrCreateInstance(mobile).hide();
    setMobileState(false);
  };
  toggle.addEventListener('click', openMobile);
  mobile.addEventListener('shown.bs.offcanvas', () => { setMobileState(true); mobile.querySelector('a')?.focus(); });
  mobile.addEventListener('hidden.bs.offcanvas', () => setMobileState(false));
  collapse?.addEventListener('click', () => {
    const collapsed = document.body.classList.toggle('navigation-collapsed');
    collapse.setAttribute('aria-expanded', String(!collapsed));
    localStorage.setItem('valora-navigation-collapsed', String(collapsed));
  });
  if (localStorage.getItem('valora-navigation-collapsed') === 'true') document.body.classList.add('navigation-collapsed');
})();
