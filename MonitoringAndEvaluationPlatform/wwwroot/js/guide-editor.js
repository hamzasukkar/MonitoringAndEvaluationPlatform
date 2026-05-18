/* User Guide inline editor (admin only).
   Config is provided by the view as window.__guideEditor:
   { overrides, canEdit, saveUrl, historyUrl, token, tinymceUrl } */
(function () {
    "use strict";

    var cfg = window.__guideEditor || {};
    var overrides = cfg.overrides || {};

    // The section currently open in the modal.
    var current = { key: null, el: null, originalHtml: "", title: "" };
    var tinymceLoaded = false;

    document.addEventListener("DOMContentLoaded", function () {
        applyOverrides();
        if (cfg.canEdit) {
            buildToolbar();
            buildModal();
            decorateSections();
        }
    });

    /* ---- 1. Replace default markup with saved overrides ---- */
    function applyOverrides() {
        Object.keys(overrides).forEach(function (key) {
            var el = document.getElementById(key);
            if (el && el.classList.contains("guide-section")) {
                el.innerHTML = overrides[key];
                el.classList.add("guide-section-overridden");
            }
        });
    }

    function sectionTitle(el) {
        var h = el.querySelector(".section-header h2, h2");
        return h ? h.textContent.trim() : el.id;
    }

    /* ---- 2. Admin toolbar (floating) ---- */
    function buildToolbar() {
        var fab = document.createElement("div");
        fab.className = "guide-edit-fab";
        fab.innerHTML =
            '<a class="btn btn-light btn-sm" href="' + cfg.historyUrl + '">' +
            '<i class="fas fa-history me-1"></i>سجل النسخ</a>' +
            '<button type="button" class="btn btn-primary" id="guideEditToggle">' +
            '<i class="fas fa-pen-to-square me-1"></i>وضع تعديل الدليل</button>';
        document.body.appendChild(fab);

        document.getElementById("guideEditToggle").addEventListener("click", function () {
            var on = document.body.classList.toggle("guide-edit-on");
            this.classList.toggle("btn-primary", !on);
            this.classList.toggle("btn-success", on);
            this.innerHTML = on
                ? '<i class="fas fa-check me-1"></i>وضع التعديل مُفعّل'
                : '<i class="fas fa-pen-to-square me-1"></i>وضع تعديل الدليل';
        });
    }

    /* ---- 3. Add an Edit button to each section ---- */
    function decorateSections() {
        document.querySelectorAll("section.guide-section").forEach(function (el) {
            if (!el.id || el.querySelector(".guide-section-edit-btn")) return;
            var wrap = document.createElement("div");
            wrap.className = "guide-section-edit-btn";
            wrap.innerHTML =
                '<button type="button" class="btn btn-primary btn-sm">' +
                '<i class="fas fa-pen me-1"></i>تعديل هذا القسم</button>';
            wrap.querySelector("button").addEventListener("click", function () {
                openEditor(el);
            });
            el.appendChild(wrap);
        });
    }

    /* ---- 4. Modal ---- */
    function buildModal() {
        var backdrop = document.createElement("div");
        backdrop.className = "guide-modal-backdrop";
        backdrop.id = "guideBackdrop";

        var modal = document.createElement("div");
        modal.className = "guide-modal";
        modal.id = "guideModal";
        modal.innerHTML =
            '<div class="guide-modal-header">' +
            '  <h3 id="guideModalTitle">تعديل القسم</h3>' +
            '  <button type="button" class="btn btn-sm btn-outline-light" id="guideCloseBtn">إغلاق</button>' +
            '</div>' +
            '<div class="guide-modal-tabs">' +
            '  <button data-pane="edit" class="active">المحرر</button>' +
            '  <button data-pane="compare">مقارنة القديم بالجديد</button>' +
            '</div>' +
            '<div class="guide-modal-body">' +
            '  <div class="guide-pane active" id="guidePaneEdit">' +
            '    <textarea id="guideEditorArea"></textarea>' +
            '  </div>' +
            '  <div class="guide-pane" id="guidePaneCompare">' +
            '    <div class="guide-diff-legend">' +
            '      <span><i class="guide-diff-swatch" style="background:#fee2e2"></i>نص محذوف</span>' +
            '      <span><i class="guide-diff-swatch" style="background:#dcfce7"></i>نص مضاف</span>' +
            '    </div>' +
            '    <div class="guide-compare">' +
            '      <div class="col col-old"><h4>النسخة الحالية</h4><div class="render-box" id="guideOldBox"></div></div>' +
            '      <div class="col col-new"><h4>النسخة الجديدة</h4><div class="render-box" id="guideNewBox"></div></div>' +
            '    </div>' +
            '    <h4 style="margin-top:18px;color:#475569;">الفروقات في النص</h4>' +
            '    <div class="render-box guide-diff" id="guideDiffBox"></div>' +
            '  </div>' +
            '  <div class="guide-confirm-box" id="guideConfirmBox">' +
            '    <strong><i class="fas fa-triangle-exclamation me-1"></i>تأكيد الحفظ</strong>' +
            '    <p class="mb-2">سيتم استبدال محتوى هذا القسم. النسخة الحالية ستُحفظ في سجل النسخ ويمكن الرجوع إليها.</p>' +
            '    <input type="text" class="form-control form-control-sm note-input" id="guideNote" placeholder="ملاحظة اختيارية على هذا التعديل (مثال: تحديث خطوات تسجيل الدخول)">' +
            '    <div class="mt-2">' +
            '      <button type="button" class="btn btn-success btn-sm" id="guideConfirmYes"><i class="fas fa-check me-1"></i>نعم، احفظ التغييرات</button>' +
            '      <button type="button" class="btn btn-outline-secondary btn-sm" id="guideConfirmNo">رجوع</button>' +
            '    </div>' +
            '  </div>' +
            '</div>' +
            '<div class="guide-modal-footer">' +
            '  <small class="text-muted" id="guideStatus"></small>' +
            '  <div>' +
            '    <button type="button" class="btn btn-outline-secondary btn-sm" id="guideCancelBtn">إلغاء</button>' +
            '    <button type="button" class="btn btn-primary btn-sm" id="guideShowConfirm"><i class="fas fa-save me-1"></i>حفظ...</button>' +
            '  </div>' +
            '</div>';

        document.body.appendChild(backdrop);
        document.body.appendChild(modal);

        modal.querySelectorAll(".guide-modal-tabs button").forEach(function (b) {
            b.addEventListener("click", function () { switchPane(b.dataset.pane); });
        });
        document.getElementById("guideCloseBtn").addEventListener("click", closeEditor);
        document.getElementById("guideCancelBtn").addEventListener("click", closeEditor);
        backdrop.addEventListener("click", closeEditor);
        document.getElementById("guideShowConfirm").addEventListener("click", function () {
            document.getElementById("guideConfirmBox").classList.add("show");
            document.getElementById("guideConfirmBox").scrollIntoView({ behavior: "smooth" });
        });
        document.getElementById("guideConfirmNo").addEventListener("click", function () {
            document.getElementById("guideConfirmBox").classList.remove("show");
        });
        document.getElementById("guideConfirmYes").addEventListener("click", saveChanges);
    }

    function switchPane(pane) {
        document.querySelectorAll(".guide-modal-tabs button").forEach(function (b) {
            b.classList.toggle("active", b.dataset.pane === pane);
        });
        document.getElementById("guidePaneEdit").classList.toggle("active", pane === "edit");
        document.getElementById("guidePaneCompare").classList.toggle("active", pane === "compare");
        if (pane === "compare") renderCompare();
    }

    /* ---- 5. Open / close ---- */
    function openEditor(el) {
        current.key = el.id;
        current.el = el;
        current.title = sectionTitle(el);
        // The content shown right now is the "current" version (override or default).
        current.originalHtml = getSectionEditableHtml(el);

        document.getElementById("guideModalTitle").textContent = "تعديل: " + current.title;
        document.getElementById("guideStatus").textContent = "";
        document.getElementById("guideConfirmBox").classList.remove("show");
        document.getElementById("guideNote").value = "";
        switchPane("edit");

        document.getElementById("guideBackdrop").classList.add("show");
        document.getElementById("guideModal").classList.add("show");

        loadTinyMce(function () {
            setEditorContent(current.originalHtml);
        });
    }

    function closeEditor() {
        document.getElementById("guideBackdrop").classList.remove("show");
        document.getElementById("guideModal").classList.remove("show");
    }

    // The edit button markup is injected at runtime - never store it.
    function getSectionEditableHtml(el) {
        var clone = el.cloneNode(true);
        clone.querySelectorAll(".guide-section-edit-btn").forEach(function (n) { n.remove(); });
        return clone.innerHTML.trim();
    }

    /* ---- 6. TinyMCE ---- */
    function loadTinyMce(cb) {
        if (tinymceLoaded && window.tinymce) { cb(); return; }
        if (window.tinymce) { tinymceLoaded = true; cb(); return; }
        var s = document.createElement("script");
        s.src = cfg.tinymceUrl;
        s.referrerPolicy = "origin";
        s.onload = function () { tinymceLoaded = true; cb(); };
        s.onerror = function () {
            document.getElementById("guideStatus").textContent =
                "تعذّر تحميل المحرر؛ سيتم استخدام محرر HTML نصّي.";
            cb();
        };
        document.head.appendChild(s);
    }

    function setEditorContent(html) {
        var area = document.getElementById("guideEditorArea");
        if (window.tinymce) {
            if (tinymce.get("guideEditorArea")) tinymce.get("guideEditorArea").remove();
            area.value = html;
            tinymce.init({
                selector: "#guideEditorArea",
                height: "100%",
                directionality: "rtl",
                menubar: "edit insert format table",
                plugins: "code lists link table image media autoresize fullscreen searchreplace",
                toolbar: "undo redo | blocks | bold italic underline forecolor | " +
                    "alignright aligncenter alignleft | bullist numlist | link image table | code fullscreen",
                branding: false,
                promotion: false,
                license_key: "gpl",
                base_url: cfg.tinymceBase,
                suffix: ".min",
                verify_html: false,
                valid_elements: "*[*]",
                extended_valid_elements: "*[*]",
                content_style: "body{font-family:'Segoe UI',Tahoma,sans-serif;direction:rtl;}"
            });
        } else {
            // Fallback: plain HTML textarea.
            area.value = html;
            area.style.width = "100%";
            area.style.minHeight = "55vh";
            area.style.fontFamily = "monospace";
            area.style.direction = "ltr";
        }
    }

    function getEditorContent() {
        if (window.tinymce && tinymce.get("guideEditorArea")) {
            return tinymce.get("guideEditorArea").getContent();
        }
        return document.getElementById("guideEditorArea").value;
    }

    /* ---- 7. Compare (rendered + text diff) ---- */
    function renderCompare() {
        var oldHtml = current.originalHtml;
        var newHtml = getEditorContent();
        document.getElementById("guideOldBox").innerHTML = oldHtml;
        document.getElementById("guideNewBox").innerHTML = newHtml;
        document.getElementById("guideDiffBox").innerHTML =
            diffHtml(htmlToText(oldHtml), htmlToText(newHtml));
    }

    function htmlToText(html) {
        var d = document.createElement("div");
        d.innerHTML = html;
        return (d.textContent || "").replace(/\s+/g, " ").trim();
    }

    // Word-level LCS diff -> <ins>/<del> markup.
    function diffHtml(a, b) {
        var x = a ? a.split(" ") : [];
        var y = b ? b.split(" ") : [];
        var n = x.length, m = y.length;
        var dp = [];
        for (var i = 0; i <= n; i++) { dp[i] = new Array(m + 1).fill(0); }
        for (i = n - 1; i >= 0; i--) {
            for (var j = m - 1; j >= 0; j--) {
                dp[i][j] = x[i] === y[j]
                    ? dp[i + 1][j + 1] + 1
                    : Math.max(dp[i + 1][j], dp[i][j + 1]);
            }
        }
        var out = [];
        i = 0; j = 0;
        function esc(t) {
            return t.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
        }
        while (i < n && j < m) {
            if (x[i] === y[j]) { out.push(esc(x[i])); i++; j++; }
            else if (dp[i + 1][j] >= dp[i][j + 1]) { out.push("<del>" + esc(x[i]) + "</del>"); i++; }
            else { out.push("<ins>" + esc(y[j]) + "</ins>"); j++; }
        }
        while (i < n) { out.push("<del>" + esc(x[i]) + "</del>"); i++; }
        while (j < m) { out.push("<ins>" + esc(y[j]) + "</ins>"); j++; }
        var html = out.join(" ");
        return html.trim() ? html : '<em class="text-muted">لا توجد فروقات نصّية.</em>';
    }

    /* ---- 8. Save ---- */
    function saveChanges() {
        var newHtml = getEditorContent();
        if (!newHtml || !newHtml.trim()) {
            document.getElementById("guideStatus").textContent = "المحتوى فارغ.";
            return;
        }
        var btn = document.getElementById("guideConfirmYes");
        btn.disabled = true;
        document.getElementById("guideStatus").textContent = "جارٍ الحفظ...";

        fetch(cfg.saveUrl, {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "RequestVerificationToken": cfg.token
            },
            body: JSON.stringify({
                sectionKey: current.key,
                title: current.title,
                newHtml: newHtml,
                originalHtml: current.originalHtml,
                note: document.getElementById("guideNote").value || null
            })
        })
            .then(function (r) { return r.ok ? r.json() : Promise.reject(r); })
            .then(function () {
                // Reflect the change immediately on the page.
                current.el.innerHTML = newHtml;
                current.el.classList.add("guide-section-overridden");
                decorateSections();
                document.getElementById("guideStatus").textContent = "تم الحفظ بنجاح.";
                setTimeout(closeEditor, 600);
            })
            .catch(function () {
                document.getElementById("guideStatus").textContent = "فشل الحفظ. حاول مجدداً.";
            })
            .finally(function () { btn.disabled = false; });
    }
})();
