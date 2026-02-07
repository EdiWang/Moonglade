import { default as Alpine } from '/lib/alpinejs/alpinejs.3.15.8.module.esm.min.js';
import { fetch2 } from '/js/app/httpService.mjs?v=1500';
import { success } from '/js/app/toastService.mjs';
import { getLocalizedString } from './utils.module.mjs';

Alpine.data('mentionManager', () => ({
mentions: [],
isLoading: true,
filterText: '',
selectedIds: [],
pendingDeleteIds: [],
deleteModal: null,
clearAllModal: null,

async init() {
    await this.loadMentions();
        
    // Initialize Bootstrap modals
    this.deleteModal = new bootstrap.Modal(document.getElementById('deleteMentionModal'));
    this.clearAllModal = new bootstrap.Modal(document.getElementById('clearAllMentionsModal'));
},

    async loadMentions() {
        this.isLoading = true;
        try {
            this.mentions = (await fetch2('/api/mention/list', 'GET')) ?? [];
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

    async deleteMention(mentionId) {
        this.pendingDeleteIds = [mentionId];
        this.deleteModal.show();
    },

    async deleteSelectedMentions() {
        if (this.selectedIds.length === 0) return;
        
        this.pendingDeleteIds = [...this.selectedIds];
        this.deleteModal.show();
    },

    async confirmDeleteMention() {
        if (!this.pendingDeleteIds || this.pendingDeleteIds.length === 0) return;

        await fetch2('/api/mention', 'DELETE', this.pendingDeleteIds);
        this.mentions = this.mentions.filter(m => !this.pendingDeleteIds.includes(m.id));
        this.selectedIds = this.selectedIds.filter(id => !this.pendingDeleteIds.includes(id));
        
        const count = this.pendingDeleteIds.length;
        let message;
        if (count === 1) {
            message = getLocalizedString('mentionDeleted');
        } else {
            const template = getLocalizedString('mentionsDeleted');
            message = template.replace('{0}', count);
        }
        success(message);
        
        this.deleteModal.hide();
        this.pendingDeleteIds = [];
    },

    async clearAllMentions() {
        this.clearAllModal.show();
    },

    async confirmClearAllMentions() {
        await fetch2('/api/mention/clear', 'DELETE');
        this.mentions = [];
        success(getLocalizedString('mentionsCleared'));
        
        this.clearAllModal.hide();
    },

    formatTime(utcTime) {
        const date = new Date(utcTime);
        return date.toLocaleString();
    }
}));

Alpine.start();
