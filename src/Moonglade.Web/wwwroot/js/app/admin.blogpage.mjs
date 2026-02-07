import { Alpine } from '/js/app/alpine-init.mjs';
import { fetch2 } from '/js/app/httpService.mjs?v=1500';
import { success } from '/js/app/toastService.mjs';
import { getLocalizedString } from './utils.module.mjs';

Alpine.data('pageManager', () => ({
    pages: [],
    isLoading: true,
    pageToDelete: null,

    async init() {
        await this.loadPages();
    },

    async loadPages() {
        this.isLoading = true;
        try {
            this.pages = (await fetch2('/api/page/segment/list', 'GET')) ?? [];
        } finally {
            this.isLoading = false;
        }
    },

    get sortedPages() {
        return [...this.pages].sort((a, b) =>
            new Date(b.createTimeUtc) - new Date(a.createTimeUtc)
        );
    },

    get hasPages() {
        return this.pages.length > 0;
    },

    formatDate(dateString) {
        const date = new Date(dateString);
        return date.toLocaleString();
    },

    showDeleteModal(pageId) {
        this.pageToDelete = pageId;
        const modal = new bootstrap.Modal(document.getElementById('deleteConfirmModal'));
        modal.show();
    },

    async confirmDelete() {
        if (this.pageToDelete) {
            await fetch2(`/api/page/${this.pageToDelete}`, 'DELETE');
            await this.loadPages();
            success(getLocalizedString('pageDeleted'));
            
            const modal = bootstrap.Modal.getInstance(document.getElementById('deleteConfirmModal'));
            modal.hide();
            this.pageToDelete = null;
        }
    }
}));

Alpine.start();
