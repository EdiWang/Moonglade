import { default as Alpine } from '/lib/alpinejs/alpinejs.3.15.0.module.esm.min.js';
import { fetch2 } from '/js/app/httpService.mjs?v=1500';
import { success } from '/js/app/toastService.mjs';

Alpine.data('mentionManager', () => ({
    mentions: [],
    isLoading: true,
    filterText: '',

    async init() {
        await this.loadMentions();
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
        if (!confirm('Delete this mention?')) return;

        await fetch2(`/api/mention/${mentionId}`, 'DELETE');
        this.mentions = this.mentions.filter(m => m.id !== mentionId);
        success('Mention deleted');
    },

    async clearAllMentions() {
        if (!confirm('Are you sure you want to clear all mentions?')) return;

        await fetch2('/api/mention/clear', 'DELETE');
        this.mentions = [];
        success('Mention logs are cleared');
    },

    formatTime(utcTime) {
        const date = new Date(utcTime);
        return date.toLocaleString();
    }
}));

Alpine.start();
