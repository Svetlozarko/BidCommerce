$(document).ready(function () {
    $("#toggleForm").on("submit", function () {
        const button = $(this).find("button");
        button.prop("disabled", true).removeClass("btn-outline-primary").addClass("btn-warning").text("Switching...");
    });
});