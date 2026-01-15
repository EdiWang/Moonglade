import { default as Alpine } from '/lib/alpinejs/alpinejs.3.15.0.module.esm.min.js';
import { fetch2 } from '/js/app/httpService.mjs?v=1500';
import { formatUtcTime } from './utils.module.mjs';
import { success } from '/js/app/toastService.mjs';

Alpine.data('scheduledManager', () => ({
    posts: [],
    isLoading: true,
    currentPostId: null,
    modal: null,

    async init() {
        await this.loadPosts();
        this.initModal();
    },

    initModal() {
        const modalElement = document.getElementById('publishPostModal');
        if (modalElement && typeof bootstrap !== 'undefined') {
            this.modal = new bootstrap.Modal(modalElement);
        }
    },

    async loadPosts() {
        this.isLoading = true;
        try {
            const data = await fetch2('/api/post/scheduled', 'GET');
            this.posts = data.posts ?? [];
            
            this.$nextTick(() => {
                formatUtcTime();
            });
        } finally {
            this.isLoading = false;
        }
    },

    async deletePost(postId) {
        if (confirm('Delete Confirmation?')) {
            await fetch2(`/api/post/${postId}/recycle`, 'DELETE');
            this.posts = this.posts.filter(p => p.id !== postId);
            success('Post deleted.');
        }
    },

    showPublishModal(postId) {
        this.currentPostId = postId;
        if (this.modal) {
            this.modal.show();
        }
    },

    async confirmPublish() {
        if (this.currentPostId) {
            await this.publishPost(this.currentPostId);
            if (this.modal) {
                this.modal.hide();
            }
            this.currentPostId = null;
        }
    },

    async publishPost(postId) {
        await fetch2(`/api/post/${postId}/publish`, 'PUT');
        this.posts = this.posts.filter(p => p.id !== postId);
        success('Post published');
    },

    async postponePost(postId) {
        const hours = 24;
        await fetch2(`/api/post/${postId}/postpone?hours=${hours}`, 'PUT');
        success(`Post postponed for ${hours} hour(s)`);
        
        // Reload posts after a short delay
        setTimeout(async () => {
            await this.loadPosts();
        }, 1000);
    },

    get hasPosts() {
        return this.posts.length > 0;
    },

    get sortedPosts() {
        return [...this.posts].sort((a, b) => 
            new Date(b.scheduledPublishTimeUtc) - new Date(a.scheduledPublishTimeUtc)
        );
    }
}));

Alpine.start();