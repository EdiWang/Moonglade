window.toastr.options = {
    "positionClass": "toast-bottom-center"
};

$(function () {
    $('[data-toggle="popover"]').popover();
    $('[data-toggle="tooltip"]').tooltip();

    $(".site-qrcode").qrcode(document.location.origin);

    $("input#search, #search-mobile")
        .focus(function () {
            $(this).attr("placeholder", "");
        })
        .blur(function () {
            $(this).attr("placeholder", "Search");
        });
});