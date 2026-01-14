import { default as Alpine } from '/lib/alpinejs/alpinejs.3.15.0.module.esm.min.js';
import { fetch2 } from '/js/app/httpService.mjs?v=1500';
import { formatUtcTime } from './utils.module.mjs';
import { success } from '/js/app/toastService.mjs';

Alpine.data('draftManager', () => ({
    posts: [],
    isLoading: true,

    async init() {
        await this.loadPosts();
    },

    async loadPosts() {
        this.isLoading = true;
        try {
            const data = await fetch2('/api/post/drafts', 'GET');
            this.posts = (data.posts ?? []).sort((a, b) => 
                new Date(b.lastModifiedUtc) - new Date(a.lastModifiedUtc)
            );
            
            formatUtcTime();
        } finally {
            this.isLoading = false;
        }
    },

    async deletePost(postId) {
        if (confirm('Delete Confirmation?')) {
            await fetch2(`/api/post/${postId}/recycle`, 'DELETE');
            this.posts = this.posts.filter(p => p.id !== postId);
            success('Post deleted');
        }
    },

    get hasPosts() {
        return this.posts.length > 0;
    }
}));

Alpine.start();