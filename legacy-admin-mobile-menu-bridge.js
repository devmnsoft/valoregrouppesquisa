(function () {
  'use strict';

  var DEBUG_PREFIX = '[Valora Mobile Menu Bridge]';
  var TOGGLE_SELECTOR = '[data-action="toggleAdminMobileMenu"], .admin-mobile-toggle, [data-admin-mobile-toggle]';
  var SIDEBAR_SELECTOR = '#adminSidebar, .admin-sidebar, [data-admin-sidebar]';

  function isMobile() {
    return window.matchMedia && window.matchMedia('(max-width: 991.98px)').matches;
  }

  function sidebar() {
    return document.querySelector(SIDEBAR_SELECTOR);
  }

  function toggleButton() {
    return document.querySelector(TOGGLE_SELECTOR);
  }

  function overlay() {
    var existing = document.querySelector('.admin-mobile-overlay');

    if (existing) {
      return existing;
    }

    var el = document.createElement('div');
    el.className = 'admin-mobile-overlay';
    el.setAttribute('aria-hidden', 'true');
    document.body.appendChild(el);
    return el;
  }

  function open() {
    var s = sidebar();
    var b = toggleButton();
    var o = overlay();

    if (!s) {
      console.warn(DEBUG_PREFIX, 'sidebar não encontrada');
      return false;
    }

    s.classList.add('open');
    s.setAttribute('aria-hidden', 'false');

    o.classList.add('active');
    o.setAttribute('aria-hidden', 'false');

    document.body.classList.add('mobile-menu-open');

    if (b) {
      b.setAttribute('aria-expanded', 'true');
    }

    return true;
  }

  function close() {
    var s = sidebar();
    var b = toggleButton();
    var o = document.querySelector('.admin-mobile-overlay');

    if (s) {
      s.classList.remove('open');
      s.setAttribute('aria-hidden', 'true');
    }

    if (o) {
      o.classList.remove('active');
      o.setAttribute('aria-hidden', 'true');
    }

    document.body.classList.remove('mobile-menu-open');

    if (b) {
      b.setAttribute('aria-expanded', 'false');
    }

    return true;
  }

  function toggle() {
    var s = sidebar();

    if (s && s.classList.contains('open')) {
      return close();
    }

    return open();
  }

  function bind() {
    if (window.__valoraLegacyMobileMenuBridgeBound) {
      return;
    }

    window.__valoraLegacyMobileMenuBridgeBound = true;

    document.addEventListener('click', function (event) {
      var target = event.target;
      var btn = target && target.closest ? target.closest(TOGGLE_SELECTOR) : null;

      if (btn) {
        event.preventDefault();
        event.stopPropagation();
        toggle();
        return;
      }

      if (target && target.closest && target.closest('.admin-mobile-overlay')) {
        event.preventDefault();
        close();
        return;
      }

      var item = target && target.closest ? target.closest('#adminSidebar a, #adminSidebar button, .admin-sidebar a, .admin-sidebar button') : null;

      if (item && isMobile() && !item.matches(TOGGLE_SELECTOR)) {
        setTimeout(close, 80);
      }
    }, true);

    document.addEventListener('keydown', function (event) {
      if (event.key === 'Escape') {
        close();
      }
    });

    window.addEventListener('resize', function () {
      if (!isMobile()) {
        close();
      }
    });
  }

  function debug() {
    var s = sidebar();
    var b = toggleButton();
    var o = document.querySelector('.admin-mobile-overlay');

    return {
      bound: !!window.__valoraLegacyMobileMenuBridgeBound,
      hasSidebar: !!s,
      hasButton: !!b,
      hasOverlay: !!o,
      sidebarClass: s ? s.className : '',
      buttonClass: b ? b.className : '',
      buttonExpanded: b ? b.getAttribute('aria-expanded') : '',
      overlayClass: o ? o.className : '',
      bodyClass: document.body.className,
      viewport: {
        width: window.innerWidth,
        height: window.innerHeight,
        isMobile: isMobile()
      }
    };
  }

  window.ValoraAdminMobileMenuBridge = {
    bind: bind,
    open: open,
    close: close,
    toggle: toggle,
    debug: debug
  };

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', bind);
  } else {
    bind();
  }

  setTimeout(bind, 250);
  setTimeout(bind, 1000);
})();
