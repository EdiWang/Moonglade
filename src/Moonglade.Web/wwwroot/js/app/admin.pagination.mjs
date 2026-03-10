export function createPaginationMixin(pageSize, pageSizeOptions) {
    return {
        currentPage: 1,
        pageSize,
        pageSizeOptions: pageSizeOptions || [],
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
            const urlPageSize = parseInt(urlParams.get('pageSize'));
            if (urlPageSize && this.pageSizeOptions.includes(urlPageSize)) {
                this.pageSize = urlPageSize;
            }
            return urlParams;
        },

        async goToPage(page) {
            if (page < 1 || page > this.totalPages) return;
            this.currentPage = page;
            await this.loadData();
            this.updateUrl();
            window.scrollTo(0, 0);
        },

        async changePageSize(newSize) {
            this.pageSize = parseInt(newSize);
            this.currentPage = 1;
            await this.loadData();
            this.updateUrl();
        },

        updateUrl() {
            const params = new URLSearchParams();
            params.set('pageIndex', this.currentPage);
            if (this.pageSizeOptions.length > 0) {
                params.set('pageSize', this.pageSize);
            }
            window.history.pushState({}, '', `?${params.toString()}`);
        }
    };
}

export function withPagination(pageSize, componentObj, pageSizeOptions) {
    const paginationDescriptors = Object.getOwnPropertyDescriptors(createPaginationMixin(pageSize, pageSizeOptions));
    const componentDescriptors = Object.getOwnPropertyDescriptors(componentObj);
    return Object.defineProperties({}, { ...paginationDescriptors, ...componentDescriptors });
}
