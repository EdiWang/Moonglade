import { Alpine } from '/js/app/alpine-init.mjs';
import { fetch2 } from '/js/app/httpService.mjs?v=1500';
import { success } from '/js/app/toastService.mjs';
import { getLocalizedString } from './utils.module.mjs';
import { showConfirmModal, hideConfirmModal } from './adminModal.mjs';

Alpine.data('pageManager', () => ({
    pages: [],
    isLoading: true,

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
        showConfirmModal({
            title: 'Confirm Delete',
            body: 'Are you sure you want to delete this page?',
            confirmText: 'Delete',
            confirmClass: 'btn-outline-danger',
            confirmIcon: 'bi-trash',
            onConfirm: async () => {
                await fetch2(`/api/page/${pageId}`, 'DELETE');
                await this.loadPages();
                success(getLocalizedString('pageDeleted'));
                hideConfirmModal();
            }
        });
    }
}));

Alpine.start();
