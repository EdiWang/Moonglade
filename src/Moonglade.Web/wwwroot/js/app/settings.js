var sendTestEmail = function () {
    $('#a-send-test-mail').text('Sending...');
    $('#a-send-test-mail').addClass('disabled');
    $('#a-send-test-mail').attr('disabled', 'disabled');

    $.post('/api/settings/send-test-email',
        function (data) {
            if (data.isSuccess) {
                notyf.success('Email is sent.');
            } else {
                notyf.error(data.message);
            }
        })
        .fail(function (xhr, status, error) {
            var responseJson = $.parseJSON(xhr.responseText);
            notyf.error(responseJson.message);
        })
        .always(function () {
            $('#a-send-test-mail').text('Send Test Email');
            $('#a-send-test-mail').removeClass('disabled');
            $('#a-send-test-mail').removeAttr('disabled');
        });
};

var btnSaveSettings = '#btn-save-settings';
var onUpdateSettingsBegin = function () {
    $(btnSaveSettings).text('Processing...');
    $(btnSaveSettings).addClass('disabled');
    $(btnSaveSettings).attr('disabled', 'disabled');
};

var onUpdateSettingsComplete = function () {
    $(btnSaveSettings).text('Save');
    $(btnSaveSettings).removeClass('disabled');
    $(btnSaveSettings).removeAttr('disabled');
};

var onUpdateSettingsSuccess = function (context) {
    if (notyf) {
        notyf.success('Settings Updated');
    } else {
        alert('Settings Updated');
    }
};

var onUpdateSettingsFailed = function (context) {
    var message = buildErrorMessage(context);

    if (notyf) {
        notyf.error(message);
    } else {
        alert(message);
    }
};

var btnClearCache = '.btn-clearcache';
var onClearCacheBegin = function () {
    $(btnClearCache).text('Processing...');
    $(btnClearCache).addClass('disabled');
    $(btnClearCache).attr('disabled', 'disabled');
};

var onClearCacheComplete = function () {
    $(btnClearCache).text('Clear');
    $(btnClearCache).removeClass('disabled');
    $(btnClearCache).removeAttr('disabled');
};

var onClearCacheSuccess = function (context) {
    $('#cacheModal').modal('hide');
    if (notyf) {
        notyf.success('Cleared Cache');
    } else {
        alert('Cleared Cache');
    }
};

var onClearCacheFailed = function (context) {
    var msg = buildErrorMessage(context);
    if (notyf) {
        notyf.error(`Server Error: ${msg}`);
    } else {
        alert(`Error Code: ${msg}`);
    }
};

function tryRestartWebsite() {
    callApi(`/api/settings/shutdown`, 'POST', {}, () => { });
    $('.btn-restart').text('Wait...');
    $('.btn-restart').addClass('disabled');
    $('.btn-restart').attr('disabled', 'disabled');
    startTimer(30, $('.btn-restart'));
    setTimeout(function () {
        location.href = '/admin/settings';
    }, 30000);
}

function tryResetWebsite() {
    callApi(`/api/settings/reset`, 'POST', {}, () => { });
    $('.btn-reset').text('Wait...');
    $('.btn-reset').addClass('disabled');
    $('.btn-reset').attr('disabled', 'disabled');
    startTimer(30, $('.btn-reset'));
    setTimeout(function () {
        location.href = '/';
    }, 30000);
}

function startTimer(duration, display) {
    var timer = duration, minutes, seconds;
    setInterval(function () {
        minutes = parseInt(timer / 60, 10);
        seconds = parseInt(timer % 60, 10);

        minutes = minutes < 10 ? '0' + minutes : minutes;
        seconds = seconds < 10 ? '0' + seconds : seconds;

        display.text(minutes + ':' + seconds);

        if (--timer < 0) {
            timer = duration;
        }
    }, 1000);
}