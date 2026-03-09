export function createPaginationMixin(pageSize) {
    return {
        currentPage: 1,
        pageSize,
        totalRows: 0,

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

        initPageFromUrl() {
            const urlParams = new URLSearchParams(window.location.search);
            this.currentPage = parseInt(urlParams.get('pageIndex')) || 1;
            return urlParams;
        },

        async goToPage(page) {
            if (page < 1 || page > this.totalPages) return;
            this.currentPage = page;
            await this.loadData();
            this.updateUrl();
            window.scrollTo(0, 0);
        },

        updateUrl() {
            const params = new URLSearchParams();
            params.set('pageIndex', this.currentPage);
            window.history.pushState({}, '', `?${params.toString()}`);
        }
    };
}

export function withPagination(pageSize, componentObj) {
    const paginationDescriptors = Object.getOwnPropertyDescriptors(createPaginationMixin(pageSize));
    const componentDescriptors = Object.getOwnPropertyDescriptors(componentObj);
    return Object.defineProperties({}, { ...paginationDescriptors, ...componentDescriptors });
}
