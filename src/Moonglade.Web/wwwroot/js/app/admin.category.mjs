import { Alpine } from './alpine-init.mjs';
import { fetch2 } from './httpService.mjs?v=1500';
import { success, error } from './toastService.mjs';
import { getLocalizedString } from './utils.module.mjs';
import { showConfirmModal, hideConfirmModal } from './adminModal.mjs';

Alpine.data('categoryManager', () => ({
    categories: [],
    isLoading: true,
    currentCategoryId: window.emptyGuid,
    editCanvas: null,
    pendingDeleteId: null,
    formData: {
        slug: '',
        displayName: '',
        note: ''
    },

    async init() {
        this.editCanvas = new bootstrap.Offcanvas(this.$refs.editCatCanvas);
        await this.loadCategories();
    },

    async loadCategories() {
        this.isLoading = true;
        try {
            this.categories = (await fetch2('/api/category/list', 'GET')) ?? [];
        } catch (err) {
            error(err);
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
        try {
            const data = await fetch2(`/api/category/${id}`, 'GET');
            this.currentCategoryId = data.id;
            this.formData = {
                slug: data.slug,
                displayName: data.displayName,
                note: data.note
            };
            this.editCanvas.show();
        } catch (err) {
            error(err);
        }
    },

    deleteCategory(id) {
        this.pendingDeleteId = id;
        showConfirmModal({
            title: getLocalizedString('confirmDelete'),
            body: getLocalizedString('confirmDelete'),
            confirmText: 'Delete',
            confirmClass: 'btn-outline-danger',
            confirmIcon: 'bi-trash',
            onConfirm: async () => {
                try {
                    await fetch2(`/api/category/${this.pendingDeleteId}`, 'DELETE');
                    await this.loadCategories();
                    success(getLocalizedString('categoryDeleted'));
                    this.pendingDeleteId = null;
                } catch (err) {
                    error(err);
                } finally {
                    hideConfirmModal();
                }
            }
        });
    },

    async handleSubmit() {
        const isCreate = this.currentCategoryId === window.emptyGuid;
        const apiAddress = isCreate ? '/api/category' : `/api/category/${this.currentCategoryId}`;
        const verb = isCreate ? 'POST' : 'PUT';

        try {
            await fetch2(apiAddress, verb, this.formData);

            this.formData = { slug: '', displayName: '', note: '' };
            this.editCanvas.hide();
            await this.loadCategories();
            success(isCreate ? getLocalizedString('categoryCreated') : getLocalizedString('categoryUpdated'));
        } catch (err) {
            error(err);
        }
    }
}));