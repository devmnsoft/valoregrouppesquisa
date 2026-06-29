(function () {
  'use strict';
  function normalizeUser(userOrToken) {
    if (!userOrToken) return null;
    if (typeof userOrToken === 'string') return { resultToken: userOrToken, role: 'convidado_externo' };
    return userOrToken;
  }
  function can(user, permission) {
    if (!permission) return false;
    if (window.ValoraRoles && typeof window.ValoraRoles.can === 'function') return window.ValoraRoles.can(user, permission);
    return false;
  }
  function canAccessCompany(user, companyId) {
    if (window.ValoraRoles && typeof window.ValoraRoles.canAccessCompany === 'function') return window.ValoraRoles.canAccessCompany(user, companyId);
    return false;
  }
  function canUseModule(user, company, moduleId) {
    if (window.ValoraModules && typeof window.ValoraModules.canUseModule === 'function') return window.ValoraModules.canUseModule(user, company, moduleId);
    return false;
  }
  function canCreateRole(currentUser, targetRole) {
    return !!(window.ValoraRoles && window.ValoraRoles.canCreateRole(currentUser, targetRole));
  }
  function availableRolesForCurrentUser(currentUser) {
    return window.ValoraRoles ? window.ValoraRoles.availableRolesForCurrentUser(currentUser) : [];
  }
  function sameOwner(response, user) {
    return !!(response && user && (response.userId === user.id || response.respondentUserId === user.id || response.email === user.email));
  }
  function resolveResponsePermissions(response, userOrToken) {
    const user = normalizeUser(userOrToken);
    const hasToken = !!(typeof userOrToken === 'string' || user?.resultToken);
    const companyId = response?.organizationId || response?.companyId;
    const viewByRole = !!(user && can(user, 'canViewResponses') && canAccessCompany(user, companyId));
    const viewOwn = sameOwner(response, user) && can(user, 'canAnswerSurveys');
    const canView = !!(hasToken || viewByRole || viewOwn);
    const canEdit = !!(user && can(user, 'canCreateSurveys') && canAccessCompany(user, companyId) && response?.status !== 'finalized');
    return {
      canView,
      canEdit,
      canDownloadCertificate: canView && response?.certificateAvailable !== false,
      requiresResultToken: !viewByRole && !viewOwn
    };
  }
  function canViewResponse(response, userOrToken) { return resolveResponsePermissions(response, userOrToken).canView; }
  function canEditResponse(response, user) { return resolveResponsePermissions(response, user).canEdit; }
  function canDownloadResponseCertificate(response, user) { return resolveResponsePermissions(response, user).canDownloadCertificate; }
  window.ValoraWebPermissions = { can, canAccessCompany, canUseModule, canCreateRole, availableRolesForCurrentUser, resolveResponsePermissions, canViewResponse, canEditResponse, canDownloadResponseCertificate };
}());
