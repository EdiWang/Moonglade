import { Alpine } from './alpine-init.mjs';
import { fetch2 } from './httpService.mjs?v=1500';
import { success, error } from './toastService.mjs';
import { getLocalizedString } from './utils.module.mjs';
import { ImageUploader } from './imageuploader.mjs';

const avatarUploader = new ImageUploader('avatar', 300, 'image/jpeg');
avatarUploader.bindEvents();
document.getElementById('btn-upload-avatar').addEventListener('click', async () => {
    const settings = document.getElementById('account-settings');
    await avatarUploader.uploadImage(settings.dataset.avatarUploadUrl);
});

const gravatar = document.getElementById('account-gravatar');
gravatar.addEventListener('change', async () => {
    const enabled = gravatar.checked;
    gravatar.disabled = true;
    try {
        await fetch2(gravatar.dataset.saveUrl, 'PUT', enabled);
        success(getLocalizedString('imageUpdated'));
    } catch (err) {
        gravatar.checked = !enabled;
        error(err);
    } finally {
        gravatar.disabled = false;
    }
});

Alpine.data('accountManager', () => ({
    resetPasswordModal: null,
    resetAuthenticatorModal: null,
    emailModal: null,
    email: '',
    revision: null,
    emailLoaded: false,
    savingEmail: false,
    savingProfile: false,
    savingPassword: false,
    formData: {
        oldPassword: '',
        newPassword: '',
        currentPassword: '',
        email: '',
        emailPassword: ''
    },

    async init() {
        const settings = document.getElementById('account-settings');
        this.emailUrl = settings.dataset.emailUrl;
        this.signInUrl = settings.dataset.signinUrl;
        this.emailModal = new bootstrap.Modal('#ownerEmailModal');
        this.resetPasswordModal = new bootstrap.Modal('#resetPasswordModal');
        this.resetAuthenticatorModal = new bootstrap.Modal('#resetAuthenticatorModal');
        await this.loadEmail();
    },

    async updateProfile(form) {
        if (this.savingProfile) return;
        this.savingProfile = true;
        try {
            const profile = Object.fromEntries(new FormData(form));
            await fetch2(form.action, 'PUT', profile);
            window.location.reload();
        } catch (err) {
            error(err);
            this.savingProfile = false;
        }
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
        if (this.savingEmail || !this.emailLoaded) return;
        this.savingEmail = true;
        const changed = this.formData.email.trim() !== this.email;
        try {
            await fetch2(this.emailUrl, 'PUT', {
                Email: this.formData.email,
                Revision: this.revision,
                CurrentPassword: this.formData.emailPassword
            });
            this.formData.emailPassword = '';
            this.emailModal.hide();
            if (changed) {
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
        if (this.savingPassword) return;
        this.savingPassword = true;
        const payload = {
            OldPassword: this.formData.oldPassword,
            NewPassword: this.formData.newPassword
        };

        const emailWasLoaded = this.emailLoaded;
        this.emailLoaded = false;
        let passwordUpdated = false;
        try {
            await fetch2('/api/account/password/local', 'PUT', payload);
            passwordUpdated = true;
            this.formData.oldPassword = '';
            this.formData.newPassword = '';
            this.resetPasswordModal.hide();
            await this.loadEmail();
            success(getLocalizedString('passwordUpdated'));
        } catch (err) {
            if (!passwordUpdated) this.emailLoaded = emailWasLoaded;
            error(err);
        } finally {
            this.savingPassword = false;
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
