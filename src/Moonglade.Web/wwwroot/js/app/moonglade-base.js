window.toastr.options = {
    "positionClass": 'toast-bottom-center'
};

$(function () {
    $('[data-toggle="popover"]').popover();
    $('[data-toggle="tooltip"]').tooltip();

    $('.site-qrcode').qrcode(document.location.origin);

    if (/Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent)) {
        $('div.container').addClass('container-fluid').removeClass('container');
    }

    $('input#search, #search-mobile')
        .focus(function () {
            $(this).attr('placeholder', '');
        })
        .blur(function () {
            $(this).attr('placeholder', 'Search');
        });
});