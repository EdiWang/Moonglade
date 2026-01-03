import { default as Alpine } from '/lib/alpinejs/alpinejs.3.15.0.module.esm.min.js';
import { fetch2 } from '/js/app/httpService.mjs?v=1500';
import { success } from '/js/app/toastService.mjs';

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

    async deletePage(pageId) {
        if (confirm('Delete Confirmation?')) {
            await fetch2(`/api/page/${pageId}`, 'DELETE');
            await this.loadPages();
            success('Page deleted');
        }
    }
}));

Alpine.start();
