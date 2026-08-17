/* NPPE Prep — app shell injector + in-app animations (count-up, dials, bars). */
(function () {
    "use strict";

    var NAV_STUDENT = [
        { key: "dashboard", label: "Dashboard", href: "Dashboard.html" },
        { key: "exams",     label: "Practice Exams", href: "Exams.html" },
        { key: "history",   label: "History", href: "History.html" },
        { key: "billing",   label: "Billing", href: "Billing.html" }
    ];
    var NAV_ADMIN = [
        { key: "exams",     label: "Manage Exams", href: "Admin-Exams.html" }
    ];

    function icon(p) {
        return '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" ' +
               'stroke-linecap="round" stroke-linejoin="round">' + p + '</svg>';
    }

    /* ---- app bar ---- */
    document.querySelectorAll("[data-appbar]").forEach(function (host) {
        var active = host.getAttribute("data-active") || "";
        var role = host.getAttribute("data-role") || "student";
        var name = host.getAttribute("data-name") || "Ada Lovelace";
        var email = host.getAttribute("data-email") || "ada@firm.ca";
        var initials = name.split(/\s+/).map(function (w) { return w[0]; }).join("").slice(0, 2).toUpperCase();

        var links = (role === "admin" ? NAV_ADMIN : NAV_STUDENT).map(function (n) {
            return '<a href="' + n.href + '"' + (n.key === active ? ' class="is-active"' : "") + '>' + n.label + "</a>";
        }).join("");

        host.className = "appbar";
        host.innerHTML =
            '<div class="appbar__in">' +
                '<a class="wm" href="Dashboard.html"><span class="mk">N</span><b>NPPE&nbsp;Prep</b></a>' +
                '<nav class="nav" data-nav>' + links + "</nav>" +
                '<div class="appbar__right">' +
                    '<div class="lang"><button class="on">EN</button><span class="sep">/</span><button>FR</button></div>' +
                    '<div class="profile" data-profile>' +
                        '<span class="avatar">' + initials + "</span>" +
                        '<span class="who"><b>' + name + "</b><span>" + email + "</span></span>" +
                        '<span class="caret">' + icon('<path d="M6 9l6 6 6-6"/>') + "</span>" +
                        '<div class="menu" data-menu>' +
                            '<a href="Profile.html">' + icon('<circle cx="12" cy="8" r="4"/><path d="M4 21c0-4 4-6 8-6s8 2 8 6"/>') + " My profile</a>" +
                            '<a href="ChangePassword.html">' + icon('<rect x="5" y="11" width="14" height="9" rx="1.5"/><path d="M8 11V8a4 4 0 0 1 8 0v3"/>') + " Change password</a>" +
                            '<a href="Billing.html">' + icon('<rect x="3" y="5" width="18" height="14" rx="2"/><path d="M3 10h18"/>') + " Billing</a>" +
                            '<div class="sep"></div>' +
                            '<a class="danger" href="Login.html">' + icon('<path d="M15 12H3"/><path d="M9 8l-4 4 4 4"/><path d="M13 4h6v16h-6"/>') + " Sign out</a>" +
                        "</div>" +
                    "</div>" +
                    '<button class="burger" data-burger aria-label="Menu">' + icon('<path d="M3 6h18M3 12h18M3 18h18"/>') + "</button>" +
                "</div>" +
            "</div>";

        var profile = host.querySelector("[data-profile]");
        var menu = host.querySelector("[data-menu]");
        if (profile && menu) {
            profile.addEventListener("click", function (e) { e.stopPropagation(); menu.classList.toggle("open"); });
            document.addEventListener("click", function () { menu.classList.remove("open"); });
        }
        var burger = host.querySelector("[data-burger]");
        var nav = host.querySelector("[data-nav]");
        if (burger && nav) burger.addEventListener("click", function () { nav.classList.toggle("mobile-open"); });
    });

    /* ---- footer ---- */
    document.querySelectorAll("[data-appfoot]").forEach(function (host) {
        var year = host.getAttribute("data-year") || "2026";
        host.className = "appfoot";
        host.innerHTML =
            '<div class="appfoot__in">' +
                '<div class="wm"><span class="mk">N</span><b>NPPE&nbsp;Prep</b></div>' +
                "<nav><a href='#'>About</a><a href='Privacy.html'>Privacy</a><a href='#'>Terms</a><a href='#'>Contact</a></nav>" +
                '<div class="copy">© ' + year + " NPPE Prep — Built for engineers-in-training · Not affiliated with any regulator</div>" +
            "</div>";
    });

    /* ---- reveal-driven animations: count-up, dials, bars, spine ---- */
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
            var circ = 162.31; // path length (2·π·r, r≈25.83)
            c.style.strokeDashoffset = (circ * pct / 100).toFixed(2);
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
    }, { threshold: 0.25 });
    document.querySelectorAll("[data-animate], .spine").forEach(function (el) { io.observe(el); });
})();
