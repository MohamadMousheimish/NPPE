/* NPPE Prep — free 3-question sample. Reveal correct/incorrect on click; after all
   three, lock it (cookie), show the CTA, and return to the home page. No dependencies. */
(function () {
    "use strict";
    var root = document.getElementById("sample");
    if (!root) return;

    var okText = root.getAttribute("data-ok") || "Correct.";
    var noText = root.getAttribute("data-no") || "Not quite.";
    var ansLabel = root.getAttribute("data-answer-label") || "Correct answer:";
    var home = root.getAttribute("data-home") || "/";

    var cards = [].slice.call(root.querySelectorAll(".sq"));
    var total = cards.length;
    var countEl = document.getElementById("sp-count");
    var done = document.getElementById("sample-done");
    var answered = 0;

    cards.forEach(function (card) {
        var opts = [].slice.call(card.querySelectorAll(".sq__opt"));
        opts.forEach(function (btn) {
            btn.addEventListener("click", function () {
                if (card.getAttribute("data-answered") === "1") return;
                card.setAttribute("data-answered", "1");
                card.classList.add("locked");

                var correctBtn = opts.filter(function (b) { return b.getAttribute("data-correct") === "true"; })[0];
                var isRight = btn.getAttribute("data-correct") === "true";
                btn.classList.add("chosen");
                if (correctBtn) correctBtn.classList.add("correct");
                if (!isRight) btn.classList.add("wrong");

                var expl = card.querySelector(".sq__expl");
                var verdict = card.querySelector(".sq__verdict");
                var ans = card.querySelector(".sq__ans");
                if (verdict) {
                    verdict.textContent = isRight ? okText : noText;
                    verdict.className = "sq__verdict " + (isRight ? "is-ok" : "is-no");
                }
                if (ans) {
                    if (!isRight && correctBtn) {
                        var lab = correctBtn.getAttribute("data-label");
                        var txt = (correctBtn.querySelector(".sq__txt") || {}).textContent || "";
                        ans.textContent = " " + ansLabel + " " + lab + " — " + txt + " ";
                    } else {
                        ans.textContent = " ";
                    }
                }
                if (expl) { expl.hidden = false; }

                answered++;
                if (countEl) countEl.textContent = String(answered);
                if (answered >= total) finish();
            });
        });
    });

    function finish() {
        try {
            document.cookie = "nppe_sample_done=1; max-age=15552000; path=/; SameSite=Lax";
        } catch (e) {}
        if (done) {
            done.hidden = false;
            done.scrollIntoView({ behavior: "smooth", block: "center" });
        }
        var secs = 12;
        var note = document.getElementById("sample-redirect");
        var baseNote = note ? note.textContent : "";
        var timer = window.setInterval(function () {
            secs--;
            if (note) note.textContent = baseNote + " (" + secs + ")";
            if (secs <= 0) { window.clearInterval(timer); window.location.href = home; }
        }, 1000);
    }
})();
