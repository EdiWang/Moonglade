import { Alpine } from './alpine-init.mjs';
import { fetch2 } from './httpService.mjs?v=1500';
import { success, error } from './toastService.mjs';
import { formatUtcTime, getLocalizedString } from './utils.module.mjs';
import { showConfirmModal, hideConfirmModal } from './adminModal.mjs';
import { withPagination } from './admin.pagination.mjs';

Alpine.data('mentionManager', () => withPagination(10, {
mentions: [],
isLoading: true,
selectedIds: [],
domainFilter: '',
sourceTitleFilter: '',
targetPostTitleFilter: '',
startDate: '',
endDate: '',
filterCanvas: null,

async init() {
    this.initPageFromUrl();
    this.filterCanvas = new bootstrap.Offcanvas(this.$refs.filterCanvas);
    await this.loadData();
},

    async loadData() {
        this.isLoading = true;
        try {
            const params = new URLSearchParams({
                pageIndex: this.currentPage,
                pageSize: this.pageSize
            });

            if (this.domainFilter) {
                params.append('domain', this.domainFilter);
            }

            if (this.sourceTitleFilter) {
                params.append('sourceTitle', this.sourceTitleFilter);
            }

            if (this.targetPostTitleFilter) {
                params.append('targetPostTitle', this.targetPostTitleFilter);
            }

            if (this.startDate) {
                const startUtc = new Date(this.startDate).toISOString();
                params.append('startTimeUtc', startUtc);
            }

            if (this.endDate) {
                const endUtc = new Date(this.endDate + 'T23:59:59').toISOString();
                params.append('endTimeUtc', endUtc);
            }

            const data = await fetch2(`/api/mention/list?${params.toString()}`, 'GET');
            this.mentions = data.items ?? [];
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

    get hasMentions() {
        return this.mentions.length > 0;
    },

    get mentionCount() {
        return this.totalRows;
    },

    deleteMention(mentionId) {
        showConfirmModal({
            title: 'Confirm Delete',
            body: getLocalizedString('confirmDeleteMention'),
            confirmText: 'Delete',
            confirmClass: 'btn-outline-danger',
            confirmIcon: 'bi-trash',
            onConfirm: async () => {
                try {
                    await fetch2('/api/mention', 'DELETE', [mentionId]);
                    this.selectedIds = this.selectedIds.filter(id => id !== mentionId);
                    await this.loadData();
                    success(getLocalizedString('mentionDeleted'));
                } catch (err) {
                    error(err);
                } finally {
                    hideConfirmModal();
                }
            }
        });
    },

    deleteSelectedMentions() {
        if (this.selectedIds.length === 0) return;

        const idsToDelete = [...this.selectedIds];
        const count = idsToDelete.length;
        const body = count === 1
            ? getLocalizedString('confirmDeleteMention')
            : getLocalizedString('confirmDeleteMentions').replace('{0}', count);

        showConfirmModal({
            title: 'Confirm Delete',
            body: body,
            confirmText: 'Delete',
            confirmClass: 'btn-outline-danger',
            confirmIcon: 'bi-trash',
            onConfirm: async () => {
                try {
                    await fetch2('/api/mention', 'DELETE', idsToDelete);
                    this.selectedIds = [];
                    await this.loadData();
                    const template = getLocalizedString('mentionsDeleted');
                    success(template.replace('{0}', count));
                } catch (err) {
                    error(err);
                } finally {
                    hideConfirmModal();
                }
            }
        });
    },

    clearAllMentions() {
        showConfirmModal({
            title: 'Confirm Clear All',
            body: getLocalizedString('confirmClearAll'),
            confirmText: 'Clear All',
            confirmClass: 'btn-outline-danger',
            confirmIcon: 'bi-trash',
            onConfirm: async () => {
                try {
                    await fetch2('/api/mention/clear', 'DELETE');
                    this.mentions = [];
                    this.totalRows = 0;
                    this.selectedIds = [];
                    success(getLocalizedString('mentionsCleared'));
                } catch (err) {
                    error(err);
                } finally {
                    hideConfirmModal();
                }
            }
        });
    },

    formatTime(utcTime) {
        const date = new Date(utcTime);
        return date.toLocaleString();
    },

    openFilter() {
        this.filterCanvas.show();
    },

    async handleFilter() {
        this.currentPage = 1;
        this.selectedIds = [];
        await this.loadData();
        this.updateUrl();
        this.filterCanvas.hide();
    },

    async clearFilter() {
        this.domainFilter = '';
        this.sourceTitleFilter = '';
        this.targetPostTitleFilter = '';
        this.startDate = '';
        this.endDate = '';
        this.currentPage = 1;
        this.selectedIds = [];
        await this.loadData();
        this.updateUrl();
        this.filterCanvas.hide();
    },

    get activeFilterCount() {
        let count = 0;
        if (this.domainFilter) count++;
        if (this.sourceTitleFilter) count++;
        if (this.targetPostTitleFilter) count++;
        if (this.startDate) count++;
        if (this.endDate) count++;
        return count;
    }
}, [10, 20, 30, 40, 50]));
