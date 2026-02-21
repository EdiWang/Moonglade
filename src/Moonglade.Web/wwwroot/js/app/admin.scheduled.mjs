import { Alpine } from '/js/app/alpine-init.mjs';
import { fetch2 } from '/js/app/httpService.mjs?v=1500';
import { formatUtcTime, getLocalizedString } from './utils.module.mjs';
import { success, error } from '/js/app/toastService.mjs';
import { showConfirmModal, hideConfirmModal } from './adminModal.mjs';

Alpine.data('scheduledManager', () => ({
    posts: [],
    isLoading: true,

    async init() {
        await this.loadPosts();
    },

    async loadPosts() {
        this.isLoading = true;
        try {
            const data = await fetch2('/api/schedule/list', 'GET');
            this.posts = data ?? [];

            await this.$nextTick();
            setTimeout(() => formatUtcTime(), 50);
        } catch (err) {
            error(err);
        } finally {
            this.isLoading = false;
        }
    },

    async deletePost(postId) {
        showConfirmModal({
            title: 'Delete Confirmation',
            body: getLocalizedString('deleteConfirmation'),
            confirmText: 'Delete',
            confirmClass: 'btn-outline-danger',
            confirmIcon: 'bi-trash',
            onConfirm: async () => {
                try {
                    await fetch2(`/api/post/${postId}/recycle`, 'DELETE');
                    this.posts = this.posts.filter(p => p.id !== postId);
                    success(getLocalizedString('postDeleted'));
                } catch (err) {
                    error(err);
                } finally {
                    hideConfirmModal();
                }
            }
        });
    },

    showPublishModal(postId) {
        showConfirmModal({
            title: 'Publish Post',
            body: 'Are you sure you want to publish this post now?',
            confirmText: 'Publish',
            confirmClass: 'btn-accent',
            onConfirm: async () => {
                await this.publishPost(postId);
                hideConfirmModal();
            }
        });
    },

    async publishPost(postId) {
        try {
            await fetch2(`/api/post/${postId}/publish`, 'PUT');
            this.posts = this.posts.filter(p => p.id !== postId);
            success(getLocalizedString('postPublished'));
        } catch (err) {
            error(err);
        }
    },

    async postponePost(postId) {
        try {
            const hours = 24;
            await fetch2(`/api/schedule/postpone/${postId}?hours=${hours}`, 'PUT');
            const template = getLocalizedString('postPostponed');
            success(template.replace('{0}', hours));
            setTimeout(async () => await this.loadPosts(), 500);
        } catch (err) {
            error(err);
        }
    },

    showCancelScheduleModal(postId) {
        showConfirmModal({
            title: 'Cancel Schedule',
            body: 'Are you sure you want to cancel the scheduled publish and move this post to drafts?',
            confirmText: 'Yes, Cancel Schedule',
            confirmClass: 'btn-outline-accent',
            confirmIcon: 'bi-x-circle',
            onConfirm: async () => {
                try {
                    await fetch2(`/api/schedule/cancel/${postId}`, 'PUT');
                    this.posts = this.posts.filter(p => p.id !== postId);
                    success(getLocalizedString('scheduleCancelled'));
                } catch (err) {
                    error(err);
                } finally {
                    hideConfirmModal();
                }
            }
        });
    },

    parseUtcSafe(dateString) {
        if (!dateString) return null;
        const candidates = [
            dateString,
            `${dateString}Z`,
            dateString.replace(' ', 'T'),
            `${dateString.replace(' ', 'T')}Z`,
            dateString.replace(/-/g, '/')
        ];
        for (const c of candidates) {
            const d = new Date(c);
            if (!isNaN(d.getTime())) return d;
        }
        return null;
    },

    formatScheduledTime(dateString) {
        const d = this.parseUtcSafe(dateString);
        return d ? d.toLocaleString() : (dateString ?? '');
    },

    get hasPosts() {
        return this.posts.length > 0;
    },

    get sortedPosts() {
        return [...this.posts].sort((a, b) => {
            const da = this.parseUtcSafe(a.scheduledPublishTimeUtc) ?? new Date(0);
            const db = this.parseUtcSafe(b.scheduledPublishTimeUtc) ?? new Date(0);
            return db - da;
        });
    }
}));

Alpine.start();