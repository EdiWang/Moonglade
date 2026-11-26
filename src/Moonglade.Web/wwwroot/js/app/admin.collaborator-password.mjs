import { fetch2 } from './httpService.mjs?v=1500';
import { success, error } from './toastService.mjs';

const form = document.getElementById('collaborator-password-form');
const submitButton = form.querySelector('button[type="submit"]');

form.addEventListener('submit', async (event) => {
    event.preventDefault();
    if (submitButton.disabled) return;

    submitButton.disabled = true;
    try {
        await fetch2(form.dataset.saveUrl, 'PUT', {
            OldPassword: form.elements.OldPassword.value,
            NewPassword: form.elements.NewPassword.value
        });
        form.reset();
        success(form.dataset.success);
    } catch (err) {
        error(err);
    } finally {
        submitButton.disabled = false;
    }
});
