import { default as Alpine } from '/lib/alpinejs/alpinejs.3.15.8.module.esm.min.js';
import { fetch2 } from '/js/app/httpService.mjs?v=1500';
import { success } from '/js/app/toastService.mjs';
import { getLocalizedString } from './utils.module.mjs';

Alpine.data('accountManager', () => ({
    loginHistory: [],
    isLoading: true,
    resetPasswordModal: null,
    formData: {
        newUsername: '',
        oldPassword: '',
        newPassword: ''
    },

    async init() {
        this.resetPasswordModal = new bootstrap.Modal('#resetPasswordModal');
        await this.loadLoginHistory();
        await this.loadCurrentUsername();
    },

    async loadLoginHistory() {
        this.isLoading = true;
        try {
            const data = await fetch2('/auth/loginhistory/list', 'GET');
            this.loginHistory = (data ?? [])
                .sort((a, b) => new Date(b.loginTimeUtc) - new Date(a.loginTimeUtc))
                .slice(0, 10);
        } finally {
            this.isLoading = false;
        }
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

    get hasLoginHistory() {
        return this.loginHistory.length > 0;
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

        await fetch2('/api/settings/password/local', 'PUT', payload);

        this.formData.oldPassword = '';
        this.formData.newPassword = '';
        this.resetPasswordModal.hide();
        success(getLocalizedString('passwordUpdated'));
    },

    formatTime(utcTime) {
        const date = new Date(utcTime);
        return date.toLocaleString();
    }
}));

Alpine.start();