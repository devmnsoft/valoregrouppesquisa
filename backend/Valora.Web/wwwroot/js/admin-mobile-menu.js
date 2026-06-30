(function(){
  'use strict';
  var toggleSelector='[data-action="toggleWebMobileAdminMenu"],[data-web-mobile-admin-toggle]';
  var sidebarSelector='#mobileSidebar';
  function body(open){document.body.classList.toggle('web-mobile-menu-open', !!open);}
  function button(){return document.querySelector(toggleSelector);}
  function sidebar(){return document.querySelector(sidebarSelector);}
  function open(){var s=sidebar();var b=button(); if(!s)return false; if(window.bootstrap&&window.bootstrap.Offcanvas){window.bootstrap.Offcanvas.getOrCreateInstance(s).show();}else{s.classList.add('show');s.style.visibility='visible';} body(true); if(b)b.setAttribute('aria-expanded','true'); return true;}
  function close(){var s=sidebar();var b=button(); if(s){if(window.bootstrap&&window.bootstrap.Offcanvas){window.bootstrap.Offcanvas.getOrCreateInstance(s).hide();}else{s.classList.remove('show');s.style.visibility='hidden';}} body(false); if(b)b.setAttribute('aria-expanded','false'); return true;}
  function bind(){if(window.__valoraWebMobileAdminMenuBound)return; window.__valoraWebMobileAdminMenuBound=true; document.addEventListener('click',function(e){var t=e.target;var btn=t&&t.closest?t.closest(toggleSelector):null;if(btn){e.preventDefault();open();return;}var link=t&&t.closest?t.closest('#mobileSidebar a.nav-link'):null;if(link) setTimeout(close,80);}); document.addEventListener('keydown',function(e){if(e.key==='Escape')close();}); var s=sidebar(); if(s){s.addEventListener('shown.bs.offcanvas',function(){body(true);var b=button();if(b)b.setAttribute('aria-expanded','true');});s.addEventListener('hidden.bs.offcanvas',function(){body(false);var b=button();if(b)b.setAttribute('aria-expanded','false');});}}
  window.ValoraWebMobileAdminMenu={bind:bind,open:open,close:close};
  if(document.readyState==='loading')document.addEventListener('DOMContentLoaded',bind);else bind();
})();
