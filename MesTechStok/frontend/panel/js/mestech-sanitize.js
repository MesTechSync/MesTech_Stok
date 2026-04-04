/**
 * MesTech Sanitize Utilities — XSS Protection Layer
 * FIX-18 #2: DOMPurify wrapper + textContent helpers
 */
'use strict';

/**
 * Safely set HTML content using DOMPurify
 * @param {HTMLElement} el - Target element
 * @param {string} html - HTML string to sanitize
 */
function safeHTML(el, html) {
  if (!el) return;
  if (typeof DOMPurify !== 'undefined') {
    el.innerHTML = DOMPurify.sanitize(html, { RETURN_TRUSTED_TYPE: false });
  } else {
    console.warn('[MesTech] DOMPurify not loaded — falling back to textContent');
    el.textContent = html;
  }
}

/**
 * Safely set text content (no HTML parsing)
 * @param {HTMLElement} el - Target element
 * @param {string} text - Plain text
 */
function safeText(el, text) {
  if (!el) return;
  el.textContent = text;
}

/**
 * Escape HTML entities to prevent XSS when inserting into innerHTML
 * Use when DOMPurify is not available and HTML structure is needed
 * @param {string} str - Raw string
 * @returns {string} HTML-escaped string
 */
function escapeHtml(str) {
  if (typeof str !== 'string') return '';
  return str
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#39;');
}
