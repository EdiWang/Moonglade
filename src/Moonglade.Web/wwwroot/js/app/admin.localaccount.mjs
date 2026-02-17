import { Alpine } from '/js/app/alpine-init.mjs';
import { fetch2 } from '/js/app/httpService.mjs?v=1500';
import { success, error } from '/js/app/toastService.mjs';
import { getLocalizedString } from './utils.module.mjs';

Alpine.data('accountManager', () => ({
    resetPasswordModal: null,
    formData: {
        newUsername: '',
        oldPassword: '',
        newPassword: ''
    },

    async init() {
        this.resetPasswordModal = new bootstrap.Modal('#resetPasswordModal');
        await this.loadCurrentUsername();
    },

    async loadCurrentUsername() {
        try {
            const response = await fetch2('/auth/me', 'GET');
            if (response && response.userName) {
                this.formData.newUsername = response.userName.trim();
            }
        } catch (error) {
            console.error('Failed to load current username:', error);
        }
    },

    openResetPasswordModal() {
        this.formData.oldPassword = '';
        this.formData.newPassword = '';
        this.resetPasswordModal.show();
    },

    async resetPassword() {
        const payload = {
            NewUsername: this.formData.newUsername,
            OldPassword: this.formData.oldPassword,
            NewPassword: this.formData.newPassword
        };

        try {
            await fetch2('/api/settings/password/local', 'PUT', payload);
            this.formData.oldPassword = '';
            this.formData.newPassword = '';
            this.resetPasswordModal.hide();
            success(getLocalizedString('passwordUpdated'));
        } catch (err) {
            error(err);
        }
    }
}));

Alpine.start();