/* NPPE Prep — Import-exam wizard: upload .docx → parse (AJAX) → editable review
   with live validation → gated create. */
(function () {
    "use strict";
    var WIZ = window.WIZ || {}; var t = WIZ.i18n || {};
    var form = document.getElementById("importForm");
    if (!form) return;
    var token = (form.querySelector('input[name="__RequestVerificationToken"]') || {}).value || "";

    var dropzone = document.getElementById("dropzone");
    var fileInput = document.getElementById("fileInput");
    var fileName = document.getElementById("fileName");
    var loader = document.getElementById("loader");
    var loaderText = document.getElementById("loaderText");
    var uploadError = document.getElementById("uploadError");
    var reviewList = document.getElementById("reviewList");
    var createBtn = document.getElementById("createBtn");
    var questionsJson = document.getElementById("questionsJson");
    var examTitle = document.getElementById("examTitle");
    var titleError = document.getElementById("titleError");
    var panels = [].slice.call(document.querySelectorAll(".wpanel"));
    var steps = [].slice.call(document.querySelectorAll("[data-stepper]"));

    var questions = [];

    function goStep(n) {
        panels.forEach(function (p) { p.classList.toggle("is-active", +p.dataset.step === n); });
        steps.forEach(function (s) {
            var k = +s.dataset.stepper;
            s.classList.toggle("is-active", k === n);
            s.classList.toggle("is-done", k < n);
        });
        if (n === 1) { dropzone.hidden = false; loader.hidden = true; }
        window.scrollTo({ top: 0, behavior: "smooth" });
    }

    /* ---------- step 1: upload ---------- */
    dropzone.addEventListener("click", function () { fileInput.click(); });
    dropzone.addEventListener("keydown", function (e) { if (e.key === "Enter" || e.key === " ") { e.preventDefault(); fileInput.click(); } });
    ["dragover", "dragenter"].forEach(function (ev) { dropzone.addEventListener(ev, function (e) { e.preventDefault(); dropzone.classList.add("drag"); }); });
    ["dragleave", "dragend"].forEach(function (ev) { dropzone.addEventListener(ev, function (e) { e.preventDefault(); dropzone.classList.remove("drag"); }); });
    dropzone.addEventListener("drop", function (e) { e.preventDefault(); dropzone.classList.remove("drag"); if (e.dataTransfer.files[0]) handleFile(e.dataTransfer.files[0]); });
    fileInput.addEventListener("change", function () { if (fileInput.files[0]) handleFile(fileInput.files[0]); });

    function showUploadError(msg) { uploadError.hidden = false; uploadError.textContent = msg; }

    function handleFile(file) {
        uploadError.hidden = true;
        if (!/\.docx$/i.test(file.name)) { showUploadError(t.onlyDocx); return; }
        fileName.hidden = false; fileName.textContent = file.name;
        dropzone.hidden = true; loader.hidden = false; loaderText.textContent = t.parsing;

        var fd = new FormData();
        fd.append("file", file);
        fd.append("__RequestVerificationToken", token);
        var started = Date.now();

        fetch(WIZ.parseUrl, { method: "POST", body: fd, headers: { "RequestVerificationToken": token } })
            .then(function (r) { return r.json(); })
            .then(function (data) {
                var wait = Math.max(0, 650 - (Date.now() - started)); // keep the loader briefly visible
                setTimeout(function () {
                    loader.hidden = true;
                    if (!data.recognized) { dropzone.hidden = false; fileName.hidden = true; showUploadError(data.error || t.onlyDocx); return; }
                    questions = (data.questions || []).map(function (q) {
                        return {
                            text: q.text || "",
                            explanationForCorrect: q.explanationForCorrect || "",
                            explanationForIncorrect: q.explanationForIncorrect || "",
                            notes: q.notes || [],
                            options: (q.options || []).map(function (o) { return { text: o.text || "", isCorrect: !!o.isCorrect }; })
                        };
                    });
                    renderReview();
                    goStep(2);
                }, wait);
            })
            .catch(function () { loader.hidden = true; dropzone.hidden = false; fileName.hidden = true; showUploadError("Upload failed. Please try again."); });
    }

    /* ---------- step nav ---------- */
    document.getElementById("toReview").addEventListener("click", function () {
        if (!examTitle.value.trim()) { titleError.textContent = t.titleRequired; examTitle.classList.add("invalid"); examTitle.focus(); return; }
        titleError.textContent = ""; examTitle.classList.remove("invalid");
        goStep(3); validate();
    });
    document.querySelectorAll("[data-back]").forEach(function (b) {
        b.addEventListener("click", function () {
            var cur = panels.filter(function (p) { return p.classList.contains("is-active"); })[0];
            goStep(+cur.dataset.step - 1);
        });
    });

    /* ---------- step 3: review ---------- */
    function esc(s) { var d = document.createElement("div"); d.textContent = s; return d.innerHTML; }

    function renderReview() {
        reviewList.innerHTML = "";
        questions.forEach(function (q, qi) { reviewList.appendChild(buildCard(q, qi)); });
    }

    function buildCard(q, qi) {
        var card = document.createElement("div");
        card.className = "rev-q";
        card.dataset.qi = qi;
        var num = ("0" + (qi + 1)).slice(-2);
        var notesHtml = (q.notes || []).map(function (n) {
            return '<div class="q-note"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M12 9v4M12 17h.01"/><path d="M10.3 3.9 1.8 18a2 2 0 0 0 1.7 3h17a2 2 0 0 0 1.7-3L13.7 3.9a2 2 0 0 0-3.4 0Z"/></svg>' + esc(n) + "</div>";
        }).join("");
        var optsHtml = "";
        for (var oi = 0; oi < Math.max(q.options.length, 4); oi++) {
            var letter = String.fromCharCode(65 + oi);
            var checked = (q.options[oi] && q.options[oi].isCorrect) ? "checked" : "";
            optsHtml +=
                '<div class="opt-edit">' +
                    '<div class="opt-edit__top"><span class="lab">' + letter + '</span>' +
                    '<label class="check"><input type="radio" name="correct-' + qi + '" data-correct ' + checked + ' /><span class="box"></span> ' + esc(t.correct) + '</label></div>' +
                    '<span class="field__wrap"><input class="field__input" data-opt placeholder="' + esc(t.option) + ' ' + letter + '" /></span>' +
                '</div>';
        }
        card.innerHTML =
            '<div class="rev-q__head"><span class="n">' + num + '</span><b>' + esc(t.question) + ' ' + (qi + 1) + '</b><span class="flag" hidden>' + esc(t.needsFix) + '</span></div>' +
            notesHtml +
            '<label class="field"><span class="field__label">' + esc(t.questionText) + '</span><span class="field__wrap"><textarea class="field__input" data-f="text" rows="2"></textarea></span></label>' +
            '<div class="opt-editor">' + optsHtml + '</div>' +
            '<div class="row-2" style="margin-top:1rem">' +
                '<label class="field"><span class="field__label">' + esc(t.feedbackCorrect) + '</span><span class="field__wrap"><textarea class="field__input" data-f="ec"></textarea></span></label>' +
                '<label class="field"><span class="field__label">' + esc(t.feedbackIncorrect) + '</span><span class="field__wrap"><textarea class="field__input" data-f="ei"></textarea></span></label>' +
            '</div>';

        // populate values via properties (safe, no escaping issues)
        card.querySelector('[data-f="text"]').value = q.text;
        card.querySelector('[data-f="ec"]').value = q.explanationForCorrect;
        card.querySelector('[data-f="ei"]').value = q.explanationForIncorrect;
        var optInputs = card.querySelectorAll("[data-opt]");
        for (var j = 0; j < optInputs.length; j++) optInputs[j].value = (q.options[j] && q.options[j].text) || "";
        return card;
    }

    reviewList.addEventListener("input", validate);
    reviewList.addEventListener("change", validate);

    function validate() {
        var cards = [].slice.call(reviewList.querySelectorAll(".rev-q"));
        var flagged = 0, totalOptions = 0;
        cards.forEach(function (card) {
            var qFlag = false;
            var stem = card.querySelector('[data-f="text"]');
            var badStem = !stem.value.trim();
            stem.classList.toggle("invalid", badStem); if (badStem) qFlag = true;

            var opts = [].slice.call(card.querySelectorAll(".opt-edit"));
            totalOptions += opts.length;
            var hasCorrect = false;
            opts.forEach(function (o) {
                var inp = o.querySelector("[data-opt]");
                var empty = !inp.value.trim();
                inp.classList.toggle("invalid", empty); if (empty) qFlag = true;
                var isC = o.querySelector("[data-correct]").checked;
                o.classList.toggle("correct", isC);
                if (isC) hasCorrect = true;
            });
            if (opts.length !== 4 || !hasCorrect) qFlag = true;

            card.classList.toggle("invalid", qFlag);
            card.querySelector(".flag").hidden = !qFlag;
            if (qFlag) flagged++;
        });

        document.getElementById("cQ").textContent = cards.length;
        document.getElementById("cO").textContent = totalOptions;
        document.getElementById("cFlag").textContent = flagged;
        document.getElementById("cFlagWrap").classList.toggle("has", flagged > 0);

        var ok = flagged === 0 && cards.length > 0;
        createBtn.disabled = !ok;
        return ok;
    }

    function syncFromDom() {
        questions = [].slice.call(reviewList.querySelectorAll(".rev-q")).map(function (card) {
            return {
                text: card.querySelector('[data-f="text"]').value.trim(),
                explanationForCorrect: card.querySelector('[data-f="ec"]').value.trim(),
                explanationForIncorrect: card.querySelector('[data-f="ei"]').value.trim(),
                options: [].slice.call(card.querySelectorAll(".opt-edit")).map(function (o) {
                    return { text: o.querySelector("[data-opt]").value.trim(), isCorrect: o.querySelector("[data-correct]").checked };
                })
            };
        });
    }

    /* ---------- create ---------- */
    form.addEventListener("submit", function (e) {
        syncFromDom();
        if (!validate()) { e.preventDefault(); return; }
        questionsJson.value = JSON.stringify(questions);
    });
})();
