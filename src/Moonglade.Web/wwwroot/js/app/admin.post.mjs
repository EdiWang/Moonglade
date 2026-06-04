import { Alpine } from './alpine-init.mjs';
import { fetch2 } from './httpService.mjs?v=1500';
import { formatUtcTime, getLocalizedString } from './utils.module.mjs';
import { success, error } from './toastService.mjs';
import { showDeleteConfirmModal, hideConfirmModal } from './adminModal.mjs';
import { withPagination } from './admin.pagination.mjs';

Alpine.data('postManager', () => withPagination(4, {
    posts: [],
    isLoading: true,
    titleFilter: '',
    abstractFilter: '',
    tagFilter: '',
    sortBy: 'pubDateUtc',
    sortDescending: 'true',
    filterCanvas: null,
    sortByOptions: [
        { value: 'pubDateUtc', label: 'Publish Time' }
    ],
    popStateHandler: null,

    async init() {
        this.initPostStateFromUrl();
        this.popStateHandler = async () => {
            this.initPostStateFromUrl();
            await this.loadData();
        };
        window.addEventListener('popstate', this.popStateHandler);
        await this.loadData();
    },

    destroy() {
        if (this.popStateHandler) {
            window.removeEventListener('popstate', this.popStateHandler);
        }
    },

    initPostStateFromUrl() {
        const urlParams = this.initPageFromUrl();
        this.titleFilter = urlParams.get('title') || '';
        this.abstractFilter = urlParams.get('contentAbstract') || '';
        this.tagFilter = urlParams.get('tag') || '';

        const sortDescending = urlParams.get('sortDescending');
        if (sortDescending === 'true' || sortDescending === 'false') {
            this.sortDescending = sortDescending;
        }
    },

    async loadData() {
        this.isLoading = true;
        try {
            const params = new URLSearchParams({
                pageIndex: this.currentPage,
                pageSize: this.pageSize
            });
            
            if (this.titleFilter) {
                params.append('title', this.titleFilter);
            }

            if (this.abstractFilter) {
                params.append('contentAbstract', this.abstractFilter);
            }

            if (this.tagFilter) {
                params.append('tag', this.tagFilter);
            }

            params.append('sortDescending', this.sortDescending === 'true');

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

    openFilter() {
        this.filterCanvas ??= new bootstrap.Offcanvas(this.$refs.filterCanvas);
        this.filterCanvas.show();
    },

    async handleFilter() {
        this.currentPage = 1;
        await this.loadData();
        this.updateUrl();
        this.filterCanvas?.hide();
    },

    async clearFilter() {
        this.titleFilter = '';
        this.abstractFilter = '';
        this.tagFilter = '';
        this.sortBy = 'pubDateUtc';
        this.sortDescending = 'true';
        this.currentPage = 1;
        await this.loadData();
        this.updateUrl();
        this.filterCanvas?.hide();
    },

    updateUrl() {
        const params = new URLSearchParams();
        params.set('pageIndex', this.currentPage);
        params.set('pageSize', this.pageSize);

        if (this.titleFilter) {
            params.set('title', this.titleFilter);
        }

        if (this.abstractFilter) {
            params.set('contentAbstract', this.abstractFilter);
        }

        if (this.tagFilter) {
            params.set('tag', this.tagFilter);
        }

        params.set('sortDescending', this.sortDescending);
        window.history.pushState({}, '', `?${params.toString()}`);
    },

    deletePost(postId) {
        showDeleteConfirmModal(getLocalizedString('confirmDelete'), async () => {
            try {
                await fetch2(`/api/post/${postId}/recycle`, 'DELETE');
                await this.loadData();
                success(getLocalizedString('postDeleted'));
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

    get postCount() {
        return this.totalRows;
    },

    get activeFilterCount() {
        let count = 0;
        if (this.titleFilter) count++;
        if (this.abstractFilter) count++;
        if (this.tagFilter) count++;
        if (this.sortBy !== 'pubDateUtc' || this.sortDescending !== 'true') count++;
        return count;
    },

    getPostUrl(post) {
        const date = new Date(post.pubDateUtc);
        return `/Post/${date.getFullYear()}/${date.getMonth() + 1}/${date.getDate()}/${post.slug}`;
    }
}, [4, 10, 20, 40]));
