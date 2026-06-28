(function () {
  const publicPrefixes = ['/account/login', '/account/register', '/account/forgotpassword', '/account/resetpassword', '/s/', '/r/', '/certificates/validate'];
  function isPublicPath(path) {
    const normalized = (path || window.location.pathname || '/').toLowerCase();
    if (normalized === '/') return true;
    return publicPrefixes.some(function (prefix) { return normalized === prefix || normalized.indexOf(prefix) === 0; });
  }
  function requireAuth() {
    if (!isPublicPath(window.location.pathname) && window.Session && !Session.isAuthenticated()) {
      window.location.href = '/Account/Login?returnUrl=' + encodeURIComponent(window.location.pathname + window.location.search);
      return false;
    }
    return true;
  }
  function logout() {
    if (window.Session) Session.clear();
    window.location.href = '/Account/Login';
  }
  window.Guards = { isPublicPath: isPublicPath, requireAuth: requireAuth, logout: logout };
  $(function () {
    $('[data-logout]').on('click', function (event) { event.preventDefault(); logout(); });
    requireAuth();
  });
}());
