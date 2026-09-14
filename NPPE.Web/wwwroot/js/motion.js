/* NPPE Prep — motion: scroll-triggered reveals via IntersectionObserver.
   No dependencies. Respects prefers-reduced-motion. */
(function () {
    "use strict";

    var reduce = window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches;

    // Assign a --i index to each child of a [data-stagger] group (drives the cascade).
    document.querySelectorAll("[data-stagger]").forEach(function (group) {
        Array.prototype.forEach.call(group.children, function (child, i) {
            child.style.setProperty("--i", i);
        });
    });

    var revealTargets = document.querySelectorAll("[data-reveal], [data-stagger]");
    var countTargets = document.querySelectorAll("[data-countup]");

    // Count a number up to its target with an easeOutCubic curve.
    function runCount(el) {
        var target = parseFloat(el.getAttribute("data-countup"));
        if (isNaN(target)) return;
        var suffix = el.getAttribute("data-countup-suffix") || "";
        var dur = 1100, startTs = null;
        function frame(ts) {
            if (!startTs) startTs = ts;
            var p = Math.min((ts - startTs) / dur, 1);
            var eased = 1 - Math.pow(1 - p, 3);
            el.textContent = Math.round(target * eased) + suffix;
            if (p < 1) window.requestAnimationFrame(frame);
        }
        window.requestAnimationFrame(frame);
    }

    if (reduce || !("IntersectionObserver" in window)) {
        revealTargets.forEach(function (el) { el.classList.add("is-in"); });
        countTargets.forEach(function (el) {
            var t = el.getAttribute("data-countup");
            if (t !== null) el.textContent = t + (el.getAttribute("data-countup-suffix") || "");
        });
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
    revealTargets.forEach(function (el) { io.observe(el); });

    var cio = new IntersectionObserver(function (entries) {
        entries.forEach(function (entry) {
            if (!entry.isIntersecting) return;
            runCount(entry.target);
            cio.unobserve(entry.target);
        });
    }, { threshold: 0.6 });
    countTargets.forEach(function (el) { cio.observe(el); });
})();

/* NPPE Prep — testimonials carousel: pages one viewport-width at a time,
   auto-advances (unless reduced motion), loops, pauses on hover/focus. */
(function () {
    "use strict";

    var reduce = window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches;

    document.querySelectorAll("[data-carousel]").forEach(function (wrap) {
        var track = wrap.querySelector("[data-carousel-track]");
        var prev = wrap.querySelector("[data-carousel-prev]");
        var next = wrap.querySelector("[data-carousel-next]");
        if (!track) return;

        var atStart = function () { return track.scrollLeft <= 2; };
        var atEnd = function () { return track.scrollLeft + track.clientWidth >= track.scrollWidth - 2; };
        var pageable = function () { return track.scrollWidth - track.clientWidth > 4; };

        function updateNav() {
            var show = pageable();
            if (prev) { prev.hidden = !show; prev.style.opacity = atStart() ? "0.35" : "1"; }
            if (next) { next.hidden = !show; next.style.opacity = atEnd() ? "0.35" : "1"; }
        }
        function page(dir) {
            if (dir > 0 && atEnd()) { track.scrollTo({ left: 0 }); }
            else if (dir < 0 && atStart()) { track.scrollTo({ left: track.scrollWidth }); }
            else { track.scrollBy({ left: dir * track.clientWidth }); }
        }

        if (prev) prev.addEventListener("click", function () { page(-1); });
        if (next) next.addEventListener("click", function () { page(1); });
        track.addEventListener("scroll", function () {
            window.requestAnimationFrame(updateNav);
        }, { passive: true });
        window.addEventListener("resize", updateNav);
        updateNav();

        // Auto-advance
        if (!reduce) {
            var timer = null;
            var start = function () {
                stop();
                timer = window.setInterval(function () { if (pageable()) page(1); }, 5000);
            };
            var stop = function () { if (timer) { window.clearInterval(timer); timer = null; } };
            wrap.addEventListener("mouseenter", stop);
            wrap.addEventListener("mouseleave", start);
            wrap.addEventListener("focusin", stop);
            wrap.addEventListener("focusout", start);
            start();
        }
    });
})();
