(function () {
  const defaultTimeout = 20000;

  function apiBaseUrl() {
    return (window.ValoraWebConfig && window.ValoraWebConfig.API_BASE_URL ? window.ValoraWebConfig.API_BASE_URL : '').replace(/\/$/, '');
  }

  function generateCorrelationId() {
    if (window.crypto && window.crypto.randomUUID) {
      return 'web-' + window.crypto.randomUUID();
    }
    return 'web-' + Date.now() + '-' + Math.random().toString(16).slice(2);
  }

  function getToken() { return null; }

  function setToken() { }

  function clearToken() { if (window.Session) window.Session.clear(); }

  function normalizeApiError(xhr, correlationId) {
    const body = xhr && xhr.responseJSON && typeof xhr.responseJSON === 'object' ? xhr.responseJSON : {};
    const status = xhr && typeof xhr.status === 'number' ? xhr.status : 0;
    const responseText = xhr && xhr.responseText ? String(xhr.responseText) : '';
    const getHeader = xhr && typeof xhr.getResponseHeader === 'function' ? xhr.getResponseHeader.bind(xhr) : function () { return ''; };
    const contentType = getHeader('content-type') || '';
    let message = 'Não foi possível comunicar com a API.';

    if (responseText.trim().startsWith('<') || contentType.indexOf('text/html') >= 0) {
      message = 'A API retornou uma página HTML inesperada. Verifique publicação, proxy ou rota.';
    } else if (status === 0) {
      message = 'API offline ou indisponível no momento.';
    } else if (status === 401) {
      clearToken();
      message = 'Sessão expirada. Entre novamente.';
    } else if (status === 403) {
      message = 'Você não tem permissão para executar esta ação.';
    } else if (status === 404) {
      message = 'Recurso não encontrado na API.';
    } else if (status === 409) {
      message = body.message || 'Conflito de dados detectado pela API.';
    } else if (status === 422) {
      message = body.message || 'Verifique os dados informados antes de continuar.';
    } else if (status >= 500) {
      message = 'Erro interno na API. Informe o suporte com o correlationId.';
    } else if (body.message) {
      message = body.message;
    }

    return {
      ok: false,
      status: status,
      code: body.code || 'API_ERROR',
      message: message,
      correlationId: body.correlationId || body.correlationID || correlationId,
      traceId: body.traceId || body.traceID || ''
    };
  }

  function requestJson(method, path, data, options) {
    const correlationId = generateCorrelationId();
    const csrf = document.querySelector('meta[name="csrf-token"]');
    const headers = { 'X-Correlation-Id': correlationId, 'X-CSRF-TOKEN': csrf ? csrf.content : '' };

    const controller = new AbortController();
    const timeout = setTimeout(() => controller.abort(), (window.ValoraWebConfig && window.ValoraWebConfig.API_TIMEOUT_MS) || defaultTimeout);
    headers.Accept = 'application/json';
    if (data !== undefined && data !== null) headers['Content-Type'] = 'application/json; charset=utf-8';
    return fetch(apiBaseUrl() + path, { method, headers, credentials: 'same-origin', signal: controller.signal, body: data === undefined || data === null ? undefined : JSON.stringify(data) })
      .then(async response => {
        const responseText = await response.text();
        let body = null; try { body = responseText ? JSON.parse(responseText) : null; } catch { body = null; }
        if (!response.ok) throw normalizeApiError({ status: response.status, responseJSON: body, responseText, getResponseHeader: name => response.headers.get(name) }, correlationId);
        return body;
      }).catch(error => { if (error?.ok === false) throw error; throw normalizeApiError({ status: 0, responseText: '', getResponseHeader: () => '' }, correlationId); })
      .finally(() => clearTimeout(timeout));
  }

  function requestBinary(method, path, data) {
    const correlationId = generateCorrelationId();
    const headers = { 'X-Correlation-Id': correlationId };

    if (data) headers['Content-Type'] = 'application/json; charset=utf-8';
    return fetch(apiBaseUrl() + path, { method, headers, credentials: 'same-origin', body: data ? JSON.stringify(data) : undefined })
      .then(async response => { if (!response.ok) { const responseText=await response.text(); throw normalizeApiError({status:response.status,responseText,getResponseHeader:name=>response.headers.get(name)},correlationId); } return response.blob(); });
  }

  window.AjaxClient = {
    get: function (path, options) { return requestJson('GET', path, null, options); },
    post: function (path, data, options) { return requestJson('POST', path, data, options); },
    put: function (path, data, options) { return requestJson('PUT', path, data, options); },
    patch: function (path, data, options) { return requestJson('PATCH', path, data, options); },
    delete: function (path, data, options) { return requestJson('DELETE', path, data, options); },
    requestJson: requestJson,
    requestBinary: requestBinary,
    setToken: setToken,
    getToken: getToken,
    clearToken: clearToken,
    normalizeApiError: normalizeApiError,
    generateCorrelationId: generateCorrelationId
  };
}());
