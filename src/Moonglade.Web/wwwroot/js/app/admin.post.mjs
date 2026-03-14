import { Alpine } from './alpine-init.mjs';
import { fetch2 } from './httpService.mjs?v=1500';
import { formatUtcTime, getLocalizedString } from './utils.module.mjs';
import { success, error } from './toastService.mjs';
import { showConfirmModal, hideConfirmModal } from './adminModal.mjs';
import { withPagination } from './admin.pagination.mjs';

Alpine.data('postManager', () => withPagination(4, {
    posts: [],
    isLoading: true,
    searchTerm: '',

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

            const data = await fetch2(`/api/post/list?${params.toString()}`, 'GET');
            this.posts = data.items ?? [];
            this.totalRows = data.totalItemCount ?? 0;

            this.$nextTick(() => formatUtcTime());
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

    deletePost(postId) {
        showConfirmModal({
            title: 'Confirm',
            body: getLocalizedString('confirmDelete'),
            confirmText: 'Delete',
            confirmClass: 'btn-outline-danger',
            confirmIcon: 'bi-trash',
            onConfirm: async () => {
                try {
                    await fetch2(`/api/post/${postId}/recycle`, 'DELETE');
                    await this.loadData();
                    success(getLocalizedString('postDeleted'));
                } catch (err) {
                    error(err);
                } finally {
                    hideConfirmModal();
                }
            }
        });
    },

    get hasPosts() {
        return this.posts.length > 0;
    },

    getPostUrl(post) {
        const date = new Date(post.pubDateUtc);
        return `/Post/${date.getFullYear()}/${date.getMonth() + 1}/${date.getDate()}/${post.slug}`;
    }
}));
