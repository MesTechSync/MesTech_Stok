/**
 * MesTech API Utilities — Shared fetch wrapper
 * DRY: Centralized HTTP calls with error handling, timeout, retry.
 * Usage: const data = await mestechApi.get('/summary');
 */
'use strict';

const mestechApi = (function() {
  const DEFAULT_TIMEOUT = 15000; // 15 seconds
  const MAX_RETRIES = 1;

  /**
   * Core fetch wrapper with timeout + error normalization
   * @param {string} url - Full URL or path (relative to API_BASE)
   * @param {object} [options] - fetch options + { timeout, retries }
   * @returns {Promise<any>} Parsed JSON response
   */
  async function request(url, options) {
    options = options || {};
    const timeout = options.timeout || DEFAULT_TIMEOUT;
    const retries = options.retries != null ? options.retries : MAX_RETRIES;
    delete options.timeout;
    delete options.retries;

    // Resolve relative paths against API_BASE (if defined globally)
    if (url.startsWith('/') && typeof API_BASE !== 'undefined') {
      url = API_BASE + url;
    }

    for (let attempt = 0; attempt <= retries; attempt++) {
      const controller = new AbortController();
      const timeoutId = setTimeout(function() { controller.abort(); }, timeout);

      try {
        const response = await fetch(url, Object.assign({}, options, {
          signal: controller.signal
        }));

        clearTimeout(timeoutId);

        if (!response.ok) {
          throw new MestechApiError(
            'HTTP ' + response.status + ': ' + response.statusText,
            response.status,
            url
          );
        }

        const contentType = response.headers.get('content-type') || '';
        if (contentType.indexOf('application/json') !== -1) {
          return await response.json();
        }
        return await response.text();

      } catch (err) {
        clearTimeout(timeoutId);

        if (err.name === 'AbortError') {
          err = new MestechApiError('Istek zaman asimina ugradi (' + (timeout/1000) + 's)', 0, url);
        }

        // Last attempt — throw
        if (attempt >= retries) {
          if (!(err instanceof MestechApiError)) {
            err = new MestechApiError(err.message || 'Baglanti hatasi', 0, url);
          }
          throw err;
        }

        // Wait before retry (500ms)
        await new Promise(function(r) { setTimeout(r, 500); });
      }
    }
  }

  /**
   * GET request
   * @param {string} url
   * @param {object} [options]
   */
  function get(url, options) {
    return request(url, Object.assign({ method: 'GET' }, options));
  }

  /**
   * POST request with JSON body
   * @param {string} url
   * @param {object} body
   * @param {object} [options]
   */
  function post(url, body, options) {
    return request(url, Object.assign({
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body)
    }, options));
  }

  /**
   * PUT request with JSON body
   * @param {string} url
   * @param {object} body
   * @param {object} [options]
   */
  function put(url, body, options) {
    return request(url, Object.assign({
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body)
    }, options));
  }

  /**
   * DELETE request
   * @param {string} url
   * @param {object} [options]
   */
  function del(url, options) {
    return request(url, Object.assign({ method: 'DELETE' }, options));
  }

  // Structured error class
  function MestechApiError(message, status, url) {
    this.name = 'MestechApiError';
    this.message = message;
    this.status = status;
    this.url = url;
  }
  MestechApiError.prototype = Object.create(Error.prototype);

  return {
    get: get,
    post: post,
    put: put,
    del: del,
    request: request,
    MestechApiError: MestechApiError
  };
})();
