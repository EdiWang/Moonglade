import { fetch2 } from './httpService.mjs?v=1500'
import { toMagicJson } from './utils.module.mjs'
import { success } from './toastService.mjs'

export async function handleSettingsSubmit(event) {
    event.preventDefault();

    const btnSaveSettingsSelector = '#btn-save-settings';
    const btnSaveSettings = document.querySelector(btnSaveSettingsSelector);

    const disableButton = () => {
        btnSaveSettings.classList.add('disabled');
        btnSaveSettings.setAttribute('disabled', 'disabled');
    };

    const enableButton = () => {
        btnSaveSettings.classList.remove('disabled');
        btnSaveSettings.removeAttribute('disabled');
    };

    disableButton();

    const formData = new FormData(event.target);
    const formValues = Object.fromEntries(formData.entries());
    const formattedValues = toMagicJson(formValues);

    await fetch2(event.currentTarget.action, 'POST', formattedValues);

    success('Settings Updated');
    enableButton();
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