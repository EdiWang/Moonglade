import { Alpine } from './alpine-init.mjs';
import { fetch2 } from './httpService.mjs?v=1500';
import { success, error } from './toastService.mjs';
import { getLocalizedString } from './utils.module.mjs';

Alpine.data('accountManager', () => ({
    resetPasswordModal: null,
    resetAuthenticatorModal: null,
    emailModal: null,
    email: '',
    revision: null,
    emailLoaded: false,
    savingEmail: false,
    localAuth: false,
    formData: {
        oldPassword: '',
        newPassword: '',
        currentPassword: '',
        email: '',
        emailPassword: ''
    },

    async init() {
        const settings = document.getElementById('account-settings');
        this.localAuth = settings.dataset.localAuth === 'true';
        this.emailUrl = settings.dataset.emailUrl;
        this.signInUrl = settings.dataset.signinUrl;
        this.emailModal = new bootstrap.Modal('#ownerEmailModal');
        if (this.localAuth) {
            this.resetPasswordModal = new bootstrap.Modal('#resetPasswordModal');
            this.resetAuthenticatorModal = new bootstrap.Modal('#resetAuthenticatorModal');
        }
        await this.loadEmail();
    },

    async loadEmail() {
        this.emailLoaded = false;
        try {
            const result = await fetch2(this.emailUrl, 'GET');
            this.email = result.email ?? '';
            this.revision = result.revision;
            this.emailLoaded = true;
        } catch (err) {
            error(err);
        }
    },

    openEmailModal() {
        this.formData.email = this.email;
        this.formData.emailPassword = '';
        this.emailModal.show();
    },

    async updateEmail() {
        if (this.savingEmail) return;
        this.savingEmail = true;
        const changed = this.formData.email.trim() !== this.email;
        try {
            await fetch2(this.emailUrl, 'PUT', {
                Email: this.formData.email,
                Revision: this.revision,
                CurrentPassword: this.localAuth ? this.formData.emailPassword : null
            });
            this.formData.emailPassword = '';
            this.emailModal.hide();
            if (this.localAuth && changed) {
                window.location.href = this.signInUrl;
                return;
            }
            await this.loadEmail();
            success(getLocalizedString('emailUpdated'));
        } catch (err) {
            error(err);
        } finally {
            this.savingEmail = false;
        }
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
