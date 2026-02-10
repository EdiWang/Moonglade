import { Alpine } from '/js/app/alpine-init.mjs';
import { fetch2 } from '/js/app/httpService.mjs?v=1500';
import { formatUtcTime, getLocalizedString } from './utils.module.mjs';
import { success, error } from '/js/app/toastService.mjs';

Alpine.data('scheduledManager', () => ({
    posts: [],
    isLoading: true,
    currentPostId: null,
    modal: null,
    deleteTargetId: null,
    deleteMessage: '',
    deleteModal: null,
    cancelScheduleModal: null,
    cancelScheduleTargetId: null,

    async init() {
        await this.loadPosts();
        this.initModal();
        this.initDeleteModal();
        this.initCancelScheduleModal();
    },

    initModal() {
        const modalElement = document.getElementById('publishPostModal');
        if (modalElement && typeof bootstrap !== 'undefined') {
            this.modal = new bootstrap.Modal(modalElement);
        }
    },

    initDeleteModal() {
        const modalElement = document.getElementById('deletePostModal');
        if (modalElement && typeof bootstrap !== 'undefined') {
            this.deleteModal = new bootstrap.Modal(modalElement);
        }
    },

    initCancelScheduleModal() {
        const modalElement = document.getElementById('cancelScheduleModal');
        if (modalElement && typeof bootstrap !== 'undefined') {
            this.cancelScheduleModal = new bootstrap.Modal(modalElement);
        }
    },

    async loadPosts() {
        this.isLoading = true;
        try {
            const data = await fetch2('/api/schedule/list', 'GET');
            this.posts = data.posts ?? [];

            await this.$nextTick();
            setTimeout(() => formatUtcTime(), 50);
        } catch (err) {
            error(err);
        } finally {
            this.isLoading = false;
        }
    },

    async deletePost(postId) {
        this.deleteTargetId = postId;
        this.deleteMessage = getLocalizedString('deleteConfirmation');
        this.deleteModal?.show();
    },

    async confirmDelete() {
        if (!this.deleteTargetId) return;
        try {
            await fetch2(`/api/post/${this.deleteTargetId}/recycle`, 'DELETE');
            this.posts = this.posts.filter(p => p.id !== this.deleteTargetId);
            success(getLocalizedString('postDeleted'));
        } catch (err) {
            error(err);
        } finally {
            this.deleteModal?.hide();
            this.deleteTargetId = null;
        }
    },

    showPublishModal(postId) {
        this.currentPostId = postId;
        this.modal?.show();
    },

    async confirmPublish() {
        if (!this.currentPostId) return;
        await this.publishPost(this.currentPostId);
        this.modal?.hide();
        this.currentPostId = null;
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
            await fetch2(`/api/schedule/${postId}/postpone?hours=${hours}`, 'PUT');
            const template = getLocalizedString('postPostponed');
            success(template.replace('{0}', hours));
            setTimeout(async () => await this.loadPosts(), 500);
        } catch (err) {
            error(err);
        }
    },

    showCancelScheduleModal(postId) {
        this.cancelScheduleTargetId = postId;
        this.cancelScheduleModal?.show();
    },

    async confirmCancelSchedule() {
        if (!this.cancelScheduleTargetId) return;
        try {
            await fetch2(`/api/schedule/${this.cancelScheduleTargetId}/cancel`, 'PUT');
            this.posts = this.posts.filter(p => p.id !== this.cancelScheduleTargetId);
            success(getLocalizedString('scheduleCancelled'));
        } catch (err) {
            error(err);
        } finally {
            this.cancelScheduleModal?.hide();
            this.cancelScheduleTargetId = null;
        }
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