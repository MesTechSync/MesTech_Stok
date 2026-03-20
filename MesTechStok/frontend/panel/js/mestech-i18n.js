/**
 * MesTech i18n Engine — HTML Panel
 * Version: 1.0.0
 *
 * Usage:
 *   HTML: <span data-i18n="Products.Title">Urunler</span>
 *   JS:   MesTechI18n.t('Products.Title')
 *   Init: MesTechI18n.init('tr')
 *   Switch: MesTechI18n.setLanguage('en')
 *
 * Attributes:
 *   data-i18n            — sets textContent
 *   data-i18n-placeholder — sets placeholder
 *   data-i18n-title      — sets title attribute
 *   data-i18n-html       — sets textContent (safe, no innerHTML)
 */
const MesTechI18n = {
  currentLang: 'tr',
  translations: {},

  async init(lang) {
    this.currentLang = localStorage.getItem('mestech-language') || lang || 'tr';
    await this.loadTranslations(this.currentLang);
    this.applyAll();
    this.observeMutations();
    document.dispatchEvent(new CustomEvent('i18n:ready', { detail: { lang: this.currentLang } }));
  },

  async loadTranslations(lang) {
    try {
      const basePath = this._getBasePath();
      const response = await fetch(`${basePath}js/i18n/${lang}.json`);
      if (!response.ok) throw new Error(`HTTP ${response.status}`);
      this.translations = await response.json();
    } catch (e) {
      console.warn(`[MesTech i18n] ${lang}.json failed, fallback tr`, e);
      if (lang !== 'tr') await this.loadTranslations('tr');
    }
  },

  _getBasePath() {
    const path = window.location.pathname;
    const depth = (path.match(/\//g) || []).length - 1;
    return '../'.repeat(Math.max(0, depth - 1)) || './';
  },

  t(key, ...args) {
    let text = this.translations[key] || key;
    args.forEach((arg, i) => {
      text = text.replace(`{${i}}`, arg);
    });
    return text;
  },

  applyAll() {
    document.querySelectorAll('[data-i18n]').forEach(el => {
      el.textContent = this.t(el.dataset.i18n);
    });
    document.querySelectorAll('[data-i18n-placeholder]').forEach(el => {
      el.placeholder = this.t(el.dataset.i18nPlaceholder);
    });
    document.querySelectorAll('[data-i18n-title]').forEach(el => {
      el.title = this.t(el.dataset.i18nTitle);
    });
    document.querySelectorAll('[data-i18n-html]').forEach(el => {
      el.textContent = this.t(el.dataset.i18nHtml);
    });
    const selector = document.getElementById('language-selector');
    if (selector) selector.value = this.currentLang;
  },

  async setLanguage(lang) {
    this.currentLang = lang;
    localStorage.setItem('mestech-language', lang);
    await this.loadTranslations(lang);
    this.applyAll();
    document.dispatchEvent(new CustomEvent('i18n:changed', { detail: { lang } }));
  },

  observeMutations() {
    const observer = new MutationObserver(mutations => {
      for (const m of mutations) {
        for (const node of m.addedNodes) {
          if (node.nodeType === 1) {
            if (node.dataset && node.dataset.i18n) {
              node.textContent = this.t(node.dataset.i18n);
            }
            if (node.querySelectorAll) {
              node.querySelectorAll('[data-i18n]').forEach(el => {
                el.textContent = this.t(el.dataset.i18n);
              });
              node.querySelectorAll('[data-i18n-placeholder]').forEach(el => {
                el.placeholder = this.t(el.dataset.i18nPlaceholder);
              });
              node.querySelectorAll('[data-i18n-title]').forEach(el => {
                el.title = this.t(el.dataset.i18nTitle);
              });
            }
          }
        }
      }
    });
    observer.observe(document.body, { childList: true, subtree: true });
  },

  get lang() {
    return this.currentLang;
  }
};

// Auto-init on DOM ready
document.addEventListener('DOMContentLoaded', () => MesTechI18n.init());
