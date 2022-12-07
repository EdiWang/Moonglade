import * as utils from './utils.module.js'

export function handleSettingsSubmit(event) {
    event.preventDefault();
    var btnSaveSettings = '#btn-save-settings';

    document.querySelector(btnSaveSettings).classList.add('disabled');
    document.querySelector(btnSaveSettings).setAttribute('disabled', 'disabled');

    const data = new FormData(event.target);
    const value = Object.fromEntries(data.entries());
    const newValue = utils.toMagicJson(value);

    callApi(event.currentTarget.action, 'POST', newValue,
        (resp) => {
            blogToast.success('Settings Updated');

            document.querySelector(btnSaveSettings).classList.remove('disabled');
            document.querySelector(btnSaveSettings).removeAttribute('disabled');
        });
}

export function compareVersionNumbers(v1, v2) {
    var v1parts = v1.split('.');
    var v2parts = v2.split('.');

    // First, validate both numbers are true version numbers
    function validateParts(parts) {
        for (var i = 0; i < parts.length; ++i) {
            if (!isPositiveInteger(parts[i])) {
                return false;
            }
        }
        return true;
    }
    if (!validateParts(v1parts) || !validateParts(v2parts)) {
        return NaN;
    }

    for (var i = 0; i < v1parts.length; ++i) {
        if (v2parts.length === i) {
            return 1;
        }

        if (v1parts[i] === v2parts[i]) {
            continue;
        }
        if (v1parts[i] > v2parts[i]) {
            return 1;
        }
        return -1;
    }

    if (v1parts.length != v2parts.length) {
        return -1;
    }

    return 0;
}

function isPositiveInteger(x) {
    // http://stackoverflow.com/a/1019526/11236
    return /^\d+$/.test(x);
}