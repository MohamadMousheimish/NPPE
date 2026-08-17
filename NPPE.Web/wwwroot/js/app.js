/* NPPE Prep — in-app interactions + animations for the server-rendered shell.
   Handles the profile dropdown, mobile nav, and reveal-driven count-ups /
   gauges / bars / progress spine. Respects prefers-reduced-motion. */
(function () {
    "use strict";

    /* ---- profile dropdown ---- */
    document.querySelectorAll("[data-profile]").forEach(function (profile) {
        var menu = profile.querySelector("[data-menu]");
        if (!menu) return;
        profile.addEventListener("click", function (e) {
            if (menu.contains(e.target)) return; // let clicks inside the menu through
            e.stopPropagation();
            menu.classList.toggle("open");
        });
    });
    document.addEventListener("click", function () {
        document.querySelectorAll("[data-menu].open").forEach(function (m) { m.classList.remove("open"); });
    });
    document.addEventListener("keydown", function (e) {
        if (e.key === "Escape") document.querySelectorAll("[data-menu].open").forEach(function (m) { m.classList.remove("open"); });
    });

    /* ---- mobile nav ---- */
    document.querySelectorAll("[data-burger]").forEach(function (b) {
        b.addEventListener("click", function (e) {
            e.stopPropagation();
            var nav = document.querySelector("[data-nav]");
            if (nav) nav.classList.toggle("mobile-open");
        });
    });

    /* ---- reveal-driven animations ---- */
    var reduce = window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches;

    function animateCount(el) {
        var target = parseFloat(el.getAttribute("data-count"));
        var dec = parseInt(el.getAttribute("data-dec") || "0", 10);
        var suffix = el.getAttribute("data-suffix") || "";
        if (reduce) { el.textContent = target.toFixed(dec) + suffix; return; }
        var dur = 1100, start = null;
        function step(ts) {
            if (!start) start = ts;
            var p = Math.min((ts - start) / dur, 1);
            var eased = 1 - Math.pow(1 - p, 3);
            el.textContent = (target * eased).toFixed(dec) + suffix;
            if (p < 1) requestAnimationFrame(step);
        }
        requestAnimationFrame(step);
    }

    function fireFills(scope) {
        scope.querySelectorAll("[data-count]").forEach(animateCount);
        scope.querySelectorAll(".dial .fill[data-pct]").forEach(function (c) {
            var pct = parseFloat(c.getAttribute("data-pct"));
            c.style.strokeDashoffset = (162.31 * pct / 100).toFixed(2);
        });
        scope.querySelectorAll(".result-ring .fill[data-pct]").forEach(function (c) {
            var pct = parseFloat(c.getAttribute("data-pct"));
            var circ = parseFloat(c.getAttribute("data-circ") || "414");
            c.style.strokeDashoffset = (circ * pct / 100).toFixed(2);
        });
        scope.querySelectorAll("[data-bar]").forEach(function (b) { b.style.width = b.getAttribute("data-bar") + "%"; });
    }

    if (!("IntersectionObserver" in window)) {
        fireFills(document);
        document.querySelectorAll(".spine").forEach(function (s) { s.classList.add("is-in"); });
        return;
    }
    var io = new IntersectionObserver(function (entries) {
        entries.forEach(function (e) {
            if (!e.isIntersecting) return;
            e.target.classList.add("is-in");
            fireFills(e.target);
            io.unobserve(e.target);
        });
    }, { threshold: 0.2 });
    document.querySelectorAll("[data-animate], .spine").forEach(function (el) { io.observe(el); });
})();
