(function () {
    const form = document.getElementById("editcvForm");
    if (!form) return;

    // Validering vid blur (ingen keyup) via HTML5 Constraint Validation API.

    /** @param {HTMLInputElement | HTMLTextAreaElement} el */
    function getMessage(el) {
        const v = el.validity;

        if (v.valueMissing) return el.dataset.valRequired || "Fältet är obligatoriskt.";
        if (v.typeMismatch) return el.dataset.valEmail || el.dataset.valUrl || "Ogiltigt format.";
        if (v.patternMismatch) return el.dataset.valPattern || "Ogiltigt format.";
        if (v.tooLong) return el.dataset.valMaxlength || "För långt värde.";
        if (v.tooShort) return el.dataset.valMinlength || "För kort värde.";

        return "";
    }

    /** @param {HTMLInputElement | HTMLTextAreaElement} el */
    function findErrorSpan(el) {
        const name = el.getAttribute("name");
        if (!name) return null;

        const selector = `span[data-valmsg-for="${CSS.escape(name)}"], span[class*="field-validation"][data-valmsg-for="${CSS.escape(name)}"]`;
        return form.querySelector(selector) ?? el.closest(".editcv-field")?.querySelector(".editcv-error") ?? null;
    }

    /** @param {HTMLInputElement | HTMLTextAreaElement} el */
    function setFieldState(el, isValid, message) {
        el.classList.toggle("input-validation-error", !isValid);
        el.classList.toggle("valid", isValid && (el.value || "").trim().length > 0);
        el.setAttribute("aria-invalid", isValid ? "false" : "true");

        const span = findErrorSpan(el);
        if (!span) return;

        span.textContent = message || "";
        span.classList.toggle("field-validation-error", !isValid);
        span.classList.toggle("field-validation-valid", isValid);
    }

    /** @param {HTMLInputElement | HTMLTextAreaElement} el */
    function validateField(el) {
        if (el.disabled) return true;
        if (el.type === "hidden") return true;
        if (el.hasAttribute("readonly")) return true;

        const ok = el.checkValidity();
        const msg = ok ? "" : getMessage(el);
        setFieldState(el, ok, msg);
        return ok;
    }

    function validateForm({ focusFirstInvalid } = { focusFirstInvalid: false }) {
        const fields = Array.from(form.querySelectorAll(".editcv-input, .editcv-textarea"));

        let ok = true;
        let firstInvalid = null;

        for (const field of fields) {
            const fieldOk = validateField(field);
            ok = fieldOk && ok;
            if (!fieldOk && !firstInvalid) firstInvalid = field;
        }

        if (!ok && focusFirstInvalid && firstInvalid) {
            firstInvalid.focus({ preventScroll: true });
            firstInvalid.scrollIntoView({ behavior: "smooth", block: "center" });
        }

        return ok;
    }

    form.addEventListener(
        "invalid",
        (e) => {
            // Prevent browser tooltip; we render our own messages inline.
            e.preventDefault();
        },
        true
    );

    form.addEventListener("focusout", (e) => {
        const target = e.target;

        if (!(target instanceof HTMLInputElement || target instanceof HTMLTextAreaElement)) return;
        if (!target.classList.contains("editcv-input") && !target.classList.contains("editcv-textarea")) return;

        validateField(target);
    });

    const autosaveStatus = document.getElementById("autosaveStatus");
    const statusText = autosaveStatus?.querySelector(".editcv-status-text");

    function setSaveState(state) {
        if (!autosaveStatus) return;

        autosaveStatus.dataset.state = state;

        if (!statusText) return;
        statusText.textContent = state === "saved" ? "Sparad" : "Ej sparad";
    }

    function markDirty() {
        setSaveState("dirty");
    }

    // Initialize text based on server-rendered data-state.
    if (autosaveStatus) {
        setSaveState(autosaveStatus.dataset.state === "saved" ? "saved" : "dirty");
    }

    function isDirty() {
        return autosaveStatus?.dataset.state === "dirty";
    }

    form.addEventListener("input", (e) => {
        // Read-only (konto) fält ska inte trigga "dirty".
        const t = e.target;
        if (t instanceof HTMLInputElement || t instanceof HTMLTextAreaElement) {
            if (t.hasAttribute("readonly")) return;
        }

        markDirty();
    });

    form.addEventListener("change", (e) => {
        const t = e.target;
        if (t instanceof HTMLInputElement || t instanceof HTMLTextAreaElement) {
            if (t.hasAttribute("readonly")) return;
        }

        markDirty();
    });

    const about = document.getElementById("aboutMe");
    const aboutCount = document.getElementById("aboutCount");

    function updateCount() {
        if (!about || !aboutCount) return;
        aboutCount.textContent = String(about.value.length);
    }

    updateCount();
    about?.addEventListener("input", updateCount);

    const avatarBtn = document.getElementById("avatarBtn");
    const avatarFile = document.getElementById("AvatarFile");
    const avatarPreview = document.getElementById("avatarPreview");

    avatarBtn?.addEventListener("click", () => avatarFile?.click());

    avatarFile?.addEventListener("change", (e) => {
        const file = e.target.files && e.target.files[0];
        if (!file) return;

        const reader = new FileReader();
        reader.onload = () => {
            if (avatarPreview) {
                avatarPreview.src = String(reader.result);
            }

            markDirty();
        };

        reader.readAsDataURL(file);
    });

    // Back navigation guard: don't let user leave with invalid required fields.
    const backBtn = document.getElementById("backBtn");
    backBtn?.addEventListener("click", (e) => {
        if (!isDirty()) return;

        const ok = window.confirm("Du har osparade ändringar – vill du lämna?\n\nTryck OK för att lämna eller Avbryt för att stanna kvar.");
        if (!ok) e.preventDefault();
    });

    // On submit, validate and show inline messages.
    form.addEventListener("submit", (e) => {
        const ok = validateForm({ focusFirstInvalid: true });
        if (!ok) {
            e.preventDefault();
        }
    });

    // Skills (pills + dedupe)
    const skillsJsonInput = document.getElementById("SkillsJson");
    const skillList = document.getElementById("skillList");
    const skillInput = document.getElementById("skillInput");
    const skillAddBtn = document.getElementById("skillAddBtn");

    /** @type {string[]} */
    let skills = [];

    function loadSkillsFromHidden() {
        if (!skillsJsonInput) return;

        try {
            const parsed = JSON.parse(skillsJsonInput.value || "[]");
            if (Array.isArray(parsed)) {
                skills = parsed.filter((s) => typeof s === "string");
            }
        } catch {
            skills = [];
        }

        skills = normalizeSkills(skills);
        syncSkillsJson();
    }

    function normalizeSkills(items) {
        const map = new Map();
        for (const raw of items) {
            const v = String(raw || "").trim();
            if (!v) continue;

            const key = v.toLocaleLowerCase();
            if (!map.has(key)) map.set(key, v);
        }

        return Array.from(map.values());
    }

    function syncSkillsJson() {
        if (!skillsJsonInput) return;
        skillsJsonInput.value = JSON.stringify(skills);
    }

    function renderSkills() {
        if (!skillList) return;
        skillList.innerHTML = "";

        if (skills.length === 0) {
            skillList.innerHTML = `<div class="editcv-help">Ingakompetenser ännu. Lägg till en kompetens ovan.</div>`;
            return;
        }

        skills.forEach((skill, idx) => {
            const pill = document.createElement("div");
            pill.className = "editcv-draft-card";

            const label = document.createElement("div");
            label.className = "editcv-draft-title";
            label.textContent = skill;

            const remove = document.createElement("button");
            remove.type = "button";
            remove.className = "editcv-draft-remove";
            remove.textContent = "×";
            remove.setAttribute("aria-label", "Ta bort kompetens");

            remove.addEventListener("click", () => {
                skills.splice(idx, 1);
                syncSkillsJson();
                renderSkills();
                markDirty();
            });

            pill.appendChild(label);
            pill.appendChild(remove);
            skillList.appendChild(pill);
        });
    }

    function tryAddSkill(text) {
        const v = String(text || "").trim();
        if (!v) return;

        skills = normalizeSkills([v, ...skills]);
        syncSkillsJson();
        renderSkills();
        markDirty();

        if (skillInput) skillInput.value = "";
    }

    skillAddBtn?.addEventListener("click", () => tryAddSkill(skillInput?.value));

    skillInput?.addEventListener("keydown", (e) => {
        if (e.key !== "Enter") return;
        e.preventDefault();
        tryAddSkill(skillInput.value);
    });

    loadSkillsFromHidden();
    renderSkills();

    // Education JSON + UI
    const eduJsonInput = document.getElementById("EducationJson");
    const eduList = document.getElementById("eduList");
    const eduToggleBtn = document.getElementById("eduToggleBtn");
    const eduFormWrap = document.getElementById("eduFormWrap");
    const eduAddBtn = document.getElementById("eduAddBtn");
    const eduCancelBtn = document.getElementById("eduCancelBtn");
    const eduMiniError = document.getElementById("eduMiniError");

    const eduSchool = document.getElementById("eduSchool");
    const eduProgram = document.getElementById("eduProgram");
    const eduYears = document.getElementById("eduYears");
    const eduNote = document.getElementById("eduNote");

    /** @type {{school:string, program:string, years:string, note?:string}[]} */
    let educations = [];

    function loadEduFromHidden() {
        if (!eduJsonInput) return;
        try {
            const parsed = JSON.parse(eduJsonInput.value || "[]");
            if (Array.isArray(parsed)) {
                educations = parsed;
            }
        } catch {
            educations = [];
        }

        syncEduJson();
    }

    function openEduForm(open) {
        if (!eduFormWrap) return;

        eduFormWrap.classList.toggle("is-open", open);
        eduFormWrap.setAttribute("aria-hidden", open ? "false" : "true");

        if (eduMiniError) {
            eduMiniError.textContent = "";
        }
    }

    eduToggleBtn?.addEventListener("click", () => {
        const open = !eduFormWrap?.classList.contains("is-open");
        openEduForm(open);
    });

    eduCancelBtn?.addEventListener("click", () => {
        clearEduMiniForm();
        openEduForm(false);
    });

    eduAddBtn?.addEventListener("click", () => {
        const school = (eduSchool?.value || "").trim();
        const program = (eduProgram?.value || "").trim();
        const years = (eduYears?.value || "").trim();
        const note = (eduNote?.value || "").trim();

        if (!school || !program || !years) {
            if (eduMiniError) eduMiniError.textContent = "Fyll i Skola, Program och År.";
            return;
        }

        educations.unshift({ school, program, years, note });
        syncEduJson();
        renderEdu();
        markDirty();

        clearEduMiniForm();
        openEduForm(false);
    });

    function clearEduMiniForm() {
        if (eduSchool) eduSchool.value = "";
        if (eduProgram) eduProgram.value = "";
        if (eduYears) eduYears.value = "";
        if (eduNote) eduNote.value = "";
        if (eduMiniError) eduMiniError.textContent = "";
    }

    function syncEduJson() {
        if (!eduJsonInput) return;
        eduJsonInput.value = JSON.stringify(educations);
    }

    function renderEdu() {
        if (!eduList) return;
        eduList.innerHTML = "";

        if (educations.length === 0) {
            eduList.innerHTML = `<div class="editcv-help">Inga utbildningar ännu. Klicka “Lägg till utbildning”.</div>`;
            return;
        }

        educations.forEach((education, idx) => {
            const card = document.createElement("div");
            card.className = "editcv-draft-card";

            const left = document.createElement("div");

            const title = document.createElement("div");
            title.className = "editcv-draft-title";
            title.textContent = education.school;

            const sub = document.createElement("div");
            sub.className = "editcv-draft-sub";
            sub.textContent = `${education.years} • ${education.program}${education.note ? " • " + education.note : ""}`;

            left.appendChild(title);
            left.appendChild(sub);

            const remove = document.createElement("button");
            remove.type = "button";
            remove.className = "editcv-draft-remove";
            remove.textContent = "×";
            remove.setAttribute("aria-label", "Ta bort utbildning");
            remove.addEventListener("click", () => {
                educations.splice(idx, 1);
                syncEduJson();
                renderEdu();
                markDirty();
            });

            card.appendChild(left);
            card.appendChild(remove);

            eduList.appendChild(card);
        });
    }

    loadEduFromHidden();
    renderEdu();

    // --- Projects picker modal (EditCV) ---
    const selectedProjectsJson = document.getElementById("SelectedProjectsJson");
    const modal = document.getElementById("editcv-projects-modal");
    const picker = document.getElementById("editcv-projects-picker");
    const counter = document.getElementById("editcv-projects-counter");

    const MAX_SELECTED_PROJECTS = 4;

    /** @type {{id:number,title:string,createdUtc:string}[]} */
    let projects = [];

    /** @type {Set<number>} */
    let selected = new Set();

    function parseSelectedIds() {
        if (!selectedProjectsJson) return [];
        try {
            const parsed = JSON.parse(selectedProjectsJson.value || "[]");
            if (Array.isArray(parsed)) return parsed.map((x) => Number(x)).filter((n) => Number.isFinite(n));
        } catch {
        }
        return [];
    }

    function syncSelectedJson() {
        if (!selectedProjectsJson) return;
        selectedProjectsJson.value = JSON.stringify(Array.from(selected));
    }

    function setModalOpen(open) {
        if (!modal) return;
        modal.setAttribute("aria-hidden", open ? "false" : "true");
        document.body.classList.toggle("editcv-modal-open", open);

        if (open) {
            const closeBtn = modal.querySelector("[data-editcv-modal-close]");
            closeBtn?.focus();
        }
    }

    function updatePickerUi() {
        if (!picker) return;

        const checkboxes = Array.from(picker.querySelectorAll('input[type="checkbox"]'));
        const checked = checkboxes.filter((c) => c.checked);
        const checkedCount = checked.length;

        if (counter) counter.textContent = `${checkedCount}/4 valda`;

        const limitReached = checkedCount >= MAX_SELECTED_PROJECTS;
        for (const cb of checkboxes) {
            if (cb.checked) {
                cb.disabled = false;
                cb.closest(".editcv-picker__row")?.classList.remove("is-disabled");
                continue;
            }

            cb.disabled = limitReached;
            cb.closest(".editcv-picker__row")?.classList.toggle("is-disabled", limitReached);
        }

        // Keep `selected` in sync with DOM.
        selected = new Set(checked.map((c) => Number(c.value)));
        syncSelectedJson();
    }

    function renderPicker() {
        if (!picker) return;
        picker.innerHTML = "";

        if (!projects || projects.length === 0) {
            picker.innerHTML = `<div class="editcv-picker__empty">Du har inga projekt ännu.</div>`;
            if (counter) counter.textContent = "0/4 valda";
            return;
        }

        for (const p of projects) {
            const row = document.createElement("label");
            row.className = "editcv-picker__row";

            const chk = document.createElement("input");
            chk.type = "checkbox";
            chk.className = "editcv-picker__chk";
            chk.value = String(p.id);
            chk.checked = selected.has(p.id);

            const content = document.createElement("span");
            content.className = "editcv-picker__content";

            const title = document.createElement("span");
            title.className = "editcv-picker__title";
            title.textContent = p.title;

            const meta = document.createElement("span");
            meta.className = "editcv-picker__meta";
            meta.textContent = p.createdUtc;

            content.appendChild(title);
            content.appendChild(meta);

            row.appendChild(chk);
            row.appendChild(content);

            picker.appendChild(row);
        }

        updatePickerUi();
    }

    // `projects` comes from server-rendered JSON in the hidden input area.
    // We piggyback on existing hidden field content by embedding JSON into data attributes.
    // (We create these data attributes in the view.)
    const projectsDataEl = document.querySelector("[data-editcv-projects-json]");
    if (projectsDataEl instanceof HTMLElement) {
        try {
            const raw = projectsDataEl.getAttribute("data-editcv-projects-json") || "[]";
            const parsed = JSON.parse(raw);
            if (Array.isArray(parsed)) {
                projects = parsed
                    .map((x) => ({
                        id: Number(x.id),
                        title: String(x.title || ""),
                        createdUtc: String(x.createdUtc || "")
                    }))
                    .filter((x) => Number.isFinite(x.id) && x.title);
            }
        } catch {
            projects = [];
        }
    }

    selected = new Set(parseSelectedIds().slice(0, MAX_SELECTED_PROJECTS));
    syncSelectedJson();
    renderPicker();

    document.addEventListener("click", (e) => {
        const t = e.target;
        if (!(t instanceof HTMLElement)) return;

        const openBtn = t.closest("[data-editcv-modal-open]");
        if (openBtn) {
            const id = openBtn.getAttribute("data-editcv-modal-open");
            if (id === "editcv-projects-modal") {
                setModalOpen(true);
                updatePickerUi();
            }
            return;
        }

        if (t.closest("[data-editcv-modal-close]")) {
            if (modal?.getAttribute("aria-hidden") === "false") {
                setModalOpen(false);
            }
        }
    });

    document.addEventListener("keydown", (e) => {
        if (e.key === "Escape" && modal?.getAttribute("aria-hidden") === "false") {
            setModalOpen(false);
        }
    });

    picker?.addEventListener("change", (e) => {
        const t = e.target;
        if (!(t instanceof HTMLInputElement)) return;
        if (t.type !== "checkbox") return;

        // Enforce limit immediately.
        const checked = Array.from(picker.querySelectorAll('input[type="checkbox"]')).filter((c) => c.checked);
        if (checked.length > MAX_SELECTED_PROJECTS) {
            t.checked = false;
        }

        updatePickerUi();
        markDirty();
    });
})();
