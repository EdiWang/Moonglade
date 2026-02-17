import { Alpine } from '/js/app/alpine-init.mjs';
import { fetch2 } from '/js/app/httpService.mjs?v=1500';
import { formatUtcTime, getLocalizedString } from './utils.module.mjs';
import { success, error } from '/js/app/toastService.mjs';

Alpine.data('activityLogManager', () => ({
    logs: [],
    eventTypeGroups: [],
    isLoading: true,
    currentPage: 1,
    pageSize: 10,
    totalRows: 0,
    selectedEventTypes: [],
    startDate: '',
    endDate: '',
    deleteModal: null,
    detailModal: null,
    filterCanvas: null,
    pendingDeleteId: null,
    currentMetadata: null,
    currentLogOperation: '',

    async init() {
        const urlParams = new URLSearchParams(window.location.search);
        this.currentPage = parseInt(urlParams.get('pageIndex')) || 1;

        this.deleteModal = new bootstrap.Modal(document.getElementById('deleteLogModal'));
        this.detailModal = new bootstrap.Modal(document.getElementById('detailModal'));
        this.filterCanvas = new bootstrap.Offcanvas(this.$refs.filterCanvas);

        await this.loadEventTypes();
        await this.loadLogs();
    },

    async loadEventTypes() {
        try {
            this.eventTypeGroups = await fetch2('/api/activitylog/eventtypes', 'GET');
        } catch (err) {
            error(err);
        }
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

            this.$nextTick(() => {
                formatUtcTime();
            });
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
        this.filterCanvas.hide();
    },

    async clearFilter() {
        this.selectedEventTypes = [];
        this.startDate = '';
        this.endDate = '';
        this.currentPage = 1;
        await this.loadLogs();
        this.updateUrl();
        this.filterCanvas.hide();
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

    openFilter() {
        this.filterCanvas.show();
    },

    toggleEventType(value) {
        const index = this.selectedEventTypes.indexOf(value);
        if (index === -1) {
            this.selectedEventTypes.push(value);
        } else {
            this.selectedEventTypes.splice(index, 1);
        }
    },

    get activeFilterCount() {
        let count = this.selectedEventTypes.length;
        if (this.startDate) count++;
        if (this.endDate) count++;
        return count;
    },

    getEventTypeName(eventType) {
        // eventType from API is a string (enum name like "PostCreated")
        // We need to match it with the item.name after converting from camelCase
        for (const group of this.eventTypeGroups) {
            const item = group.items.find(i => {
                // Convert "Post Created" back to "PostCreated" and compare
                const enumName = i.name.replace(/\s+/g, '');
                return enumName === eventType;
            });
            if (item) return item.name;
        }
        return 'Unknown';
    }
}));

Alpine.start();
