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

    // Back navigation guard
    const backBtn = document.getElementById("backBtn");
    const bottomBar = document.querySelector(".editcv-bottom-inner");

    function ensureLeaveRow() {
        if (!bottomBar) return null;

        let row = document.getElementById("editcvLeaveRow");
        if (row) return row;

        row = document.createElement("div");
        row.id = "editcvLeaveRow";
        row.style.display = "none";
        row.style.marginLeft = "10px";
        row.style.marginRight = "10px";
        row.style.flex = "1 1 auto";

        row.innerHTML = `
            <div style="display:flex;align-items:center;gap:10px;flex-wrap:wrap;justify-content:flex-start;">
                <span style="font-weight:850;font-size:12px;color:rgba(0,0,0,0.72);">Du har osparade ändringar. Lämna ändå?</span>
                <a class="btn-primary-custom" href="#" id="editcvLeaveConfirm" style="text-decoration:none;">Lämna</a>
                <button type="button" class="btn-secondary-custom" id="editcvLeaveCancel">Stanna</button>
            </div>`;

        bottomBar.insertBefore(row, bottomBar.children[1] ?? null);

        const cancel = row.querySelector("#editcvLeaveCancel");
        cancel?.addEventListener("click", () => {
            row.style.display = "none";
        });

        return row;
    }

    backBtn?.addEventListener("click", (e) => {
        if (!isDirty()) return;

        e.preventDefault();

        const row = ensureLeaveRow();
        if (!row) return;

        const confirmLink = row.querySelector("#editcvLeaveConfirm");
        if (confirmLink instanceof HTMLAnchorElement && backBtn instanceof HTMLAnchorElement) {
            confirmLink.href = backBtn.href;
        }

        row.style.display = "block";
    });

    form.addEventListener("submit", (e) => {
        const ok = validateForm({ focusFirstInvalid: true });
        if (!ok) e.preventDefault();
    });

    // --- Education JSON + UI ---
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

        if (eduMiniError) eduMiniError.textContent = "";
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

    // --- Work experience JSON + UI ---
    const expJsonInput = document.getElementById("ExperienceJson");
    const expList = document.getElementById("expList");
    const expToggleBtn = document.getElementById("expToggleBtn");
    const expFormWrap = document.getElementById("expFormWrap");
    const expAddBtn = document.getElementById("expAddBtn");
    const expCancelBtn = document.getElementById("expCancelBtn");
    const expMiniError = document.getElementById("expMiniError");

    const expCompany = document.getElementById("expCompany");
    const expRole = document.getElementById("expRole");
    const expYears = document.getElementById("expYears");
    const expDesc = document.getElementById("expDesc");

    /** @type {{company:string, role:string, years:string, description?:string}[]} */
    let experiences = [];

    function loadExpFromHidden() {
        if (!expJsonInput) return;
        try {
            const parsed = JSON.parse(expJsonInput.value || "[]");
            if (Array.isArray(parsed)) {
                experiences = parsed;
            }
        } catch {
            experiences = [];
        }
        syncExpJson();
    }

    function openExpForm(open) {
        if (!expFormWrap) return;

        expFormWrap.classList.toggle("is-open", open);
        expFormWrap.setAttribute("aria-hidden", open ? "false" : "true");

        if (expMiniError) expMiniError.textContent = "";
    }

    expToggleBtn?.addEventListener("click", () => {
        const open = !expFormWrap?.classList.contains("is-open");
        openExpForm(open);
    });

    expCancelBtn?.addEventListener("click", () => {
        clearExpMiniForm();
        openExpForm(false);
    });

    expAddBtn?.addEventListener("click", () => {
        const company = (expCompany?.value || "").trim();
        const role = (expRole?.value || "").trim();
        const years = (expYears?.value || "").trim();
        const description = (expDesc?.value || "").trim();

        if (!company || !role || !years) {
            if (expMiniError) expMiniError.textContent = "Fyll i Företag, Roll och År.";
            return;
        }

        experiences.unshift({ company, role, years, description });
        syncExpJson();
        renderExp();
        markDirty();

        clearExpMiniForm();
        openExpForm(false);
    });

    function clearExpMiniForm() {
        if (expCompany) expCompany.value = "";
        if (expRole) expRole.value = "";
        if (expYears) expYears.value = "";
        if (expDesc) expDesc.value = "";
        if (expMiniError) expMiniError.textContent = "";
    }

    function syncExpJson() {
        if (!expJsonInput) return;
        expJsonInput.value = JSON.stringify(experiences);
    }

    function renderExp() {
        if (!expList) return;
        expList.innerHTML = "";

        if (experiences.length === 0) {
            expList.innerHTML = `<div class="editcv-help">Inga erfarenheter ännu. Klicka “Lägg till erfarenhet”.</div>`;
            return;
        }

        experiences.forEach((ex, idx) => {
            const card = document.createElement("div");
            card.className = "editcv-draft-card";

            const left = document.createElement("div");

            const title = document.createElement("div");
            title.className = "editcv-draft-title";
            title.textContent = ex.company;

            const sub = document.createElement("div");
            sub.className = "editcv-draft-sub";
            sub.textContent = `${ex.years} • ${ex.role}${ex.description ? " • " + ex.description : ""}`;

            left.appendChild(title);
            left.appendChild(sub);

            const remove = document.createElement("button");
            remove.type = "button";
            remove.className = "editcv-draft-remove";
            remove.textContent = "×";
            remove.setAttribute("aria-label", "Ta bort erfarenhet");
            remove.addEventListener("click", () => {
                experiences.splice(idx, 1);
                syncExpJson();
                renderExp();
                markDirty();
            });

            card.appendChild(left);
            card.appendChild(remove);

            expList.appendChild(card);
        });
    }

    loadExpFromHidden();
    renderExp();

    // --- Projects picker modal (EditCV) + live preview ---
    const selectedProjectsJson = document.getElementById("SelectedProjectsJson");
    const modal = document.getElementById("editcv-projects-modal");
    const picker = document.getElementById("editcv-projects-picker");
    const counter = document.getElementById("editcv-projects-counter");
    const preview = document.getElementById("editcvProjectPreview");

    const MAX_SELECTED_PROJECTS = 4;

    /** @type {{id:number,title:string,createdUtc:string,imagePath?:string,shortDescription?:string,techKeysCsv?:string,createdByName?:string,createdByEmail?:string}[]} */
    let projects = [];

    /** @type {Set<number>} */
    let selected = new Set();

    function parseSelectedIds() {
        if (!selectedProjectsJson) return [];
        try {
            const parsed = JSON.parse(selectedProjectsJson.value || "[]");
            if (Array.isArray(parsed)) return parsed.map((x) => Number(x)).filter((n) => Number.isFinite(n));
        } catch { }
        return [];
    }

    function syncSelectedJson() {
        if (!selectedProjectsJson) return;
        selectedProjectsJson.value = JSON.stringify(Array.from(selected));
    }

    function escapeHtml(s) {
        return String(s || "")
            .replaceAll("&", "&amp;")
            .replaceAll("<", "&lt;")
            .replaceAll(">", "&gt;")
            .replaceAll('"', "&quot;")
            .replaceAll("'", "&#39;");
    }

    function renderPreview() {
        if (!preview) return;

        const selectedList = Array.from(selected);
        if (selectedList.length === 0) {
            preview.innerHTML = `<div class="editcv-help" style="margin-top:10px;">Inga projekt valda ännu.</div>`;
            return;
        }

        preview.innerHTML = `
            <div class="mycv-projects-grid">
                ${selectedList
                .slice(0, MAX_SELECTED_PROJECTS)
                .map((id) => {
                    const p = projects.find((x) => x.id === id);
                    if (!p) return "";

                    const imgSrc = (p.imagePath || "").trim() || "/images/projects/rocketship.png";
                    const shortDesc = (p.shortDescription || "").trim();
                    const techKeys = String(p.techKeysCsv || "")
                        .split(",")
                        .map((s) => s.trim())
                        .filter(Boolean)
                        .slice(0, 4);

                    const creator = (p.createdByName || "").trim() || (p.createdByEmail || "").trim() || "Okänd";

                    return `
                            <div class="mycv-project-card" role="group" aria-label="Förhandsvisning projekt: ${escapeHtml(p.title)}">
                                <div class="mycv-project-doc">
                                    <div class="mycv-project-doc-top">
                                        <div class="mycv-project-doc-grid">
                                            <div class="mycv-project-doc-avatar" aria-hidden="true">
                                                <img src="${escapeHtml(imgSrc)}" alt="" />
                                            </div>

                                            <div class="mycv-project-doc-center">
                                                <div class="mycv-project-doc-title">${escapeHtml(p.title)}</div>
                                                <div class="mycv-project-doc-createdby">Skapare: ${escapeHtml(creator)}</div>
                                                <div class="mycv-project-doc-meta">Skapad: ${escapeHtml(p.createdUtc)}</div>
                                            </div>
                                        </div>
                                    </div>

                                    <div class="mycv-project-doc-body">
                                        <div class="mycv-project-pill${shortDesc ? "" : " mycv-project-pill--muted"}">
                                            <div class="mycv-project-pill__text">${escapeHtml(shortDesc || "Ingen kort beskrivning.")}</div>
                                        </div>

                                        ${techKeys.length
                            ? `
                                            <div class="mycv-project-doc-bottom">
                                                <div class="mycv-project-doc-sep" aria-hidden="true"></div>
                                                <div class="mycv-project-doc-tech" aria-label="Tech stack">
                                                    ${techKeys
                                .map(
                                    (k) =>
                                        `<span class="mycv-project-doc-tech-tile"><img class="mycv-project-doc-tech-icon" src="/images/svg/techstack/${encodeURIComponent(k)}.svg" alt="${escapeHtml(k)}" /></span>`
                                )
                                .join("")}
                                                </div>
                                            </div>`
                            : ""}
                                    </div>
                                </div>
                            </div>`;
                })
                .join("")}
            </div>`;
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

        selected = new Set(checked.map((c) => Number(c.value)));
        syncSelectedJson();
        renderPreview();
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
                        createdUtc: String(x.createdUtc || ""),
                        imagePath: x.imagePath ? String(x.imagePath) : "",
                        shortDescription: x.shortDescription ? String(x.shortDescription) : "",
                        techKeysCsv: x.techKeysCsv ? String(x.techKeysCsv) : "",
                        createdByName: x.createdByName ? String(x.createdByName) : "",
                        createdByEmail: x.createdByEmail ? String(x.createdByEmail) : ""
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
    renderPreview();

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

        const checked = Array.from(picker.querySelectorAll('input[type="checkbox"]')).filter((c) => c.checked);
        if (checked.length > MAX_SELECTED_PROJECTS) {
            t.checked = false;
        }

        updatePickerUi();
        markDirty();
    });

    // --- KOMPETENSVÄLJARE ---
    const compModal = document.getElementById("competenceModal");
    const compPicker = document.getElementById("editcv-competences-picker");
    const compCounter = document.getElementById("editcv-competences-counter");
    const compSearch = document.getElementById("competenceSearch");
    const selectedContainer = document.getElementById("selectedCompetenceIdsContainer");

    const openCompBtn = document.getElementById("openCompetenceModal");
    const closeCompBtn = document.getElementById("closeCompetenceModal");
    const closeCompBtnFooter = document.getElementById("closeCompetenceModalFooter");
    const clearCompBtn = document.getElementById("clearCompetencesBtn");
    const compBackdrop = compModal ? compModal.querySelector(".editcv-modal__backdrop") : null;

    const MAX_COMPETENCES = 10;

    // Viktigt: init-flagga så vi inte triggar dirty vid sidladdning/öppna modal
    let compInit = true;

    function setCompModalOpen(open) {
        if (!compModal) return;
        compModal.setAttribute("aria-hidden", open ? "false" : "true");
        compModal.classList.toggle("is-open", open);
        compModal.style.display = open ? "block" : "none";
        document.body.classList.toggle("editcv-modal-open", open);

        if (open) {
            const focusEl = compModal.querySelector(".editcv-modal__close");
            focusEl?.focus();
        }
    }

    function getAllCompetenceCheckboxes() {
        return Array.from(compPicker?.querySelectorAll("input.competence-chk") ?? []);
    }

    function getUniqueSelectedIds() {
        const seen = new Set();
        for (const cb of getAllCompetenceCheckboxes()) {
            if (!(cb instanceof HTMLInputElement)) continue;
            if (!cb.checked) continue;
            seen.add(cb.value);
        }
        return seen;
    }

    function getCheckedNames() {
        const seen = new Set();
        const names = [];
        for (const cb of getAllCompetenceCheckboxes()) {
            if (!(cb instanceof HTMLInputElement)) continue;
            if (!cb.checked) continue;
            if (seen.has(cb.value)) continue;
            seen.add(cb.value);
            const name = (cb.closest(".editcv-picker__row")?.getAttribute("data-name") || "").trim();
            if (name) names.push(name);
        }
        return names;
    }

    function syncSameIdCheckboxes(value, isChecked) {
        const target = getAllCompetenceCheckboxes().filter(cb => cb.value === value);
        target.forEach(cb => { cb.checked = isChecked; });
    }

    function rebuildHiddenIds() {
        if (!selectedContainer) return;
        selectedContainer.innerHTML = "";
        for (const id of getUniqueSelectedIds()) {
            const input = document.createElement("input");
            input.type = "hidden";
            input.name = "SelectedCompetenceIds";
            input.value = id;
            selectedContainer.appendChild(input);
        }
    }

    function updateCompetenceCounter() {
        if (!compCounter) return;
        const count = getUniqueSelectedIds().size;
        compCounter.textContent = `${count}`;
    }

    function renderCompetencePills() {
        const host = document.getElementById("competencePills");
        if (!host) return;

        const names = getCheckedNames();
        const top = names.slice(0, 4);
        const rest = names.length - top.length;

        const grid = document.createElement("div");
        grid.className = "cv-skill-grid";

        if (names.length === 0) {
            grid.innerHTML = `<span class="cv-paragraph cv-paragraph-muted">Inga kompetenser ännu.</span>`;
        } else {
            for (const s of top) {
                const pill = document.createElement("span");
                pill.className = "cv-skill-pill";
                pill.textContent = s;
                grid.appendChild(pill);
            }
            if (rest > 0) {
                const more = document.createElement("span");
                more.className = "cv-skill-pill cv-skill-pill--muted";
                more.textContent = `+${rest} fler`;
                grid.appendChild(more);
            }
        }

        host.innerHTML = "";
        host.appendChild(grid);
    }

    function enforceCompetenceLimit(changedValue) {
        const uniqueSelected = getUniqueSelectedIds();
        const limitReached = uniqueSelected.size >= MAX_COMPETENCES;

        getAllCompetenceCheckboxes().forEach(cb => {
            const row = cb.closest(".editcv-picker__row");
            if (cb.checked) {
                cb.disabled = false;
                row?.classList.remove("is-disabled");
            } else {
                const shouldDisable = limitReached && !uniqueSelected.has(cb.value);
                cb.disabled = shouldDisable;
                row?.classList.toggle("is-disabled", shouldDisable);
            }
        });

        if (limitReached && changedValue && !uniqueSelected.has(changedValue)) {
            syncSameIdCheckboxes(changedValue, false);
        }
    }

    function updateCompetenceUi() {
        rebuildHiddenIds();
        updateCompetenceCounter();
        renderCompetencePills();

        if (!compInit) markDirty();
    }

    function openCompetenceModal() {
        compInit = true;
        setCompModalOpen(true);
        enforceCompetenceLimit();
        updateCompetenceUi();
        compInit = false;
    }

    function closeCompetenceModal() {
        setCompModalOpen(false);
    }

    openCompBtn?.addEventListener("click", openCompetenceModal);
    closeCompBtn?.addEventListener("click", closeCompetenceModal);
    closeCompBtnFooter?.addEventListener("click", () => {
        // Spara valen till hidden och stäng
        updateCompetenceUi();
        closeCompetenceModal();
    });
    if (compBackdrop instanceof HTMLElement) compBackdrop.addEventListener("click", closeCompetenceModal);

    document.addEventListener("keydown", (e) => {
        if (e.key === "Escape" && compModal?.getAttribute("aria-hidden") === "false") {
            closeCompetenceModal();
        }
    });

    compPicker?.addEventListener("change", (e) => {
        const t = e.target;
        if (!(t instanceof HTMLInputElement)) return;
        if (!t.classList.contains("competence-chk")) return;

        syncSameIdCheckboxes(t.value, t.checked);

        // Stoppa 11:e unika valet
        const uniqueSelected = getUniqueSelectedIds();
        if (uniqueSelected.size > MAX_COMPETENCES) {
            syncSameIdCheckboxes(t.value, false);
        }

        enforceCompetenceLimit(t.value);
        updateCompetenceUi();
    });

    clearCompBtn?.addEventListener("click", () => {
        getAllCompetenceCheckboxes().forEach(cb => { cb.checked = false; cb.disabled = false; cb.closest(".editcv-picker__row")?.classList.remove("is-disabled"); });
        compInit = false;
        updateCompetenceUi();
    });

    compSearch?.addEventListener("input", () => {
        const q = compSearch.value.trim().toLowerCase();
        const rows = compPicker?.querySelectorAll(".editcv-picker__row") ?? [];
        rows.forEach((row) => {
            const name = (row.getAttribute("data-name") || "").toLowerCase();
            row.style.display = name.includes(q) ? "" : "none";
        });
    });

    // Init kompetenser utan dirty
    compInit = true;
    enforceCompetenceLimit();
    updateCompetenceUi();
    compInit = false;

})();
