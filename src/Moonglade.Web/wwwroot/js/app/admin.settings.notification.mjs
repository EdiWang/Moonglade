import { moongladeFetch2 } from './httpService.mjs'
import { handleSettingsSubmit } from './admin.settings.mjs';
import { success, error } from './toastService.mjs';

function toggleButtonState(button, isDisabled) {
    if (isDisabled) {
        button.classList.add('disabled');
        button.setAttribute('disabled', 'disabled');
    } else {
        button.classList.remove('disabled');
        button.removeAttribute('disabled');
    }
}

async function sendTestEmail() {
    const testEmailButton = document.querySelector('#a-send-test-mail');
    toggleButtonState(testEmailButton, true);

    try {
        await moongladeFetch2('/api/settings/email/test', 'POST', {});
        success('Email is sent.');
    } catch (err) {
        console.error('Failed to send test email:', err);
        error('Failed to send test email.');
    } finally {
        toggleButtonState(testEmailButton, false);
    }
}

document.getElementById('a-send-test-mail').addEventListener('click', sendTestEmail);

const form = document.querySelector('#form-settings');
form.addEventListener('submit', handleSettingsSubmit);
