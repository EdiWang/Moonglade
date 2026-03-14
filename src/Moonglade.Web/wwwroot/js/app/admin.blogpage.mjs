import { Alpine } from './alpine-init.mjs';
import { fetch2 } from './httpService.mjs?v=1500';
import { success, error } from './toastService.mjs';
import { getLocalizedString, formatDateString } from './utils.module.mjs';
import { showDeleteConfirmModal, hideConfirmModal } from './adminModal.mjs';

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
        } catch (err) {
            error(err);
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
        return formatDateString(dateString);
    },

    showDeleteModal(pageId) {
        showDeleteConfirmModal('Are you sure you want to delete this page?', async () => {
            try {
                await fetch2(`/api/page/${pageId}`, 'DELETE');
                await this.loadPages();
                success(getLocalizedString('pageDeleted'));
            } catch (err) {
                error(err);
            } finally {
                hideConfirmModal();
            }
        });
    }
}));
