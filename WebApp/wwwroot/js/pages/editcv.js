(function () {
    const form = document.getElementById("editcvForm");
    if (!form) return;

    // ---------- Blur-only validation (no keyup) ----------
    // This implementation uses HTML5 constraint validation APIs so it works without jQuery.
    // We set custom messages (sv) using data attributes emitted by the view.

    /** @param {HTMLInputElement|HTMLTextAreaElement} el */
    function getMessage(el) {
        const v = el.validity;
        if (v.valueMissing) return el.dataset.valRequired || "Fältet är obligatoriskt.";
        if (v.typeMismatch) return el.dataset.valEmail || el.dataset.valUrl || "Ogiltigt format.";
        if (v.patternMismatch) return el.dataset.valPattern || "Ogiltigt format.";
        if (v.tooLong) return el.dataset.valMaxlength || "För långt värde.";
        if (v.tooShort) return el.dataset.valMinlength || "För kort värde.";
        return "";
    }

    /** @param {HTMLInputElement|HTMLTextAreaElement} el */
    function findErrorSpan(el) {
        // Map by name to the asp-validation-for span classes
        const name = el.getAttribute("name");
        if (!name) return null;

        // If property is nested, MVC uses underscores in class names.
        const cls = "field-validation-valid";
        const selector = `span[data-valmsg-for="${CSS.escape(name)}"], span[class*="field-validation"][data-valmsg-for="${CSS.escape(name)}"]`;
        return form.querySelector(selector) || el.closest(".editcv-field")?.querySelector(".editcv-error") || null;
    }

    /** @param {HTMLInputElement|HTMLTextAreaElement} el */
    function setFieldState(el, isValid, message) {
        el.classList.toggle("input-validation-error", !isValid);
        el.classList.toggle("valid", isValid && (el.value || "").trim().length > 0);
        el.setAttribute("aria-invalid", isValid ? "false" : "true");

        const span = findErrorSpan(el);
        if (span) {
            span.textContent = message || "";
            span.classList.toggle("field-validation-error", !isValid);
            span.classList.toggle("field-validation-valid", isValid);
        }
    }

    /** @param {HTMLInputElement|HTMLTextAreaElement} el */
    function validateField(el) {
        // Do not validate disabled/hidden fields
        if (el.disabled) return true;
        if (el.type === "hidden") return true;

        const ok = el.checkValidity();
        const msg = ok ? "" : getMessage(el);
        setFieldState(el, ok, msg);
        return ok;
    }

    function validateForm() {
        const fields = Array.from(form.querySelectorAll(".editcv-input, .editcv-textarea"));
        let ok = true;
        for (const f of fields) {
            // Only enforce after interaction (blur triggers); but for button state we can still evaluate
            ok = validateField(f) && ok;
        }
        return ok;
    }

    // Prevent browser default tooltip bubbles
    form.addEventListener("invalid", (e) => {
        e.preventDefault();
    }, true);

    // Validate on blur only
    form.addEventListener("focusout", (e) => {
        const target = e.target;
        if (!(target instanceof HTMLInputElement || target instanceof HTMLTextAreaElement)) return;
        if (!target.classList.contains("editcv-input") && !target.classList.contains("editcv-textarea")) return;
        validateField(target);
        updateSaveButtons();
    });

    // ---------- Autosave indicator (UI-only) ----------
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

    // Mark dirty on any input change
    form.addEventListener("input", markDirty);
    form.addEventListener("change", markDirty);

    // ---------- AboutMe counter ----------
    const about = document.getElementById("aboutMe");
    const aboutCount = document.getElementById("aboutCount");

    function updateCount() {
        if (!about || !aboutCount) return;
        aboutCount.textContent = String(about.value.length);
    }
    updateCount();
    about?.addEventListener("input", updateCount);

    // ---------- Profile image uploader ----------
    const avatarBtn = document.getElementById("avatarBtn");
    const avatarFile = document.getElementById("AvatarFile");
    const avatarPreview = document.getElementById("avatarPreview");
    const avatarFallback = document.getElementById("avatarFallback");

    avatarBtn?.addEventListener("click", () => avatarFile?.click());

    avatarFile?.addEventListener("change", (e) => {
        const file = e.target.files && e.target.files[0];
        if (!file) return;

        const reader = new FileReader();
        reader.onload = () => {
            if (avatarPreview) {
                avatarPreview.src = String(reader.result);
                avatarPreview.style.display = "block";
            }
            if (avatarFallback) avatarFallback.style.display = "none";
            markDirty();
        };
        reader.readAsDataURL(file);
    });

    // ---------- Education (staged saving) ----------
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

    let educations = [];

    // Placeholder start
    educations = [
        { school: "Örebro universitet", program: "Systemvetenskap • Webbutveckling", years: "2024 – Pågående", note: "" }
    ];
    syncEduJson();
    renderEdu();

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

        educations.forEach((e, idx) => {
            const card = document.createElement("div");
            card.className = "editcv-draft-card";

            const left = document.createElement("div");
            const title = document.createElement("div");
            title.className = "editcv-draft-title";
            title.textContent = e.school;

            const sub = document.createElement("div");
            sub.className = "editcv-draft-sub";
            sub.textContent = `${e.years} • ${e.program}${e.note ? " • " + e.note : ""}`;

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

    // ---------- Projects picker (gallery) ----------
    const projectGrid = document.getElementById("projectGrid");
    const projectSearch = document.getElementById("projectSearch");
    const selectedProjectsJson = document.getElementById("SelectedProjectsJson");
    const manageProjectsBtn = document.getElementById("manageProjectsBtn");

    // Placeholder project library
    const projects = [
        {
            id: "p1",
            title: "NotLinkedIn — WebApp",
            desc: "Webbplattform där användare skapar CV-profiler, kopplar projekt och kan kommunicera via privata meddelanden. Ren Apple-ish UI och tydlig struktur.",
            tech: ["csharp", "mysql", "mongodb"]
        },
        {
            id: "p2",
            title: "AES GUI — Kryptering",
            desc: "GUI-app som krypterar/dekrypterar text med tydlig input/feedback, validering och robust felhantering. Byggd för att kunna skalas upp.",
            tech: ["csharp", "cplusplus"]
        },
        {
            id: "p3",
            title: "Linux & Nätverk — IPv6/Wireshark",
            desc: "Rapport + labb där trafik analyseras i hemmanätverk. Fokus på adressering, protokoll och verifiering av paketflöden.",
            tech: ["python", "java"]
        },
        {
            id: "p4",
            title: "Mini Dashboard — Frontend/Logik",
            desc: "Liten demo för att visa struktur: komponenter, state och tydlig dataflödeslogik. Byggd för att vara lätt att vidareutveckla.",
            tech: ["javascript", "python"]
        }
    ];

    // Default selection (placeholder)
    let selected = new Set(["p1", "p2", "p3"]);

    function syncSelectedProjects() {
        if (!selectedProjectsJson) return;
        selectedProjectsJson.value = JSON.stringify(Array.from(selected));
    }

    function techIconPath(key) {
        // folder: wwwroot/images/svg/techstack/<name>.svg
        // key must match your filenames (csharp.svg, mysql.svg, mongodb.svg, etc)
        return `/images/svg/techstack/${key}.svg`;
    }

    function renderProjects(filter = "") {
        if (!projectGrid) return;
        projectGrid.innerHTML = "";

        const f = filter.trim().toLowerCase();
        const filtered = !f
            ? projects
            : projects.filter(p =>
                (p.title + " " + p.desc).toLowerCase().includes(f)
            );

        filtered.forEach(p => {
            const card = document.createElement("div");
            card.className = "editcv-project-card";
            const isOn = selected.has(p.id);
            card.classList.toggle("is-off", !isOn);

            const toggleWrap = document.createElement("div");
            toggleWrap.className = "editcv-project-toggle";

            const toggle = document.createElement("div");
            toggle.className = "editcv-toggle";
            toggle.dataset.on = String(isOn);

            const knob = document.createElement("div");
            knob.className = "editcv-toggle-knob";

            toggle.appendChild(knob);

            toggle.addEventListener("click", () => {
                const currentlyOn = selected.has(p.id);
                if (currentlyOn) selected.delete(p.id);
                else selected.add(p.id);

                syncSelectedProjects();
                renderProjects(projectSearch?.value || "");
                markDirty();
            });

            toggleWrap.appendChild(toggle);

            const title = document.createElement("h3");
            title.className = "editcv-project-title";
            title.textContent = p.title;

            const desc = document.createElement("p");
            desc.className = "editcv-project-desc";
            desc.textContent = p.desc;

            const divider = document.createElement("div");
            divider.className = "editcv-project-divider";

            const techRow = document.createElement("div");
            techRow.className = "editcv-tech-row";

            (p.tech || []).forEach(t => {
                const tile = document.createElement("div");
                tile.className = "editcv-tech-tile";

                const img = document.createElement("img");
                img.className = "editcv-tech-icon";
                img.alt = t;
                img.src = techIconPath(t);

                tile.appendChild(img);
                techRow.appendChild(tile);
            });

            card.appendChild(toggleWrap);
            card.appendChild(title);
            card.appendChild(desc);
            card.appendChild(divider);
            card.appendChild(techRow);

            projectGrid.appendChild(card);
        });
    }

    projectSearch?.addEventListener("input", () => {
        renderProjects(projectSearch.value);
    });

    manageProjectsBtn?.addEventListener("click", (e) => {
        // Placeholder: byt till riktig route senare
        e.preventDefault();
        alert("Placeholder: här kan du länka till Projektsidan (skapa/anslut projekt).");
    });

    syncSelectedProjects();
    renderProjects();

    // ---------- Save button enabled/disabled (optional UI) ----------
    const saveBtn = document.getElementById("saveBtn");
    const saveBtnBottom = document.getElementById("saveBtnBottom");

    function updateSaveButtons() {
        const valid = validateForm();
        if (saveBtn) saveBtn.disabled = !valid;
        if (saveBtnBottom) saveBtnBottom.disabled = !valid;
    }

    // Initial
    updateSaveButtons();

    // If you want: mark saved after submit (UI-only)
    form.addEventListener("submit", () => {
        // If you want UI to show "Sparad" immediately when submitting:
        // setSaveState("saved");
        // Better to do this after redirect/TempData later.
    });
})();
