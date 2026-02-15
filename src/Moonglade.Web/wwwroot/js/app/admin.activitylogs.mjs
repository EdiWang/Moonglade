import { Alpine } from '/js/app/alpine-init.mjs';
import { fetch2 } from '/js/app/httpService.mjs?v=1500';
import { formatUtcTime, getLocalizedString } from './utils.module.mjs';
import { success, error } from '/js/app/toastService.mjs';

Alpine.data('activityLogManager', () => ({
    logs: [],
    isLoading: true,
    currentPage: 1,
    pageSize: 10,
    totalRows: 0,
    selectedEventTypes: [],
    startDate: '',
    endDate: '',
    deleteModal: null,
    detailModal: null,
    pendingDeleteId: null,
    currentMetadata: null,
    currentLogOperation: '',

    async init() {
        const urlParams = new URLSearchParams(window.location.search);
        this.currentPage = parseInt(urlParams.get('pageIndex')) || 1;
        
        this.deleteModal = new bootstrap.Modal(document.getElementById('deleteLogModal'));
        this.detailModal = new bootstrap.Modal(document.getElementById('detailModal'));
        
        await this.loadLogs();
    },

    async loadLogs() {
        this.isLoading = true;
        try {
            const params = new URLSearchParams({
                pageIndex: this.currentPage,
                pageSize: this.pageSize
            });

            if (this.selectedEventTypes.length > 0) {
                this.selectedEventTypes.forEach(et => params.append('eventTypes', et));
            }

            if (this.startDate) {
                const startUtc = new Date(this.startDate).toISOString();
                params.append('startTimeUtc', startUtc);
            }

            if (this.endDate) {
                const endUtc = new Date(this.endDate + 'T23:59:59').toISOString();
                params.append('endTimeUtc', endUtc);
            }

            const data = await fetch2(`/api/activitylog/list?${params.toString()}`, 'GET');
            this.logs = data.logs ?? [];
            this.totalRows = data.totalCount ?? 0;

            formatUtcTime();
        } catch (err) {
            error(err);
        } finally {
            this.isLoading = false;
        }
    },

    async handleFilter() {
        this.currentPage = 1;
        await this.loadLogs();
        this.updateUrl();
    },

    async clearFilter() {
        this.selectedEventTypes = [];
        this.startDate = '';
        this.endDate = '';
        this.currentPage = 1;
        await this.loadLogs();
        this.updateUrl();
    },

    async goToPage(page) {
        this.currentPage = page;
        await this.loadLogs();
        this.updateUrl();
        window.scrollTo(0, 0);
    },

    updateUrl() {
        const params = new URLSearchParams();
        params.set('pageIndex', this.currentPage);
        window.history.pushState({}, '', `?${params.toString()}`);
    },

    async deleteLog(logId) {
        this.pendingDeleteId = logId;
        this.deleteModal.show();
    },

    async confirmDelete() {
        if (!this.pendingDeleteId) return;

        try {
            await fetch2(`/api/activitylog/${this.pendingDeleteId}`, 'DELETE');
            await this.loadLogs();
            success(getLocalizedString('logDeleted'));
        } catch (err) {
            error(err);
        } finally {
            this.deleteModal.hide();
            this.pendingDeleteId = null;
        }
    },

    async showDetail(logId, operation) {
        this.currentLogOperation = operation;
        try {
            this.currentMetadata = await fetch2(`/api/activitylog/${logId}/metadata`, 'GET');
            this.detailModal.show();
        } catch (err) {
            error(err);
        }
    },

    get hasLogs() {
        return this.logs.length > 0;
    },

    get totalPages() {
        return Math.ceil(this.totalRows / this.pageSize);
    },

    get paginationPages() {
        const pages = [];
        const maxVisible = 5;
        let startPage = Math.max(1, this.currentPage - Math.floor(maxVisible / 2));
        let endPage = Math.min(this.totalPages, startPage + maxVisible - 1);

        if (endPage - startPage < maxVisible - 1) {
            startPage = Math.max(1, endPage - maxVisible + 1);
        }

        for (let i = startPage; i <= endPage; i++) {
            pages.push(i);
        }
        return pages;
    },

    getEventTypeName(eventType) {
        const eventTypeNames = {
            0: 'Default',
            100: 'Category Created',
            101: 'Category Updated',
            102: 'Category Deleted',
            200: 'Post Schedule Cancelled',
            201: 'Post Schedule Postponed',
            202: 'Post Restored',
            203: 'Post Permanently Deleted',
            204: 'Recycle Bin Cleared',
            205: 'Post Created',
            206: 'Post Updated',
            207: 'Post Deleted',
            208: 'Post Published',
            209: 'Post Unpublished',
            300: 'Comment Created',
            301: 'Comment Approval Toggled',
            302: 'Comment Deleted',
            303: 'Comment Replied',
            400: 'Page Created',
            401: 'Page Updated',
            402: 'Page Deleted',
            600: 'Tag Created',
            601: 'Tag Updated',
            602: 'Tag Deleted',
            700: 'Theme Created',
            701: 'Theme Deleted',
            800: 'Settings General Updated',
            801: 'Settings Content Updated',
            802: 'Settings Comment Updated',
            803: 'Settings Notification Updated',
            804: 'Settings Subscription Updated',
            805: 'Settings Image Updated',
            806: 'Settings Advanced Updated',
            807: 'Settings Appearance Updated',
            808: 'Settings Custom Menu Updated',
            809: 'Settings Password Updated',
            810: 'Image Uploaded',
            820: 'Avatar Updated',
            821: 'Site Icon Updated',
            850: 'Widget Created',
            851: 'Widget Updated',
            852: 'Widget Deleted',
            900: 'Activity Log Deleted'
        };
        return eventTypeNames[eventType] || 'Unknown';
    }
}));

Alpine.start();
