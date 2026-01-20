import { default as Alpine } from '/lib/alpinejs/alpinejs.3.15.0.module.esm.min.js';
import { fetch2 } from '/js/app/httpService.mjs?v=1500';
import { formatUtcTime } from './utils.module.mjs';
import { success } from '/js/app/toastService.mjs';

Alpine.data('postManager', () => ({
posts: [],
isLoading: true,
searchTerm: '',
currentPage: 1,
pageSize: 4,
totalRows: 0,
confirmModal: null,
confirmMessage: '',
pendingDeleteId: null,

async init() {
    const urlParams = new URLSearchParams(window.location.search);
    this.currentPage = parseInt(urlParams.get('pageIndex')) || 1;
    this.searchTerm = urlParams.get('searchTerm') || '';
    this.confirmModal = new bootstrap.Modal(this.$refs.confirmModal);
    await this.loadPosts();
},

    async loadPosts() {
        this.isLoading = true;
        try {
            const params = new URLSearchParams({
                pageIndex: this.currentPage,
                pageSize: this.pageSize
            });
            
            if (this.searchTerm) {
                params.append('searchTerm', this.searchTerm);
            }

            const data = await fetch2(`/api/post/list?${params.toString()}`, 'GET');
            this.posts = data.posts ?? [];
            this.totalRows = data.totalRows ?? 0;
            
            formatUtcTime();
        } finally {
            this.isLoading = false;
        }
    },

    async handleSearch() {
        this.currentPage = 1;
        await this.loadPosts();
        this.updateUrl();
    },

    async goToPage(page) {
        this.currentPage = page;
        await this.loadPosts();
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

    async deletePost(postId) {
        this.pendingDeleteId = postId;
        this.confirmMessage = 'Are you sure you want to delete this post?';
        this.confirmModal.show();
    },

    async confirmAction() {
        if (this.pendingDeleteId) {
            await fetch2(`/api/post/${this.pendingDeleteId}/recycle`, 'DELETE');
            await this.loadPosts();
            success('Post deleted');
            this.confirmModal.hide();
            this.pendingDeleteId = null;
        }
    },

    get hasPosts() {
        return this.posts.length > 0;
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
    },

    getPostUrl(post) {
        const date = new Date(post.pubDateUtc);
        return `/Post/${date.getFullYear()}/${date.getMonth() + 1}/${date.getDate()}/${post.slug}`;
    }
}));

Alpine.start();
