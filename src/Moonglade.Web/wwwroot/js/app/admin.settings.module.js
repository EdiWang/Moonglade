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