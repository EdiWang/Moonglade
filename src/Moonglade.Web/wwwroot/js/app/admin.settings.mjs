import { fetch2 } from './httpService.mjs?v=1500'
import { toMagicJson, getLocalizedString } from './utils.module.mjs'
import { success, error } from './toastService.mjs'

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

    try {
        await fetch2(event.currentTarget.action, 'POST', formattedValues);
        success(getLocalizedString('settingsUpdated'));
    } catch (err) {
        error(err);
    } finally {
        enableButton();
    }
}