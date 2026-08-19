// Admin finance dashboard — GST/HST explainer modal.
(function () {
    "use strict";
    var modal = document.getElementById("taxModal");
    if (!modal) return;
    var open = document.getElementById("taxInfo");
    var close = document.getElementById("taxClose");
    if (open) open.addEventListener("click", function () { modal.classList.add("on"); });
    if (close) close.addEventListener("click", function () { modal.classList.remove("on"); });
    modal.addEventListener("click", function (e) { if (e.target === modal) modal.classList.remove("on"); });
    document.addEventListener("keydown", function (e) { if (e.key === "Escape") modal.classList.remove("on"); });
})();
