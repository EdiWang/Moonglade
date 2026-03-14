import { Alpine } from './alpine-init.mjs';
import { fetch2 } from './httpService.mjs?v=1500';
import { formatUtcTime, getLocalizedString } from './utils.module.mjs';
import { success, error } from './toastService.mjs';
import { showDeleteConfirmModal, hideConfirmModal, escapeHtml } from './adminModal.mjs';

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
            this.posts = data.posts ?? [];
            
            formatUtcTime();
        } catch (err) {
            error(err);
        } finally {
            this.isLoading = false;
        }
    },

    confirmDelete(postId, postTitle) {
        const template = getLocalizedString('confirmDelete');
        const message = template.replace('{0}', escapeHtml(postTitle));
        showDeleteConfirmModal(`<p>${message}</p>`, async () => {
            try {
                await fetch2(`/api/post/${postId}/destroy`, 'DELETE');
                this.posts = this.posts.filter(p => p.id !== postId);
                success(getLocalizedString('postDeleted'));
            } catch (err) {
                error(err);
            } finally {
                hideConfirmModal();
            }
        });
    },

    async restorePost(postId) {
        try {
            await fetch2(`/api/post/${postId}/restore`, 'POST');
            this.posts = this.posts.filter(p => p.id !== postId);
            success(getLocalizedString('postRestored'));
        } catch (err) {
            error(err);
        }
    },

    confirmEmptyRecycleBin() {
        showDeleteConfirmModal('<p>Are you sure you want to empty the recycle bin? This action cannot be undone.</p>', async () => {
            try {
                await fetch2('/api/post/recyclebin', 'DELETE');
                this.posts = [];
                success(getLocalizedString('cleared'));
            } catch (err) {
                error(err);
            } finally {
                hideConfirmModal();
            }
        });
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