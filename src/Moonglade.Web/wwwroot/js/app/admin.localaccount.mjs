import { Alpine } from './alpine-init.mjs';
import { fetch2 } from './httpService.mjs?v=1500';
import { success, error } from './toastService.mjs';
import { getLocalizedString } from './utils.module.mjs';

Alpine.data('accountManager', () => ({
    resetPasswordModal: null,
    resetAuthenticatorModal: null,
    formData: {
        oldPassword: '',
        newPassword: '',
        currentPassword: ''
    },

    async init() {
        this.resetPasswordModal = new bootstrap.Modal('#resetPasswordModal');
        this.resetAuthenticatorModal = new bootstrap.Modal('#resetAuthenticatorModal');
    },

    openResetPasswordModal() {
        this.formData.oldPassword = '';
        this.formData.newPassword = '';
        this.resetPasswordModal.show();
    },

    openResetAuthenticatorModal() {
        this.formData.currentPassword = '';
        this.resetAuthenticatorModal.show();
    },

    async resetPassword() {
        const payload = {
            OldPassword: this.formData.oldPassword,
            NewPassword: this.formData.newPassword
        };

        try {
            await fetch2('/api/account/password/local', 'PUT', payload);
            this.formData.oldPassword = '';
            this.formData.newPassword = '';
            this.resetPasswordModal.hide();
            success(getLocalizedString('passwordUpdated'));
        } catch (err) {
            error(err);
        }
    },

    async resetAuthenticator() {
        const payload = {
            CurrentPassword: this.formData.currentPassword
        };

        try {
            await fetch2('/api/account/totp/local/reset', 'PUT', payload);
            this.formData.currentPassword = '';
            this.resetAuthenticatorModal.hide();
            window.location.href = '/auth/signin';
        } catch (err) {
            error(err);
        }
    }
}));
