$(function(){
  const $body=$(document.body);
  const path=window.location.pathname.toLowerCase();
  $('.sidebar-nav .nav-link').each(function(){const href=($(this).attr('href')||'').toLowerCase(); if(href&&path.startsWith(href.toLowerCase()))$(this).addClass('active');});
  const sidebarEl=document.getElementById('mobileSidebar');
  const $toggle=$('[data-action="toggleWebMobileAdminMenu"]');
  function setExpanded(open){$toggle.attr('aria-expanded',open?'true':'false');$body.toggleClass('web-mobile-menu-open',open);}
  $toggle.on('click',function(){const off=bootstrap.Offcanvas.getOrCreateInstance(sidebarEl);off.show();setExpanded(true);});
  $('#mobileSidebar .nav-link').on('click',function(){const off=bootstrap.Offcanvas.getInstance(sidebarEl); if(off)off.hide();});
  if(sidebarEl){sidebarEl.addEventListener('shown.bs.offcanvas',()=>setExpanded(true));sidebarEl.addEventListener('hidden.bs.offcanvas',()=>setExpanded(false));}
  $(document).on('keydown',function(event){if(event.key==='Escape'){const off=bootstrap.Offcanvas.getInstance(sidebarEl);if(off)off.hide();setExpanded(false);}});
});
