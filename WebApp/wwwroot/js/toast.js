(() => {
  const toasts = document.querySelectorAll('[data-toast]');
  if (!toasts || toasts.length === 0) return;

  toasts.forEach((toast) => {
    const closeBtns = toast.querySelectorAll('[data-toast-close]');

    const dismiss = () => {
      toast.classList.add('toast--out');
      // Allow CSS transition to play if present.
      window.setTimeout(() => toast.remove(), 140);
    };

    closeBtns.forEach((b) => b.addEventListener('click', dismiss));

    // Auto-hide after 6 seconds
    window.setTimeout(dismiss, 6000);
  });
})();
