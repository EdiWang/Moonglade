import { default as Alpine } from '/lib/alpinejs/alpinejs.3.15.0.module.esm.min.js';
import { fetch2 } from '/js/app/httpService.mjs?v=1500';
import { success } from '/js/app/toastService.mjs';

Alpine.data('mentionManager', () => ({
mentions: [],
isLoading: true,
filterText: '',
pendingDeleteId: null,
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
        this.pendingDeleteId = mentionId;
        this.deleteModal.show();
    },

    async confirmDeleteMention() {
        if (!this.pendingDeleteId) return;

        await fetch2(`/api/mention/${this.pendingDeleteId}`, 'DELETE');
        this.mentions = this.mentions.filter(m => m.id !== this.pendingDeleteId);
        success('Mention deleted');
        
        this.deleteModal.hide();
        this.pendingDeleteId = null;
    },

    async clearAllMentions() {
        this.clearAllModal.show();
    },

    async confirmClearAllMentions() {
        await fetch2('/api/mention/clear', 'DELETE');
        this.mentions = [];
        success('Mention logs are cleared');
        
        this.clearAllModal.hide();
    },

    formatTime(utcTime) {
        const date = new Date(utcTime);
        return date.toLocaleString();
    }
}));

Alpine.start();
