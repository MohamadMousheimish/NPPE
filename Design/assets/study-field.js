/* NPPE Prep — ambient study-motif field generator.
   Drop `data-study-field` (optionally `data-study-count`) on a positioned
   container; this injects a dense, drifting field of technical line-icons.
   Icons: book, exam sheet, notepad, pencil, compass, set-square, cap, check,
   ruler, clipboard, dial. Motion is CSS-driven and frozen by prefers-reduced-motion. */
(function () {
    "use strict";

    var ICONS = [
        /* open book */
        '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.2"><path d="M12 5c-1.6-1-4.1-1.6-6.2-1.6V18c2.1 0 4.6.6 6.2 1.6 1.6-1 4.1-1.6 6.2-1.6V3.4c-2.1 0-4.6.6-6.2 1.6Z"/><path d="M12 5v14.6"/></svg>',
        /* OMR bubble sheet */
        '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.1"><rect x="4" y="3" width="16" height="18" rx="1"/><circle cx="8" cy="8" r="1"/><line x1="11" y1="8" x2="16" y2="8"/><circle cx="8" cy="12" r="1" fill="currentColor" stroke="none"/><line x1="11" y1="12" x2="16" y2="12"/><circle cx="8" cy="16" r="1"/><line x1="11" y1="16" x2="16" y2="16"/></svg>',
        /* notepad */
        '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.2"><rect x="5" y="3" width="14" height="18" rx="1"/><line x1="8" y1="8" x2="16" y2="8"/><line x1="8" y1="11.5" x2="16" y2="11.5"/><line x1="8" y1="15" x2="13" y2="15"/></svg>',
        /* pencil */
        '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.3"><path d="M4 20l1-4L16 5l3 3L8 19l-4 1Z"/><path d="M14 7l3 3"/></svg>',
        /* drafting compass */
        '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.2"><circle cx="12" cy="5" r="1.7"/><path d="M11.4 6.4 6.5 20"/><path d="M12.6 6.4 17.5 20"/><path d="M9.4 14h5.2"/></svg>',
        /* set square */
        '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.2"><path d="M5 5v14h14Z"/><path d="M8 16h2M8 13h2M8 10h2"/></svg>',
        /* graduation cap */
        '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.2"><path d="M2 9l10-4 10 4-10 4Z"/><path d="M6 11v4c0 1.5 2.7 2.6 6 2.6s6-1.1 6-2.6v-4"/><path d="M22 9v4.5"/></svg>',
        /* check in circle */
        '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.4"><circle cx="12" cy="12" r="8"/><path d="M8.5 12.5l2.5 2.5 4.5-5.5"/></svg>',
        /* ruler */
        '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.2"><rect x="2" y="8" width="20" height="8" rx="1"/><path d="M6 8v3M10 8v4M14 8v3M18 8v4"/></svg>',
        /* clipboard */
        '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.2"><rect x="5" y="4" width="14" height="17" rx="1"/><rect x="9" y="2.5" width="6" height="3" rx="1"/><path d="M8 10h8M8 13.5h8M8 17h5"/></svg>',
        /* dial / gauge */
        '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.2"><circle cx="12" cy="12" r="8"/><path d="M12 12l3.5-2.5"/><path d="M12 4v2M20 12h-2M12 20v-2M4 12h2"/></svg>'
    ];

    function rnd(a, b) { return a + Math.random() * (b - a); }

    document.querySelectorAll("[data-study-field]").forEach(function (container) {
        var count = parseInt(container.getAttribute("data-study-count") || "18", 10);
        var layer = document.createElement("div");
        layer.className = "study-bg";
        layer.setAttribute("aria-hidden", "true");

        // loose grid + jitter so icons spread evenly without clustering
        var cols = 4;
        var rows = Math.ceil(count / cols);
        var n = 0;
        for (var r = 0; r < rows; r++) {
            for (var c = 0; c < cols; c++) {
                if (n >= count) break;
                var x = (c + rnd(0.15, 0.85)) * (100 / cols);
                var y = (r + rnd(0.15, 0.85)) * (100 / rows);
                var size = rnd(34, 68);
                var signal = Math.random() < 0.12;
                var el = document.createElement("div");
                el.className = "motif" + (signal ? " motif--signal" : "");
                el.style.cssText =
                    "top:" + y.toFixed(1) + "%;left:" + x.toFixed(1) + "%;" +
                    "width:" + size.toFixed(0) + "px;height:" + size.toFixed(0) + "px;" +
                    "--dur:" + rnd(20, 38).toFixed(0) + "s;--delay:" + (-rnd(0, 24)).toFixed(1) + "s;" +
                    "--dx:" + rnd(-16, 16).toFixed(0) + "px;--dy:" + rnd(-18, 18).toFixed(0) + "px;" +
                    "--rot:" + rnd(-14, 14).toFixed(0) + "deg;--spin:" + rnd(-8, 8).toFixed(0) + "deg;" +
                    "--op:" + rnd(0.08, 0.20).toFixed(2);
                el.innerHTML = ICONS[Math.floor(Math.random() * ICONS.length)];
                layer.appendChild(el);
                n++;
            }
        }
        container.insertBefore(layer, container.firstChild);
    });
})();
