(() => {
  'use strict';
  const sidebar = document.querySelector('[data-admin-sidebar]');
  const mobile = document.querySelector('#mobileSidebar');
  const toggle = document.querySelector('[data-action="toggle-navigation"]');
  const collapse = document.querySelector('[data-action="collapse-navigation"]');
  if (!sidebar || !mobile || !toggle) return;

  const links = document.querySelectorAll('.valora-sidebar-nav .nav-link');
  const path = window.location.pathname.toLowerCase();
  links.forEach((link) => {
    const current = path === new URL(link.href).pathname.toLowerCase() || (path.startsWith(new URL(link.href).pathname.toLowerCase()) && new URL(link.href).pathname !== '/');
    link.classList.toggle('active', current);
    if (current) link.setAttribute('aria-current', 'page');
    link.addEventListener('click', () => closeMobile());
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
