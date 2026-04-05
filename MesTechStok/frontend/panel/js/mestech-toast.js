/**
 * MesTech Toast Notification — Shared notification utility
 * Bootstrap 5 Toast based, auto-dismiss, stackable, accessible.
 * Usage: mestechToast.success('Kaydedildi');
 *        mestechToast.error('Baglanti hatasi');
 *        mestechToast.warning('Stok dusuk');
 *        mestechToast.info('Guncelleme mevcut');
 */
'use strict';

const mestechToast = (function() {
  const CONTAINER_ID = 'mestech-toast-container';
  const AUTO_DISMISS_MS = 5000;
  const MAX_TOASTS = 5;

  const TYPES = {
    success: { icon: 'fa-check-circle', bg: 'bg-success', label: 'Basarili' },
    error:   { icon: 'fa-exclamation-circle', bg: 'bg-danger', label: 'Hata' },
    warning: { icon: 'fa-exclamation-triangle', bg: 'bg-warning', label: 'Uyari' },
    info:    { icon: 'fa-info-circle', bg: 'bg-primary', label: 'Bilgi' }
  };

  /**
   * Ensure toast container exists in DOM
   */
  function getContainer() {
    let container = document.getElementById(CONTAINER_ID);
    if (!container) {
      container = document.createElement('div');
      container.id = CONTAINER_ID;
      container.className = 'toast-container position-fixed top-0 end-0 p-3';
      container.style.zIndex = '9999';
      container.setAttribute('aria-live', 'polite');
      container.setAttribute('aria-atomic', 'true');
      document.body.appendChild(container);
    }
    return container;
  }

  /**
   * Trim old toasts if over limit
   */
  function trimToasts(container) {
    let toasts = container.querySelectorAll('.toast');
    while (toasts.length >= MAX_TOASTS) {
      const oldest = toasts[0];
      oldest.remove();
      toasts = container.querySelectorAll('.toast');
    }
  }

  /**
   * Show a toast notification
   * @param {string} message - Notification text
   * @param {'success'|'error'|'warning'|'info'} type - Toast type
   * @param {number} [duration] - Auto-dismiss ms (0 = no auto-dismiss)
   */
  function show(message, type, duration) {
    type = type || 'info';
    duration = duration != null ? duration : AUTO_DISMISS_MS;
    const config = TYPES[type] || TYPES.info;

    const container = getContainer();
    trimToasts(container);

    // Build toast element (DOM-safe — no innerHTML)
    const toast = document.createElement('div');
    toast.className = 'toast show border-0 shadow-sm';
    toast.setAttribute('role', 'alert');
    toast.setAttribute('aria-live', 'assertive');
    toast.setAttribute('aria-atomic', 'true');

    const header = document.createElement('div');
    header.className = 'toast-header ' + config.bg + ' text-white';

    const icon = document.createElement('i');
    icon.className = 'fas ' + config.icon + ' me-2';

    const title = document.createElement('strong');
    title.className = 'me-auto';
    title.textContent = config.label;

    const time = document.createElement('small');
    time.className = 'text-white-50';
    time.textContent = new Date().toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' });

    const closeBtn = document.createElement('button');
    closeBtn.type = 'button';
    closeBtn.className = 'btn-close btn-close-white';
    closeBtn.setAttribute('aria-label', 'Kapat');
    closeBtn.addEventListener('click', function() {
      toast.classList.remove('show');
      setTimeout(function() { toast.remove(); }, 300);
    });

    header.appendChild(icon);
    header.appendChild(title);
    header.appendChild(time);
    header.appendChild(closeBtn);

    const body = document.createElement('div');
    body.className = 'toast-body';
    body.textContent = message;

    toast.appendChild(header);
    toast.appendChild(body);
    container.appendChild(toast);

    // Auto-dismiss
    if (duration > 0) {
      setTimeout(function() {
        if (toast.parentNode) {
          toast.classList.remove('show');
          setTimeout(function() { toast.remove(); }, 300);
        }
      }, duration);
    }

    return toast;
  }

  return {
    success: function(msg, dur) { return show(msg, 'success', dur); },
    error:   function(msg, dur) { return show(msg, 'error', dur != null ? dur : 8000); },
    warning: function(msg, dur) { return show(msg, 'warning', dur); },
    info:    function(msg, dur) { return show(msg, 'info', dur); },
    show:    show
  };
})();
