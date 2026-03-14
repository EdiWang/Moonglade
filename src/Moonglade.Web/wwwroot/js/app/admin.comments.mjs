import { Alpine } from './alpine-init.mjs';
import { fetch2 } from './httpService.mjs?v=1500';
import { formatUtcTime, getLocalizedString } from './utils.module.mjs';
import { success, error } from './toastService.mjs';
import { showConfirmModal, hideConfirmModal } from './adminModal.mjs';
import { withPagination } from './admin.pagination.mjs';

Alpine.data('commentManager', () => withPagination(5, {
    comments: [],
    isLoading: true,
    searchTerm: '',
    selectedCommentIds: [],

    async init() {
        const urlParams = this.initPageFromUrl();
        this.searchTerm = urlParams.get('searchTerm') || '';
        await this.loadData();
    },

    async loadData() {
        this.isLoading = true;
        try {
            const params = new URLSearchParams({
                pageIndex: this.currentPage,
                pageSize: this.pageSize
            });

            if (this.searchTerm) {
                params.append('searchTerm', this.searchTerm);
            }

            const data = await fetch2(`/api/comment/list?${params.toString()}`, 'GET');
            this.comments = (data.items ?? []).map(comment => ({
                ...comment,
                showReplyForm: false,
                replyContent: ''
            }));
            this.totalRows = data.totalItemCount ?? 0;

            this.$nextTick(() => {
                formatUtcTime();
            });
        } catch (err) {
            error(err);
        } finally {
            this.isLoading = false;
        }
    },

    async handleSearch() {
        this.currentPage = 1;
        await this.loadData();
        this.updateUrl();
    },

    updateUrl() {
        const params = new URLSearchParams();
        params.set('pageIndex', this.currentPage);
        if (this.searchTerm) {
            params.set('searchTerm', this.searchTerm);
        }
        window.history.pushState({}, '', `?${params.toString()}`);
    },

    async deleteComment(commentId) {
        showConfirmModal({
            title: 'Confirm',
            body: getLocalizedString('confirmDelete'),
            confirmText: 'Delete',
            confirmClass: 'btn-outline-danger',
            confirmIcon: 'bi-trash',
            onConfirm: async () => {
                try {
                    await fetch2('/api/comment', 'DELETE', [commentId]);
                    await this.loadData();
                    success(getLocalizedString('commentDeleted'));
                } catch (err) {
                    error(err);
                } finally {
                    hideConfirmModal();
                }
            }
        });
    },

    async deleteSelectedComments() {
        if (this.selectedCommentIds.length === 0) return;

        showConfirmModal({
            title: 'Confirm',
            body: getLocalizedString('confirmDeleteSelected'),
            confirmText: 'Delete',
            confirmClass: 'btn-outline-danger',
            confirmIcon: 'bi-trash',
            onConfirm: async () => {
                try {
                    await fetch2('/api/comment', 'DELETE', this.selectedCommentIds);
                    this.selectedCommentIds = [];
                    await this.loadData();
                    success(getLocalizedString('commentsDeleted'));
                } catch (err) {
                    error(err);
                } finally {
                    hideConfirmModal();
                }
            }
        });
    },

    async toggleApproval(commentId) {
        try {
            await fetch2(`/api/comment/${commentId}/approval/toggle`, 'PUT', {});
            const comment = this.comments.find(c => c.id === commentId);
            if (comment) {
                comment.isApproved = !comment.isApproved;
            }
        } catch (err) {
            error(err);
        }
    },

    async replyComment(commentId) {
        const comment = this.comments.find(c => c.id === commentId);
        if (!comment || !comment.replyContent) return;

        try {
            const reply = await fetch2(`/api/comment/${commentId}/reply`, 'POST', comment.replyContent);

            if (reply) {
                comment.replies.push(reply);
                comment.showReplyForm = false;
                comment.replyContent = '';
                this.$nextTick(() => {
                    formatUtcTime();
                });
                success('Reply posted');
            }
        } catch (err) {
            error(err);
        }
    },

    get hasComments() {
        return this.comments.length > 0;
    }
}));