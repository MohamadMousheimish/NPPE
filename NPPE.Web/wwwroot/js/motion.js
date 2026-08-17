/* NPPE Prep — motion: scroll-triggered reveals via IntersectionObserver.
   No dependencies. Respects prefers-reduced-motion. */
(function () {
    "use strict";

    var reduce = window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    var targets = document.querySelectorAll("[data-reveal]");

    if (reduce || !("IntersectionObserver" in window)) {
        targets.forEach(function (el) { el.classList.add("is-in"); });
        return;
    }

    var io = new IntersectionObserver(function (entries) {
        entries.forEach(function (entry) {
            if (!entry.isIntersecting) return;
            var el = entry.target;
            var delay = parseInt(el.getAttribute("data-reveal-delay") || "0", 10);
            setTimeout(function () { el.classList.add("is-in"); }, delay);
            io.unobserve(el);
        });
    }, { threshold: 0.15, rootMargin: "0px 0px -8% 0px" });

    targets.forEach(function (el) { io.observe(el); });
})();
