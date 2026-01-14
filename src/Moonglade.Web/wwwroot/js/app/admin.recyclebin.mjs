import { default as Alpine } from '/lib/alpinejs/alpinejs.3.15.0.module.esm.min.js';
import { fetch2 } from '/js/app/httpService.mjs?v=1500';
import { formatUtcTime } from './utils.module.mjs';
import { success } from '/js/app/toastService.mjs';

Alpine.data('recycleBinManager', () => ({
    posts: [],
    isLoading: true,

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

    async deletePost(postId) {
        if (confirm('Delete Confirmation?')) {
            await fetch2(`/api/post/${postId}/destroy`, 'DELETE');
            this.posts = this.posts.filter(p => p.id !== postId);
            success('Post deleted');
        }
    },

    async restorePost(postId) {
        await fetch2(`/api/post/${postId}/restore`, 'POST');
        this.posts = this.posts.filter(p => p.id !== postId);
        success('Post restored');
    },

    async emptyRecycleBin() {
        if (confirm('Are you sure you want to empty the recycle bin? This action cannot be undone.')) {
            await fetch2('/api/post/recyclebin', 'DELETE');
            this.posts = [];
            success('Cleared');
        }
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