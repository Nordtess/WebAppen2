(function () {
    const form = document.getElementById("projectForm");
    if (!form) return;

    // Project image picker (predefined images)
    const projectImage = document.getElementById("ProjectImage");
    const avatarBtn = document.getElementById("projectAvatarBtn");
    const avatarPreview = document.getElementById("projectAvatarPreview");
    const avatarPicker = document.getElementById("projectAvatarPicker");
    const avatarClose = document.getElementById("projectAvatarClose");
    const avatarGrid = document.getElementById("projectAvatarGrid");

    function setProjectImage(src) {
        if (projectImage) projectImage.value = src;
        if (avatarPreview) avatarPreview.src = src;
        avatarGrid?.querySelectorAll(".projectedit-avatar-tile").forEach((b) => b.classList.remove("is-on"));
        avatarGrid?.querySelector(`.projectedit-avatar-tile[data-src='${src}']`)?.classList.add("is-on");
    }

    function openPicker() {
        if (!avatarPicker) return;
        avatarPicker.classList.add("is-open");
        avatarPicker.style.display = "block";
        avatarPicker.setAttribute("aria-hidden", "false");
    }

    function closePicker() {
        if (!avatarPicker) return;
        avatarPicker.classList.remove("is-open");
        avatarPicker.style.display = "none";
        avatarPicker.setAttribute("aria-hidden", "true");
    }

    avatarBtn?.addEventListener("click", () => {
        if (!avatarPicker) return;
        if (avatarPicker.classList.contains("is-open")) closePicker();
        else openPicker();
    });

    avatarClose?.addEventListener("click", closePicker);

    avatarGrid?.addEventListener("click", (e) => {
        const btn = e.target instanceof Element ? e.target.closest("button[data-src]") : null;
        if (!btn) return;
        const src = btn.getAttribute("data-src");
        if (!src) return;
        setProjectImage(src);
        closePicker();
    });

    document.addEventListener("click", (e) => {
        if (!avatarPicker || !avatarBtn) return;
        if (!avatarPicker.classList.contains("is-open")) return;
        const t = e.target;
        if (t instanceof Node && (avatarPicker.contains(t) || avatarBtn.contains(t))) return;
        closePicker();
    });

    // Initialize from hidden value
    if (projectImage?.value) setProjectImage(projectImage.value);

    // Counter
    const desc = form.querySelector("textarea[name='Description']");
    const descCount = document.getElementById("descCount");

    function updateCount() {
        if (!desc || !descCount) return;
        descCount.textContent = String(desc.value.length);
    }

    updateCount();
    desc?.addEventListener("input", updateCount);

    // Tech stack selection
    const techJson = document.getElementById("TechStackJson");
    const techSelected = document.getElementById("techSelected");
    const techGrid = document.getElementById("techGrid");
    const techSearch = document.getElementById("techSearch");

    const MAX_TECH = 4;

    // Inline message (avoid browser alert encoding issues)
    const techMsg = document.createElement("div");
    techMsg.className = "projectedit-help projectedit-tech-msg";
    techMsg.setAttribute("aria-live", "polite");
    techMsg.style.display = "none";
    techGrid?.parentElement?.insertBefore(techMsg, techGrid);

    // Available tech keys (match svg filenames). Must exist in wwwroot/images/svg/techstack.
    const available = [
        "cplusplus",
        "csharp",
        "dotnet",
        "java",
        "javascript",
        "mongodb",
        "mysql",
        "python",
        "react",
        "ruby",
        "swift",
        "typescript"
    ];

    /** @type {Set<string>} */
    let selected = new Set();

    function showTechMsg(text) {
        if (!techMsg) return;
        if (!text) {
            techMsg.textContent = "";
            techMsg.classList.remove("is-error");
            techMsg.style.display = "none";
            return;
        }

        techMsg.textContent = text;
        techMsg.classList.add("is-error");
        techMsg.style.display = "block";
    }

    function loadSelected() {
        if (!techJson) return;
        try {
            const parsed = JSON.parse(techJson.value || "[]");
            if (Array.isArray(parsed)) selected = new Set(parsed.map((x) => String(x)));
        } catch {
            selected = new Set();
        }

        sync();
    }

    function sync() {
        if (!techJson) return;
        techJson.value = JSON.stringify(Array.from(selected));
    }

    function iconPath(key) {
        return `/images/svg/techstack/${key}.svg`;
    }

    function renderSelected() {
        if (!techSelected) return;
        techSelected.innerHTML = "";

        Array.from(selected).forEach((key) => {
            const tile = document.createElement("div");
            tile.className = "projectedit-tech-tile is-on";
            tile.title = "Ta bort";

            const img = document.createElement("img");
            img.className = "projectedit-tech-icon";
            img.src = iconPath(key);
            img.alt = key;

            tile.appendChild(img);
            tile.addEventListener("click", () => {
                selected.delete(key);
                sync();
                showTechMsg("");
                render();
            });

            techSelected.appendChild(tile);
        });
    }

    function renderGrid(filter) {
        if (!techGrid) return;
        techGrid.innerHTML = "";

        const f = (filter || "").trim().toLowerCase();
        const list = !f ? available : available.filter((k) => k.includes(f));

        list.forEach((key) => {
            const tile = document.createElement("div");
            tile.className = "projectedit-tech-tile";
            tile.classList.toggle("is-on", selected.has(key));

            const img = document.createElement("img");
            img.className = "projectedit-tech-icon";
            img.src = iconPath(key);
            img.alt = key;

            tile.appendChild(img);
            tile.addEventListener("click", () => {
                if (selected.has(key)) {
                    selected.delete(key);
                    showTechMsg("");
                } else {
                    if (selected.size >= MAX_TECH) {
                        showTechMsg("Du kan max använda 4 teknologier!");
                        return;
                    }
                    selected.add(key);
                    showTechMsg("");
                }

                sync();
                render();
            });

            techGrid.appendChild(tile);
        });
    }

    function render() {
        renderSelected();
        renderGrid(techSearch?.value || "");
    }

    techSearch?.addEventListener("input", () => render());

    // Validation display: rely on unobtrusive + HTML5 invalid styling.
    form.addEventListener(
        "invalid",
        (e) => {
            e.preventDefault();
        },
        true
    );

    loadSelected();
    render();
})();
