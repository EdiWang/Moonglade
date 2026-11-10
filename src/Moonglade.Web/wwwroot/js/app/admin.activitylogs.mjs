import { Alpine } from './alpine-init.mjs';
import { fetch2 } from './httpService.mjs?v=1500';
import { formatUtcTime, getLocalizedString, localDateBoundaryToUtcIso } from './utils.module.mjs';
import { success, error } from './toastService.mjs';
import { showConfirmModal, hideConfirmModal } from './adminModal.mjs';
import { withPagination } from './admin.pagination.mjs';
import { createOffcanvas } from './admin.offcanvas.mjs';

Alpine.data('activityLogManager', () => withPagination(10, {
    logs: [],
    eventTypeGroups: [],
    isLoading: true,
    selectedEventKeys: [],
    startDate: '',
    endDate: '',
    detailCanvas: null,
    filterCanvas: null,
    currentDetail: null,

    async init() {
        this.initPageFromUrl();

        this.detailCanvas = new bootstrap.Offcanvas(this.$refs.detailCanvas);
        this.filterCanvas = createOffcanvas(this.$refs.filterCanvas);

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

            if (this.selectedEventKeys.length > 0) {
                this.selectedEventKeys.forEach(key => params.append('eventKeys', key));
            }

            if (this.startDate) {
                const startUtc = localDateBoundaryToUtcIso(this.startDate);
                if (startUtc) params.append('startTimeUtc', startUtc);
            }

            if (this.endDate) {
                const endUtc = localDateBoundaryToUtcIso(this.endDate, true);
                if (endUtc) params.append('endTimeUtc', endUtc);
            }

            const data = await fetch2(`/api/activitylog/list?${params.toString()}`, 'GET');
            this.logs = data.items ?? [];
            this.totalRows = data.totalItemCount ?? 0;

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
        this.selectedEventKeys = [];
        this.startDate = '';
        this.endDate = '';
        this.currentPage = 1;
        await this.loadData();
        this.updateUrl();
        this.filterCanvas.hide();
    },

    clearAllLogs() {
        const clearAll = getLocalizedString('clearAll');
        showConfirmModal({
            title: clearAll,
            body: getLocalizedString('confirmClearAllLogs'),
            confirmText: clearAll,
            confirmClass: 'btn-outline-danger',
            confirmIcon: 'bi-trash',
            onConfirm: async () => {
                try {
                    await fetch2('/api/activitylog/clear', 'DELETE');
                    this.currentPage = 1;
                    await this.loadData();
                    success(getLocalizedString('logsCleared'));
                } catch (err) {
                    error(err);
                } finally {
                    hideConfirmModal();
                }
            }
        });
    },

    async showDetail(logId, trigger) {
        this.currentDetail = null;
        try {
            this.currentDetail = await fetch2(`/api/activitylog/${logId}/metadata`, 'GET');
            if (trigger) {
                this.$refs.detailCanvas.addEventListener('hidden.bs.offcanvas', () => trigger.focus(), { once: true });
            }
            this.detailCanvas.show();
            this.$nextTick(() => formatUtcTime());
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

    toggleEventKey(value) {
        const index = this.selectedEventKeys.indexOf(value);
        if (index === -1) {
            this.selectedEventKeys.push(value);
        } else {
            this.selectedEventKeys.splice(index, 1);
        }
    },

    get activeFilterCount() {
        let count = this.selectedEventKeys.length;
        if (this.startDate) count++;
        if (this.endDate) count++;
        return count;
    },

    get hasDetailContext() {
        const detail = this.currentDetail;
        return !!(detail?.targetId || detail?.targetDisplayName || detail?.ipAddress || detail?.userAgent);
    },

    get hasSettingsChanges() {
        return Array.isArray(this.currentDetail?.changes);
    },

    formatDetailValue(value, key) {
        if (value === null || value === undefined) return '\u2014';
        if (typeof value === 'boolean') return getLocalizedString(value ? 'yes' : 'no');
        if (Array.isArray(value)) return value.map(item => this.formatDetailValue(item, key)).join(', ');
        if (key === 'PostStatus' && ['Draft', 'Published', 'Scheduled'].includes(value)) {
            return getLocalizedString(value.toLowerCase());
        }
        const choices = {
            SideBarOption: { Right: 'right', Left: 'left', Disabled: 'disabled' },
            DefaultEditor: { Html: 'html', Markdown: 'markdown' },
            PostTitleAlignment: { Left: 'left', Center: 'center' },
            CommentProvider: { BuiltIn: 'builtIn', ThirdParty: 'thirdParty' },
            CommentOrder: { OldToNew: 'oldToNew', NewToOld: 'newToOld' },
            WordFilterMode: { Mask: 'maskWord', Block: 'blockComment' }
        };
        const label = choices[key]?.[value];
        if (label) return getLocalizedString(label);
        return String(value);
    }
}, [10, 20, 30, 40, 50]));
