// Clears placeholder text on focus and restores it on blur (if field is empty).
// Applies to any input/textarea with a `placeholder`.

(() => {
    function init(root = document) {
        const fields = root.querySelectorAll('input[placeholder], textarea[placeholder]');

        fields.forEach((el) => {
            if (!(el instanceof HTMLInputElement || el instanceof HTMLTextAreaElement)) return;

            // Avoid double-binding
            if (el.dataset.phcBound === '1') return;
            el.dataset.phcBound = '1';

            const original = el.getAttribute('placeholder') ?? '';
            el.dataset.phcOriginal = original;

            el.addEventListener('focus', () => {
                // Only clear visual hint, never touch user-typed value.
                el.setAttribute('placeholder', '');
            });

            el.addEventListener('blur', () => {
                if ((el.value || '').trim().length === 0) {
                    el.setAttribute('placeholder', el.dataset.phcOriginal ?? original);
                }
            });
        });
    }

    document.addEventListener('DOMContentLoaded', () => init());
})();
