import { Alpine } from './alpine-init.mjs';
import { fetch2 } from './httpService.mjs?v=1500';
import { formatUtcTime, getLocalizedString } from './utils.module.mjs';
import { success } from './toastService.mjs';
import { showDeleteConfirmModal, hideConfirmModal, escapeHtml } from './adminModal.mjs';

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

            this.$nextTick(() => formatUtcTime());
        } finally {
            this.isLoading = false;
        }
    },

    openDeleteModal(postId, postTitle) {
        showDeleteConfirmModal(`<p>Are you sure you want to delete this draft?</p><p class="text-muted">${escapeHtml(postTitle)}</p>`, async () => {
            await fetch2(`/api/post/${postId}/recycle`, 'DELETE');
            this.posts = this.posts.filter(p => p.id !== postId);
            success(getLocalizedString('postDeleted'));
            hideConfirmModal();
        });
    },

    get hasPosts() {
        return this.posts.length > 0;
    }
}));