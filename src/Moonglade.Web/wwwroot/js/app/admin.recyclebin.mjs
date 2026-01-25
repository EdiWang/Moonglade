import { default as Alpine } from '/lib/alpinejs/alpinejs.3.15.0.module.esm.min.js';
import { fetch2 } from '/js/app/httpService.mjs?v=1500';
import { formatUtcTime, getLocalizedString } from './utils.module.mjs';
import { success } from '/js/app/toastService.mjs';

Alpine.data('recycleBinManager', () => ({
    posts: [],
    isLoading: true,
    deleteTargetId: null,
    deleteMessage: '',

    async init() {
        await this.loadPosts();
    },

    async loadPosts() {
        this.isLoading = true;
        try {
            const data = await fetch2('/api/post/list/recyclebin', 'GET');
            this.posts = (data.posts ?? []).sort((a, b) => 
                new Date(b.createTimeUtc) - new Date(a.createTimeUtc)
            );
            
            formatUtcTime();
        } finally {
            this.isLoading = false;
        }
    },

    confirmDelete(postId, postTitle) {
        this.deleteTargetId = postId;
        const template = getLocalizedString('confirmDelete');
        this.deleteMessage = template.replace('{0}', postTitle);
        const modal = new bootstrap.Modal(document.getElementById('deleteModal'));
        modal.show();
    },

    async executeDelete() {
        if (this.deleteTargetId) {
            await fetch2(`/api/post/${this.deleteTargetId}/destroy`, 'DELETE');
            this.posts = this.posts.filter(p => p.id !== this.deleteTargetId);
            success(getLocalizedString('postDeleted'));
            
            const modal = bootstrap.Modal.getInstance(document.getElementById('deleteModal'));
            modal.hide();
            this.deleteTargetId = null;
        }
    },

    async restorePost(postId) {
        await fetch2(`/api/post/${postId}/restore`, 'POST');
        this.posts = this.posts.filter(p => p.id !== postId);
        success(getLocalizedString('postRestored'));
    },

    confirmEmptyRecycleBin() {
        const modal = new bootstrap.Modal(document.getElementById('emptyBinModal'));
        modal.show();
    },

    async executeEmptyRecycleBin() {
        await fetch2('/api/post/recyclebin', 'DELETE');
        this.posts = [];
        success(getLocalizedString('cleared'));
        
        const modal = bootstrap.Modal.getInstance(document.getElementById('emptyBinModal'));
        modal.hide();
    },

    get hasPosts() {
        return this.posts.length > 0;
    },

    get sortedPosts() {
        return [...this.posts].sort((a, b) => 
            new Date(b.createTimeUtc) - new Date(a.createTimeUtc)
        );
    }
}));

Alpine.start();