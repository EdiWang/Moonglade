var notyf;
var isDarkMode = false;

notyf = new Notyf({
    position: {
        x: 'center',
        y: 'bottom',
    },
    types: [
        {
            type: 'success',
            background: 'var(--bs-success)',
            duration: 2000
        },
        {
            type: 'error',
            background: 'var(--bs-danger)',
            duration: 3000
        }
    ]
});

var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
    return new bootstrap.Tooltip(tooltipTriggerEl);
});

var bsToast = new bootstrap.Toast(document.getElementById('liveToast'));
var blogToast = {
    success: function (message) {
        $('#liveToast').removeClass('bg-success,bg-warning,bg-danger,bg-info,bg-primary,bg-secondary');
        $('#liveToast').addClass('bg-success');
        $('#blogtoast-message').html(message);
        bsToast.show();
    },
    info: function (message) {
        $('#liveToast').removeClass('bg-success,bg-warning,bg-danger,bg-info,bg-primary,bg-secondary');
        $('#liveToast').addClass('bg-info');
        $('#blogtoast-message').html(message);
        bsToast.show();
    },
    warning: function (message) {
        $('#liveToast').removeClass('bg-success,bg-warning,bg-danger,bg-info,bg-primary,bg-secondary');
        $('#liveToast').addClass('bg-warning');
        $('#blogtoast-message').html(message);
        bsToast.show();
    },
    error: function (message) {
        $('#liveToast').removeClass('bg-success,bg-warning,bg-danger,bg-info,bg-primary,bg-secondary');
        $('#liveToast').addClass('bg-danger');
        $('#blogtoast-message').html(message);
        bsToast.show();
    }
};

$(function () {
    //if (/Android|webOS|iPhone|iPad|iPod|IEMobile|Opera Mini/i.test(navigator.userAgent)) {
    //    $('div.container').addClass('container-fluid').removeClass('container');
    //}

    $('input#term')
        .focus(function () {
            $(this).attr('placeholder', '');
        })
        .blur(function () {
            $(this).attr('placeholder', 'Search');
        });

    $('.lightswitch').click(function () {
        if (isDarkMode) {
            themeModeSwitcher.useLightMode();
        } else {
            themeModeSwitcher.useDarkMode();
        }
    });
});

/**
 * Detect the current active responsive breakpoint in Bootstrap
 * @returns {string}
 * @author farside {@link https://stackoverflow.com/users/4354249/farside}
 */
function getResponsiveBreakpoint() {
    var envs = { xs: 'd-none', sm: 'd-sm-none', md: 'd-md-none', lg: 'd-lg-none', xl: 'd-xl-none' };
    var env = '';

    var $el = $('<div>');
    $el.appendTo($('body'));

    for (var i = Object.keys(envs).length - 1; i >= 0; i--) {
        env = Object.keys(envs)[i];
        $el.addClass(envs[env]);
        if ($el.is(':hidden')) {
            break; // env detected
        }
    }
    $el.remove();
    return env;
};

function buildErrorMessage(responseObject) {
    if (responseObject.responseJSON) {
        var json = responseObject.responseJSON;
        if (json.combinedErrorMessage) {
            return json.combinedErrorMessage;
        } else {
            var errorMessage = 'Error(s):\n\r';

            Object.keys(json).forEach(function (k) {
                errorMessage += (k + ': ' + json[k]) + '\n\r';
            });

            return errorMessage;
        }
    }

    if (responseObject.responseText) {
        return responseObject.responseText.trim();
    }

    return responseObject.status;
}