import { default as Alpine } from '/lib/alpinejs/alpinejs.3.15.0.module.esm.min.js';
import { fetch2 } from '/js/app/httpService.mjs?v=1500';
import { success } from '/js/app/toastService.mjs';

Alpine.data('categoryManager', () => ({
    categories: [],
    isLoading: true,
    currentCategoryId: window.emptyGuid,
    editCanvas: null,
    formData: {
        slug: '',
        displayName: '',
        note: ''
    },

    async init() {
        this.editCanvas = new bootstrap.Offcanvas(this.$refs.editCatCanvas);
        await this.loadCategories();
        console.log(this.isLoading);
    },

    async loadCategories() {
        this.isLoading = true;
        try {
            this.categories = (await fetch2('/api/category/list', 'GET')) ?? [];
        } finally {
            this.isLoading = false;
        }
    },

    get sortedCategories() {
        return [...this.categories].sort((a, b) =>
            a.displayName.localeCompare(b.displayName)
        );
    },

    get hasCategories() {
        return this.categories.length > 0;
    },

    initCreateCategory() {
        this.currentCategoryId = window.emptyGuid;
        this.formData = { slug: '', displayName: '', note: '' };
        this.editCanvas.show();
    },

    async editCategory(id) {
        const data = await fetch2(`/api/category/${id}`, 'GET');
        this.currentCategoryId = data.id;
        this.formData = {
            slug: data.slug,
            displayName: data.displayName,
            note: data.note
        };
        this.editCanvas.show();
    },

    async deleteCategory(id) {
        if (confirm('Delete?')) {
            await fetch2(`/api/category/${id}`, 'DELETE');
            await this.loadCategories();
            success('Category deleted');
        }
    },

    async handleSubmit() {
        const isCreate = this.currentCategoryId === window.emptyGuid;
        const apiAddress = isCreate ? '/api/category' : `/api/category/${this.currentCategoryId}`;
        const verb = isCreate ? 'POST' : 'PUT';

        await fetch2(apiAddress, verb, this.formData);

        this.formData = { slug: '', displayName: '', note: '' };
        this.editCanvas.hide();
        await this.loadCategories();
        success(isCreate ? 'Category created' : 'Category updated');
    }
}));

Alpine.start();