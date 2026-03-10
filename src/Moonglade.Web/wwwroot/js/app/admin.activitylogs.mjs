import { Alpine } from './alpine-init.mjs';
import { fetch2 } from './httpService.mjs?v=1500';
import { formatUtcTime, getLocalizedString } from './utils.module.mjs';
import { success, error } from './toastService.mjs';
import { showConfirmModal, hideConfirmModal } from './adminModal.mjs';
import { withPagination } from './admin.pagination.mjs';

Alpine.data('activityLogManager', () => withPagination(10, {
    logs: [],
    eventTypeGroups: [],
    isLoading: true,
    selectedEventTypes: [],
    startDate: '',
    endDate: '',
    detailModal: null,
    filterCanvas: null,
    currentMetadata: null,
    currentLogOperation: '',

    async init() {
        this.initPageFromUrl();

        this.detailModal = new bootstrap.Modal(document.getElementById('detailModal'));
        this.filterCanvas = new bootstrap.Offcanvas(this.$refs.filterCanvas);

        await this.loadEventTypes();
        await this.loadData();
    },

    async loadEventTypes() {
        try {
            this.eventTypeGroups = await fetch2('/api/activitylog/eventtypes', 'GET');
        } catch (err) {
            error(err);
        }
    },

    async loadData() {
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
        await this.loadData();
        this.updateUrl();
        this.filterCanvas.hide();
    },

    async clearFilter() {
        this.selectedEventTypes = [];
        this.startDate = '';
        this.endDate = '';
        this.currentPage = 1;
        await this.loadData();
        this.updateUrl();
        this.filterCanvas.hide();
    },

    deleteLog(logId) {
        showConfirmModal({
            title: 'Confirm Delete',
            body: getLocalizedString('confirmDeleteLog'),
            confirmText: 'Delete',
            confirmClass: 'btn-outline-danger',
            confirmIcon: 'bi-trash',
            onConfirm: async () => {
                try {
                    await fetch2(`/api/activitylog/${logId}`, 'DELETE');
                    await this.loadData();
                    success(getLocalizedString('logDeleted'));
                } catch (err) {
                    error(err);
                } finally {
                    hideConfirmModal();
                }
            }
        });
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
}, [10, 20, 30, 40, 50]));
