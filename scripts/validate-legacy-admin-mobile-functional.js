const {assert,has,done}=require('./_legacy-validator-lib');
['capture','toggleAdminMobileMenu','.admin-mobile-toggle','#adminSidebar, .admin-sidebar','admin-mobile-overlay','mobile-menu-open','aria-expanded','Escape','ValoraAdminMobileMenuBridge'].forEach(x=>assert(has('legacy-admin-mobile-menu-bridge.js',x),`bridge missing ${x}`));
['.admin-sidebar.open','.admin-mobile-overlay.active','body.mobile-menu-open','991.98px'].forEach(x=>assert(has('style.css',x),`css missing ${x}`));done('legacy admin mobile structural');
