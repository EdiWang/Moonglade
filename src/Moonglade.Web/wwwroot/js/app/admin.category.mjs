import { Alpine } from '/js/app/alpine-init.mjs';
import { fetch2 } from '/js/app/httpService.mjs?v=1500';
import { success } from '/js/app/toastService.mjs';
import { getLocalizedString } from './utils.module.mjs';

Alpine.data('categoryManager', () => ({
    categories: [],
    isLoading: true,
    currentCategoryId: window.emptyGuid,
    editCanvas: null,
    confirmModal: null,
    confirmMessage: '',
    pendingDeleteId: null,
    formData: {
        slug: '',
        displayName: '',
        note: ''
    },

    async init() {
        this.editCanvas = new bootstrap.Offcanvas(this.$refs.editCatCanvas);
        this.confirmModal = new bootstrap.Modal(this.$refs.confirmModal);
        await this.loadCategories();
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

    deleteCategory(id) {
        this.pendingDeleteId = id;
        this.confirmMessage = getLocalizedString('confirmDelete');
        this.confirmModal.show();
    },

    async confirmAction() {
        if (this.pendingDeleteId) {
            await fetch2(`/api/category/${this.pendingDeleteId}`, 'DELETE');
            await this.loadCategories();
            success(getLocalizedString('categoryDeleted'));
            this.confirmModal.hide();
            this.pendingDeleteId = null;
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
        success(isCreate ? getLocalizedString('categoryCreated') : getLocalizedString('categoryUpdated'));
    }
}));

Alpine.start();