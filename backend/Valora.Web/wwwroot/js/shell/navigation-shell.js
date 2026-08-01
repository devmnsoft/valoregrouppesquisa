(function () {
  "use strict";

  const sidebar = document.getElementById("mobileSidebar");
  const toggle = document.querySelector('[data-action="toggle-navigation"]');
  let returnFocus = null;

  function setState(open) {
    document.body.classList.toggle("web-mobile-menu-open", open);
    document.body.style.overflow = open ? "hidden" : "";
    toggle?.setAttribute("aria-expanded", String(open));
  }

  function open() {
    if (!sidebar) return;
    returnFocus = document.activeElement;
    window.bootstrap?.Offcanvas.getOrCreateInstance(sidebar).show();
    setState(true);
  }

  function close() {
    if (!sidebar) return;
    window.bootstrap?.Offcanvas.getOrCreateInstance(sidebar).hide();
    setState(false);
    if (returnFocus instanceof HTMLElement) returnFocus.focus();
  }

  function markCurrentRoute() {
    const path = window.location.pathname.toLowerCase();
    document.querySelectorAll(".sidebar-nav .nav-link, #mobileSidebar .nav-link").forEach((link) => {
      const current = path === new URL(link.href).pathname.toLowerCase();
      link.classList.toggle("active", current);
      if (current) link.setAttribute("aria-current", "page");
      else link.removeAttribute("aria-current");
    });
  }

  document.addEventListener("click", (event) => {
    if (event.target.closest('[data-action="toggle-navigation"]')) open();
    if (event.target.closest('[data-action="close-navigation"]')) close();
    if (event.target.closest("#mobileSidebar .nav-link")) close();
  });
  document.addEventListener("keydown", (event) => { if (event.key === "Escape") close(); });
  sidebar?.addEventListener("shown.bs.offcanvas", () => setState(true));
  sidebar?.addEventListener("hidden.bs.offcanvas", () => setState(false));
  markCurrentRoute();
})();
