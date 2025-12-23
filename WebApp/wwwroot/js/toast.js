(() => {
  const toast = document.querySelector('[data-toast]');
  if (!toast) return;

  const closeBtn = toast.querySelector('[data-toast-close]');
  const dismiss = () => {
    toast.remove();
  };

  closeBtn?.addEventListener('click', dismiss);

  // Auto-hide after 6 seconds
  window.setTimeout(dismiss, 6000);
})();
