var isDarkMode = false;

var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
    return new bootstrap.Tooltip(tooltipTriggerEl);
});

var bsToast = new bootstrap.Toast(document.getElementById('liveToast'));
var lt = document.querySelector('#liveToast');
var blogtoastMessage = document.querySelector('#blogtoast-message');

function removeToastBgColor() {
    const bgClasses = [
        'bg-success',
        'bg-warning',
        'bg-danger',
        'bg-info',
        'bg-primary',
        'bg-secondary'
    ];

    for (var i = 0; i < bgClasses.length; i++) {
        lt.classList.remove(bgClasses[i]);
    }
}

var blogToast = {
    success: function (message) {
        removeToastBgColor();
        lt.classList.add('bg-success');
        blogtoastMessage.innerHTML = message;
        bsToast.show();
    },
    info: function (message) {
        removeToastBgColor();
        lt.classList.add('bg-info');
        blogtoastMessage.innerHTML = message;
        bsToast.show();
    },
    warning: function (message) {
        removeToastBgColor();
        lt.classList.add('bg-warning');
        blogtoastMessage.innerHTML = message;
        bsToast.show();
    },
    error: function (message) {
        removeToastBgColor();
        lt.classList.add('bg-danger');
        blogtoastMessage.innerHTML = message;
        bsToast.show();
    }
};

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
