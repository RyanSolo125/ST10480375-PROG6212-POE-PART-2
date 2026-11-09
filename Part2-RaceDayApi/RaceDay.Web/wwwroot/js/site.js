// Asks "Are you sure?" before submitting any form that has a data-confirm attribute
// (e.g. cancelling an enrolment or removing a category).
document.addEventListener("submit", function (event) {
    var form = event.target;
    var message = form.getAttribute("data-confirm");
    if (message && !window.confirm(message)) {
        event.preventDefault();
    }
});
