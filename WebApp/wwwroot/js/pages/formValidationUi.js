// Generic UI helper for inputs using ASP.NET unobtrusive validation.
// Adds checkmark/error icons to the right of the input depending on validity.
// Only runs on forms that opt-in with: data-validation-ui="true"

(function () {
    function ensureWrapper(input) {
        var parent = input.parentElement;

        if (parent && parent.classList.contains('field-wrap')) {
            return parent;
        }

        var wrapper = document.createElement('div');
        wrapper.className = 'field-wrap';

        input.parentNode.insertBefore(wrapper, input);
        wrapper.appendChild(input);

        var icon = document.createElement('img');
        icon.className = 'field-status-icon';
        icon.alt = '';
        icon.setAttribute('aria-hidden', 'true');
        wrapper.appendChild(icon);

        return wrapper;
    }

    function getValidationMessage(input) {
        var name = input.getAttribute('name');
        if (!name) return null;

        return document.querySelector('[data-valmsg-for="' + CSS.escape(name) + '"]');
    }

    function updateState(input) {
        var wrapper = ensureWrapper(input);
        var icon = wrapper.querySelector('.field-status-icon');
        var msg = getValidationMessage(input);

        var isEmpty = !input.value || input.value.trim().length === 0;
        var required = input.hasAttribute('data-val-required') || input.required;

        var hasServerMessage = msg && msg.textContent && msg.textContent.trim().length > 0;
        var htmlValid = input.checkValidity();

        var isValid = !hasServerMessage && htmlValid && (!required || !isEmpty);
        var isInvalid = hasServerMessage || (!isEmpty && !htmlValid) || (required && isEmpty);

        wrapper.classList.toggle('is-valid', isValid);
        wrapper.classList.toggle('is-invalid', isInvalid && !isValid);

        if (isValid) {
            icon.src = '/images/svg/icons/checkmark.svg';
            icon.style.display = 'block';
        } else if (isInvalid) {
            icon.src = '/images/svg/icons/error.svg';
            icon.style.display = 'block';
        } else {
            icon.style.display = 'none';
        }
    }

    function wireForm(form) {
        var inputs = form.querySelectorAll('input, textarea, select');

        inputs.forEach(function (input) {
            ensureWrapper(input);

            input.addEventListener('input', function () {
                updateState(input);
            });

            input.addEventListener('blur', function () {
                updateState(input);
            });

            input.addEventListener('focus', function () {
                input.classList.add('is-focused');
            });

            input.addEventListener('focusout', function () {
                input.classList.remove('is-focused');
            });

            // initial
            updateState(input);
        });

        form.addEventListener('submit', function () {
            setTimeout(function () {
                inputs.forEach(updateState);
            }, 0);
        });
    }

    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('form[data-validation-ui="true"]').forEach(wireForm);
    });
})();
