/* NPPE Prep — Study reader: builds the on-page table of contents, a scroll-spy,
   a reading-progress bar, and a read-time estimate. Progressive enhancement only. */
(function () {
    "use strict";
    var reader = document.querySelector("[data-reader]");
    if (!reader) return;

    // ---- read-time estimate ----
    var timeEl = document.querySelector("[data-readtime]");
    if (timeEl) {
        var words = (reader.innerText || "").trim().split(/\s+/).length;
        var mins = Math.max(1, Math.round(words / 200));
        timeEl.textContent = mins + " min read";
    }

    // ---- table of contents from h2 headings ----
    var tocHost = document.querySelector("[data-toc]");
    var headings = [].slice.call(reader.querySelectorAll("h2"));
    headings.forEach(function (h, i) {
        if (!h.id) h.id = "s-" + i;
    });

    if (tocHost && headings.length) {
        var frag = document.createDocumentFragment();
        headings.forEach(function (h) {
            var a = document.createElement("a");
            a.href = "#" + h.id;
            a.textContent = h.getAttribute("data-toc") || h.textContent;
            a.dataset.for = h.id;
            frag.appendChild(a);
        });
        tocHost.appendChild(frag);
    } else if (tocHost) {
        var host = document.querySelector("[data-toc-host]");
        if (host) host.style.display = "none";
    }

    var links = tocHost ? [].slice.call(tocHost.querySelectorAll("a")) : [];
    function setActive(id) {
        links.forEach(function (l) { l.classList.toggle("on", l.dataset.for === id); });
    }

    // ---- scroll-spy ----
    if ("IntersectionObserver" in window && headings.length) {
        var seen = {};
        var io = new IntersectionObserver(function (entries) {
            entries.forEach(function (e) { seen[e.target.id] = e.isIntersecting; });
            // pick the topmost heading currently in view
            for (var i = 0; i < headings.length; i++) {
                if (seen[headings[i].id]) { setActive(headings[i].id); break; }
            }
        }, { rootMargin: "-15% 0px -70% 0px" });
        headings.forEach(function (h) { io.observe(h); });
    }

    // ---- reading progress bar ----
    var bar = document.querySelector("[data-readbar]");
    if (bar) {
        var tick = function () {
            var r = reader.getBoundingClientRect();
            var total = reader.offsetHeight - window.innerHeight;
            var passed = Math.min(Math.max(-r.top, 0), Math.max(total, 1));
            bar.style.width = (total > 0 ? (passed / total) * 100 : 0) + "%";
        };
        window.addEventListener("scroll", tick, { passive: true });
        window.addEventListener("resize", tick);
        tick();
    }

    // smooth-scroll with offset for the sticky header
    links.forEach(function (l) {
        l.addEventListener("click", function (e) {
            var t = document.getElementById(l.dataset.for);
            if (!t) return;
            e.preventDefault();
            var y = t.getBoundingClientRect().top + window.pageYOffset - 90;
            window.scrollTo({ top: y, behavior: "smooth" });
            history.replaceState(null, "", "#" + l.dataset.for);
        });
    });
})();
