import { default as Alpine } from '/lib/alpinejs/alpinejs.3.15.0.module.esm.min.js';
import { fetch2 } from '/js/app/httpService.mjs?v=1500';
import { formatUtcTime, getLocalizedString } from '/js/app/utils.module.mjs';
import { success } from '/js/app/toastService.mjs';

Alpine.data('commentManager', () => ({
    comments: [],
    isLoading: true,
    searchTerm: '',
    currentPage: 1,
    pageSize: 5,
    totalRows: 0,
    selectedCommentIds: [],
    confirmModal: null,
    confirmMessage: '',
    pendingDeleteId: null,
    isDeleteSelected: false,

    async init() {
        const urlParams = new URLSearchParams(window.location.search);
        this.currentPage = parseInt(urlParams.get('pageIndex')) || 1;
        this.searchTerm = urlParams.get('searchTerm') || '';
        this.confirmModal = new bootstrap.Modal(this.$refs.confirmModal);
        await this.loadComments();
    },

    async loadComments() {
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
            this.comments = (data.comments ?? []).map(comment => ({
                ...comment,
                showReplyForm: false,
                replyContent: ''
            }));
            this.totalRows = data.totalRows ?? 0;
            
            formatUtcTime();
        } finally {
            this.isLoading = false;
        }
    },

    async handleSearch() {
        this.currentPage = 1;
        await this.loadComments();
        this.updateUrl();
    },

    async goToPage(page) {
        if (page < 1 || page > this.totalPages) return;
        this.currentPage = page;
        await this.loadComments();
        this.updateUrl();
        window.scrollTo(0, 0);
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
        this.pendingDeleteId = commentId;
        this.isDeleteSelected = false;
        this.confirmMessage = getLocalizedString('confirmDelete');
        this.confirmModal.show();
    },

    async deleteSelectedComments() {
        if (this.selectedCommentIds.length === 0) return;
        
        this.isDeleteSelected = true;
        this.confirmMessage = getLocalizedString('confirmDeleteSelected');
        this.confirmModal.show();
    },

    async confirmAction() {
        if (this.isDeleteSelected) {
            await fetch2('/api/comment', 'DELETE', this.selectedCommentIds);
            this.selectedCommentIds = [];
            await this.loadComments();
            success(getLocalizedString('commentsDeleted'));
        } else if (this.pendingDeleteId) {
            await fetch2('/api/comment', 'DELETE', [this.pendingDeleteId]);
            await this.loadComments();
            success(getLocalizedString('commentDeleted'));
            this.pendingDeleteId = null;
        }
        this.confirmModal.hide();
    },

    async toggleApproval(commentId) {
        await fetch2(`/api/comment/${commentId}/approval/toggle`, 'PUT', {});
        const comment = this.comments.find(c => c.id === commentId);
        if (comment) {
            comment.isApproved = !comment.isApproved;
        }
    },

    async replyComment(commentId) {
        const comment = this.comments.find(c => c.id === commentId);
        if (!comment || !comment.replyContent) return;

        const reply = await fetch2(`/api/comment/${commentId}/reply`, 'POST', comment.replyContent);
        
        if (reply) {
            comment.replies.push(reply);
            comment.showReplyForm = false;
            comment.replyContent = '';
            formatUtcTime();
            success('Reply posted');
        }
    },

    get hasComments() {
        return this.comments.length > 0;
    },

    get totalPages() {
        return Math.ceil(this.totalRows / this.pageSize);
    },

    get paginationPages() {
        const pages = [];
        const maxVisible = 5;
        let startPage = Math.max(1, this.currentPage - Math.floor(maxVisible / 2));
        let endPage = Math.min(this.totalPages, startPage + maxVisible - 1);

        if (endPage - startPage < maxVisible - 1) {
            startPage = Math.max(1, endPage - maxVisible + 1);
        }

        for (let i = startPage; i <= endPage; i++) {
            pages.push(i);
        }
        return pages;
    }
}));

Alpine.start();