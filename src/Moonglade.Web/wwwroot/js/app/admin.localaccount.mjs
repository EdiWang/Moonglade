import { success } from '/js/app/toastService.mjs'

const resetPasswordModal = new bootstrap.Modal('#resetPasswordModal');

function resetPassword() {
    document.querySelector('#resetpassword-form').reset();
    resetPasswordModal.show();
}

function handleResetPasswordFormSubmit(event) {
    event.preventDefault();

    const data = new FormData(event.target);
    const value = Object.fromEntries(data.entries());

    callApi(`/api/settings/password/local`,
        'PUT',
        value,
        (resp) => {
            document.querySelector('#resetpassword-form').reset();
            resetPasswordModal.hide();
            success('Password updated.');
        });
}

document.getElementById('btn-reset-password').addEventListener('click', resetPassword);

const resetPasswordForm = document.querySelector('#resetpassword-form');
resetPasswordForm.addEventListener('submit', handleResetPasswordFormSubmit);