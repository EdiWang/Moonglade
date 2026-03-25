import { Alpine } from './alpine-init.mjs';
import { fetch2 } from './httpService.mjs?v=1500';
import { formatUtcTime, getLocalizedString } from './utils.module.mjs';
import { success, error } from './toastService.mjs';
import { showDeleteConfirmModal, hideConfirmModal } from './adminModal.mjs';
import { withPagination } from './admin.pagination.mjs';

Alpine.data('commentManager', () => withPagination(5, {
    comments: [],
    isLoading: true,
    selectedCommentIds: [],
    usernameFilter: '',
    emailFilter: '',
    commentContentFilter: '',
    startDate: '',
    endDate: '',
    filterCanvas: null,

    async init() {
        this.initPageFromUrl();
        this.filterCanvas = new bootstrap.Offcanvas(this.$refs.filterCanvas);
        await this.loadData();
    },

    async loadData() {
        this.isLoading = true;
        try {
            const params = new URLSearchParams({
                pageIndex: this.currentPage,
                pageSize: this.pageSize
            });

            if (this.usernameFilter) {
                params.append('username', this.usernameFilter);
            }

            if (this.emailFilter) {
                params.append('email', this.emailFilter);
            }

            if (this.commentContentFilter) {
                params.append('commentContent', this.commentContentFilter);
            }

            if (this.startDate) {
                const startUtc = new Date(this.startDate).toISOString();
                params.append('startTimeUtc', startUtc);
            }

            if (this.endDate) {
                const endUtc = new Date(this.endDate + 'T23:59:59').toISOString();
                params.append('endTimeUtc', endUtc);
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

    updateUrl() {
        const params = new URLSearchParams();
        params.set('pageIndex', this.currentPage);
        window.history.pushState({}, '', `?${params.toString()}`);
    },

    openFilter() {
        this.filterCanvas.show();
    },

    async handleFilter() {
        this.currentPage = 1;
        this.selectedCommentIds = [];
        await this.loadData();
        this.updateUrl();
        this.filterCanvas.hide();
    },

    async clearFilter() {
        this.usernameFilter = '';
        this.emailFilter = '';
        this.commentContentFilter = '';
        this.startDate = '';
        this.endDate = '';
        this.currentPage = 1;
        this.selectedCommentIds = [];
        await this.loadData();
        this.updateUrl();
        this.filterCanvas.hide();
    },

    get activeFilterCount() {
        let count = 0;
        if (this.usernameFilter) count++;
        if (this.emailFilter) count++;
        if (this.commentContentFilter) count++;
        if (this.startDate) count++;
        if (this.endDate) count++;
        return count;
    },

    get commentCount() {
        return this.totalRows;
    },

    async deleteComment(commentId) {
        showDeleteConfirmModal(getLocalizedString('confirmDelete'), async () => {
            try {
                await fetch2('/api/comment', 'DELETE', [commentId]);
                await this.loadData();
                success(getLocalizedString('commentDeleted'));
            } catch (err) {
                error(err);
            } finally {
                hideConfirmModal();
            }
        });
    },

    async deleteSelectedComments() {
        if (this.selectedCommentIds.length === 0) return;

        showDeleteConfirmModal(getLocalizedString('confirmDeleteSelected'), async () => {
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