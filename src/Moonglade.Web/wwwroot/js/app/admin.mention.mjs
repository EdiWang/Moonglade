import { Alpine } from './alpine-init.mjs';
import { fetch2 } from './httpService.mjs?v=1500';
import { success, error } from './toastService.mjs';
import { getLocalizedString } from './utils.module.mjs';
import { showConfirmModal, hideConfirmModal } from './adminModal.mjs';

Alpine.data('mentionManager', () => ({
mentions: [],
isLoading: true,
filterText: '',
selectedIds: [],

async init() {
    await this.loadMentions();
},

    async loadMentions() {
        this.isLoading = true;
        try {
            this.mentions = (await fetch2('/api/mention/list', 'GET')) ?? [];
        } catch (err) {
            error(err);
        } finally {
            this.isLoading = false;
        }
    },

    get sortedMentions() {
        return [...this.mentions].sort((a, b) => 
            new Date(b.pingTimeUtc) - new Date(a.pingTimeUtc)
        );
    },

    get filteredMentions() {
        if (!this.filterText) {
            return this.sortedMentions;
        }

        const filter = this.filterText.toLowerCase();
        return this.sortedMentions.filter(item => {
            const searchText = `${item.sourceTitle} ${item.targetPostTitle} ${item.domain} ${item.sourceIp} ${item.worker}`.toLowerCase();
            return searchText.includes(filter);
        });
    },

    get hasMentions() {
        return this.mentions.length > 0;
    },

    get mentionCount() {
        return this.filteredMentions.length;
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
                    this.mentions = this.mentions.filter(m => m.id !== mentionId);
                    this.selectedIds = this.selectedIds.filter(id => id !== mentionId);
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
                    this.mentions = this.mentions.filter(m => !idsToDelete.includes(m.id));
                    this.selectedIds = this.selectedIds.filter(id => !idsToDelete.includes(id));
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
    }
}));
