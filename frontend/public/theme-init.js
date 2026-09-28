// Applies a stored light/dark choice before first paint (external file: the CSP forbids inline scripts).
(function () {
  try {
    var theme = localStorage.getItem('devinsight.theme');
    if (theme === 'light' || theme === 'dark') {
      document.documentElement.setAttribute('data-theme', theme);
    }
  } catch (e) {
    // Storage unavailable: fall back to prefers-color-scheme.
  }
})();
