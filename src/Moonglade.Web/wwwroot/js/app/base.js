var isDarkMode = false;

var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
    return new bootstrap.Tooltip(tooltipTriggerEl);
});

var bsToast = new bootstrap.Toast(document.getElementById('liveToast'));
var blogToast = {
    success: function (message) {
        $('#liveToast').removeClass('bg-success bg-warning bg-danger bg-info bg-primary bg-secondary');
        document.querySelector('#liveToast').classList.add('bg-success');
        document.querySelector('#blogtoast-message').innerHTML = message;
        bsToast.show();
    },
    info: function (message) {
        $('#liveToast').removeClass('bg-success bg-warning bg-danger bg-info bg-primary bg-secondary');
        document.querySelector('#liveToast').classList.add('bg-info');
        document.querySelector('#blogtoast-message').innerHTML = message;
        bsToast.show();
    },
    warning: function (message) {
        $('#liveToast').removeClass('bg-success bg-warning bg-danger bg-info bg-primary bg-secondary');
        document.querySelector('#liveToast').classList.add('bg-warning');
        document.querySelector('#blogtoast-message').innerHTML = message;
        bsToast.show();
    },
    error: function (message) {
        $('#liveToast').removeClass('bg-success bg-warning bg-danger bg-info bg-primary bg-secondary');
        document.querySelector('#liveToast').classList.add('bg-danger');
        document.querySelector('#blogtoast-message').innerHTML = message;
        bsToast.show();
    }
};

$('input#term')
    .focus(function () {
        $(this).attr('placeholder', '');
    })
    .blur(function () {
        $(this).attr('placeholder', 'Search');
    });

function toggleTheme() {
    if (isDarkMode) {
        themeModeSwitcher.useLightMode();
    } else {
        themeModeSwitcher.useDarkMode();
    }
}

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
