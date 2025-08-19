import { fetch2 } from './httpService.mjs?v=1427'
import { success } from './toastService.mjs'

const resetPasswordModal = new bootstrap.Modal('#resetPasswordModal');

function resetPassword() {
    document.querySelector('#resetpassword-form').reset();
    resetPasswordModal.show();
}

async function handleResetPasswordFormSubmit(event) {
    event.preventDefault();

    const data = new FormData(event.target);
    const value = Object.fromEntries(data.entries());

    await fetch2(`/api/settings/password/local`, 'PUT', value);

    document.querySelector('#resetpassword-form').reset();
    resetPasswordModal.hide();
    success('Password updated.');
}

document.getElementById('btn-reset-password').addEventListener('click', resetPassword);

const resetPasswordForm = document.querySelector('#resetpassword-form');
resetPasswordForm.addEventListener('submit', handleResetPasswordFormSubmit);