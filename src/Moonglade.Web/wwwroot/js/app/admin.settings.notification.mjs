import { fetch2 } from './httpService.mjs?v=1500'
import { initializeSettings } from './admin.settings.mjs';
import { success, error } from './toastService.mjs';
import { getLocalizedString } from './utils.module.mjs';

const form = document.getElementById('form-settings');
const testEmailButton = document.getElementById('a-send-test-mail');
let canSendTestEmail = !testEmailButton.disabled;
let sendingTestEmail = false;

function updateTestEmailButton() {
    testEmailButton.disabled = sendingTestEmail || !canSendTestEmail;
}

async function sendTestEmail() {
    if (testEmailButton.disabled) {
        return;
    }

    sendingTestEmail = true;
    updateTestEmailButton();

    try {
        const queued = await fetch2('/api/settings/email/test', 'POST', {});
        if (queued === true) {
            success(getLocalizedString('emailSent'));
        }
    } catch (err) {
        error(err);
    } finally {
        sendingTestEmail = false;
        updateTestEmailButton();
    }
}

testEmailButton.addEventListener('click', sendTestEmail);

initializeSettings({
    onSaved(fields) {
        if ('EnableEmailSending' in fields) {
            canSendTestEmail = form.dataset.emailCapabilityState === 'Available' && fields.EnableEmailSending;
            updateTestEmailButton();
        }
    }
});
