(function () {
    // UI-hjälpare för formulär som opt-in:ar med `data-validation-ui="true"`.
    // Visar en ikon (check/fel) baserat på HTML5-validering + eventuella servermeddelanden.

    /** @param {HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement} input */
    function ensureWrapper(input) {
        const parent = input.parentElement;

        if (parent && parent.classList.contains("field-wrap")) {
            return parent;
        }

        const wrapper = document.createElement("div");
        wrapper.className = "field-wrap";

        input.parentNode.insertBefore(wrapper, input);
        wrapper.appendChild(input);

        const icon = document.createElement("img");
        icon.className = "field-status-icon";
        icon.alt = "";
        icon.setAttribute("aria-hidden", "true");
        wrapper.appendChild(icon);

        return wrapper;
    }

    /** @param {HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement} input */
    function getValidationMessageElement(input) {
        const name = input.getAttribute("name");
        if (!name) return null;

        return document.querySelector(`[data-valmsg-for="${CSS.escape(name)}"]`);
    }

    /** @param {HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement} input */
    function updateState(input) {
        const wrapper = ensureWrapper(input);
        const icon = wrapper.querySelector(".field-status-icon");
        const messageElement = getValidationMessageElement(input);

        const isEmpty = !input.value || input.value.trim().length === 0;
        const isRequired = input.hasAttribute("data-val-required") || input.required;

        const hasServerMessage =
            !!messageElement && !!messageElement.textContent && messageElement.textContent.trim().length > 0;

        const htmlValid = input.checkValidity();

        const isValid = !hasServerMessage && htmlValid && (!isRequired || !isEmpty);
        const isInvalid = hasServerMessage || (!isEmpty && !htmlValid) || (isRequired && isEmpty);

        wrapper.classList.toggle("is-valid", isValid);
        wrapper.classList.toggle("is-invalid", isInvalid && !isValid);

        if (!icon) {
            return;
        }

        if (isValid) {
            icon.src = "/images/svg/icons/checkmark.svg";
            icon.style.display = "block";
            return;
        }

        if (isInvalid) {
            icon.src = "/images/svg/icons/error.svg";
            icon.style.display = "block";
            return;
        }

        icon.style.display = "none";
    }

    /** @param {HTMLFormElement} form */
    function wireForm(form) {
        const inputs = form.querySelectorAll("input, textarea, select");

        inputs.forEach((input) => {
            ensureWrapper(input);

            input.addEventListener("input", () => {
                updateState(input);
            });

            input.addEventListener("blur", () => {
                updateState(input);
            });

            input.addEventListener("focus", () => {
                input.classList.add("is-focused");
            });

            input.addEventListener("focusout", () => {
                input.classList.remove("is-focused");
            });

            updateState(input);
        });

        form.addEventListener("submit", () => {
            setTimeout(() => {
                inputs.forEach(updateState);
            }, 0);
        });
    }

    document.addEventListener("DOMContentLoaded", () => {
        document
            .querySelectorAll("form[data-validation-ui=\"true\"]")
            .forEach((form) => wireForm(form));
    });
})();
