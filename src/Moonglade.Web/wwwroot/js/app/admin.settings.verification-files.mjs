import { Alpine } from './alpine-init.mjs';
import { fetch2 } from './httpService.mjs?v=1500';
import { success, error } from './toastService.mjs';
import { getLocalizedString } from './utils.module.mjs';
import { showDeleteConfirmModal, hideConfirmModal } from './adminModal.mjs';

const emptyFormData = () => ({
    fileName: '',
    content: '',
    isEnabled: true
});

const textEncoder = new TextEncoder();

Alpine.data('siteVerificationFileManager', () => ({
    files: [],
    isLoading: true,
    currentFileId: window.emptyGuid,
    editCanvas: null,
    formData: emptyFormData(),

    async init() {
        this.editCanvas = new bootstrap.Offcanvas(this.$refs.editFileCanvas);
        await this.loadFiles();
    },

    get hasFiles() {
        return this.files.length > 0;
    },

    get isCreateMode() {
        return this.currentFileId === window.emptyGuid;
    },

    get contentBytes() {
        return textEncoder.encode(this.formData.content ?? '').length;
    },

    async loadFiles() {
        this.isLoading = true;
        try {
            this.files = (await fetch2('/api/site-verification-files/list', 'GET')) ?? [];
        } catch (err) {
            error(err);
        } finally {
            this.isLoading = false;
        }
    },

    initCreateFile() {
        this.currentFileId = window.emptyGuid;
        this.formData = emptyFormData();
        this.editCanvas.show();
    },

    async editFile(id) {
        try {
            const file = await fetch2(`/api/site-verification-files/${id}`, 'GET');
            this.currentFileId = file.id;
            this.formData = {
                fileName: file.fileName,
                content: file.content,
                isEnabled: file.isEnabled
            };
            this.editCanvas.show();
        } catch (err) {
            error(err);
        }
    },

    async handleSubmit() {
        if (!this.formData.fileName) {
            error(getLocalizedString('fileNameRequired'));
            return;
        }

        if (!this.formData.content || !this.formData.content.trim()) {
            error(getLocalizedString('fileContentRequired'));
            return;
        }

        const isCreate = this.isCreateMode;
        const apiAddress = isCreate
            ? '/api/site-verification-files'
            : `/api/site-verification-files/${this.currentFileId}`;
        const verb = isCreate ? 'POST' : 'PUT';

        try {
            await fetch2(apiAddress, verb, this.formData);
            this.editCanvas.hide();
            await this.loadFiles();
            success(isCreate ? getLocalizedString('fileCreated') : getLocalizedString('fileUpdated'));
        } catch (err) {
            error(err);
        }
    },

    deleteFile(file) {
        showDeleteConfirmModal(getLocalizedString('deleteFile'), async () => {
            try {
                await fetch2(`/api/site-verification-files/${file.id}`, 'DELETE');
                hideConfirmModal();
                await this.loadFiles();
                success(getLocalizedString('fileDeleted'));
            } catch (err) {
                error(err);
            }
        });
    },

    async toggleFile(file) {
        try {
            await fetch2(`/api/site-verification-files/${file.id}/toggle`, 'POST', {
                isEnabled: !file.isEnabled
            });
            await this.loadFiles();
            success(file.isEnabled ? getLocalizedString('fileDisabled') : getLocalizedString('fileEnabled'));
        } catch (err) {
            error(err);
        }
    },

    async loadSelectedFile(event) {
        const file = event.target.files?.[0];
        if (!file) return;

        try {
            const content = await file.text();
            this.formData.fileName = file.name;
            this.formData.content = content;
            success(getLocalizedString('fileLoaded'));
        } catch (err) {
            error(getLocalizedString('fileReadFailed'));
        } finally {
            event.target.value = '';
        }
    },

    async copyUrl(fileName) {
        try {
            await navigator.clipboard.writeText(this.getFileUrl(fileName));
            success(getLocalizedString('urlCopied'));
        } catch (err) {
            error(err);
        }
    },

    getFileUrl(fileName) {
        return `${window.location.origin}/${fileName}`;
    },

    formatBytes(bytes) {
        if (bytes < 1024) return `${bytes} B`;
        return `${(bytes / 1024).toFixed(1)} KB`;
    },

    formatDate(dateString) {
        if (!dateString) return '';
        const normalized = dateString.endsWith('Z') ? dateString : `${dateString}Z`;
        const date = new Date(normalized);
        return isNaN(date.getTime()) ? dateString : date.toLocaleString();
    }
}));
