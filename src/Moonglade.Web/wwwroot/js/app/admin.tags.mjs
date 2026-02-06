import { default as Alpine } from '/lib/alpinejs/alpinejs.3.15.0.module.esm.min.js';
import { fetch2 } from '/js/app/httpService.mjs?v=1500';
import { success } from '/js/app/toastService.mjs';
import { getLocalizedString } from './utils.module.mjs';

Alpine.data('tagManager', () => ({
tags: [],
isLoading: true,
tagFilter: '',
editCanvas: null,
deleteModal: null,
deleteTarget: {
    tagId: null,
    tagName: ''
},
formData: {
    displayName: ''
},
originalTagNames: {},

async init() {
    this.editCanvas = new bootstrap.Offcanvas(this.$refs.editTagCanvas);
    this.deleteModal = new bootstrap.Modal(this.$refs.deleteModal);
    await this.loadTags();
},

    async loadTags() {
        this.isLoading = true;
        try {
            this.tags = (await fetch2('/api/tags/list/count', 'GET')) ?? [];
        } finally {
            this.isLoading = false;
        }
    },

    get hasTags() {
        return this.tags.length > 0;
    },

    get activeTags() {
        return this.tags
            .filter(t => t.postCount > 0)
            .sort((a, b) => a.displayName.localeCompare(b.displayName));
    },

    get inactiveTags() {
        return this.tags
            .filter(t => t.postCount === 0)
            .sort((a, b) => a.displayName.localeCompare(b.displayName));
    },

    get filteredActiveTags() {
        return this.filterTagList(this.activeTags);
    },

    get filteredInactiveTags() {
        return this.filterTagList(this.inactiveTags);
    },

    get hasActiveTags() {
        return this.filteredActiveTags.length > 0;
    },

    get hasInactiveTags() {
        return this.filteredInactiveTags.length > 0;
    },

    filterTagList(tagList) {
        if (!this.tagFilter) return tagList;
        const filterLower = this.tagFilter.toLowerCase();
        return tagList.filter(t => 
            t.displayName.toLowerCase().includes(filterLower)
        );
    },

    filterTags() {
        // Reactive filtering handled by computed properties
    },

    initCreateTag() {
        this.formData = { displayName: '' };
        this.editCanvas.show();
    },

    async updateTag(event, tagId) {
        const newTagName = event.target.textContent.trim();
        const originalTagName = this.originalTagNames[tagId] || 
            this.tags.find(t => t.id === tagId)?.displayName || '';

        if (newTagName === originalTagName || !newTagName) {
            event.target.textContent = originalTagName;
            return;
        }

        try {
            await fetch2(`/api/tags/${tagId}`, 'PUT', newTagName);
            this.originalTagNames[tagId] = newTagName;
            await this.loadTags();
            success(getLocalizedString('tagUpdated'));
        } catch (err) {
            event.target.textContent = originalTagName;
            console.error(err);
        }
    },

    async deleteTag(tagId, tagName) {
        this.deleteTarget = { tagId, tagName };
        this.deleteModal.show();
    },

    async confirmDelete() {
        try {
            await fetch2(`/api/tags/${this.deleteTarget.tagId}`, 'DELETE');
            this.deleteModal.hide();
            await this.loadTags();
            success(getLocalizedString('tagDeleted'));
        } catch (err) {
            console.error(err);
        }
    },

    async handleSubmit() {
        const tagName = this.formData.displayName.trim();
        if (!tagName) return;

        try {
            await fetch2('/api/tags', 'POST', tagName);
            this.formData = { displayName: '' };
            this.editCanvas.hide();
            await this.loadTags();
            success(getLocalizedString('tagAdded'));
        } catch (err) {
            console.error(err);
        }
    }
}));

Alpine.start();
