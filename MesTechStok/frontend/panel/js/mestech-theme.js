/**
 * MesTech Theme Manager — Dark/Light mode toggle
 * Reads system preference, stores user choice in localStorage.
 * Usage: Include this script, it auto-initializes.
 *        Call mestechTheme.toggle() to switch, or add data-theme-toggle to a button.
 */
'use strict';

const mestechTheme = (function() {
  const STORAGE_KEY = 'mestech-theme';
  const DARK = 'dark';
  const LIGHT = 'light';

  /**
   * Get current theme from: localStorage > system preference > default light
   */
  function getPreferred() {
    const stored = localStorage.getItem(STORAGE_KEY);
    if (stored === DARK || stored === LIGHT) return stored;

    if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
      return DARK;
    }
    return LIGHT;
  }

  /**
   * Apply theme to document
   */
  function apply(theme) {
    document.documentElement.setAttribute('data-theme', theme);
    document.body.setAttribute('data-theme', theme);

    // Update meta theme-color for mobile browsers
    const meta = document.querySelector('meta[name="theme-color"]');
    if (meta) {
      meta.setAttribute('content', theme === DARK ? '#0f172a' : '#f27a1a');
    }

    // Update all toggle button icons
    const toggleBtns = document.querySelectorAll('[data-theme-toggle]');
    for (let i = 0; i < toggleBtns.length; i++) {
      const icon = toggleBtns[i].querySelector('i');
      if (icon) {
        icon.className = theme === DARK ? 'fas fa-sun' : 'fas fa-moon';
      }
      toggleBtns[i].setAttribute('title', theme === DARK ? 'Acik temaya gec' : 'Koyu temaya gec');
    }
  }

  /**
   * Toggle between dark and light
   */
  function toggle() {
    const current = document.documentElement.getAttribute('data-theme') || LIGHT;
    const next = current === DARK ? LIGHT : DARK;
    localStorage.setItem(STORAGE_KEY, next);
    apply(next);
    return next;
  }

  /**
   * Get current active theme
   */
  function current() {
    return document.documentElement.getAttribute('data-theme') || LIGHT;
  }

  // Auto-initialize on load
  function init() {
    const theme = getPreferred();
    apply(theme);

    // Auto-bind click handlers to [data-theme-toggle] buttons
    document.addEventListener('click', function(e) {
      const btn = e.target.closest('[data-theme-toggle]');
      if (btn) {
        e.preventDefault();
        toggle();
      }
    });

    // Listen for system theme changes
    if (window.matchMedia) {
      try {
        window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', function(e) {
          // Only auto-switch if user hasn't manually set a preference
          if (!localStorage.getItem(STORAGE_KEY)) {
            apply(e.matches ? DARK : LIGHT);
          }
        });
      } catch (_) {
        // Safari < 14 fallback
      }
    }
  }

  // Run init when DOM is ready
  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }

  return {
    toggle: toggle,
    current: current,
    apply: apply,
    DARK: DARK,
    LIGHT: LIGHT
  };
})();
