import { handleSettingsSubmit } from './admin.settings.js';
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
        await callApi('/api/settings/email/test', 'POST', {});
        success('Email is sent.');
    } catch (error) {
        console.error('Failed to send test email:', error);
        error('Failed to send test email.');
    } finally {
        toggleButtonState(testEmailButton, false);
    }
}

document.getElementById('a-send-test-mail').addEventListener('click', sendTestEmail);

const form = document.querySelector('#form-settings');
form.addEventListener('submit', handleSettingsSubmit);
