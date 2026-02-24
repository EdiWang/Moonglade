import { Alpine } from '/js/app/alpine-init.mjs';
import { fetch2 } from '/js/app/httpService.mjs?v=1500';
import { success, error } from '/js/app/toastService.mjs';
import { getLocalizedString } from './utils.module.mjs';
import { showConfirmModal, hideConfirmModal } from './adminModal.mjs';
import { renderWidgetContent } from './admin.widgets.render.mjs';
import { createLinkMixin } from './admin.widgets.links.mjs';
import { createButtonMixin } from './admin.widgets.buttons.mjs';

const emptyImageLink = () => ({ imageUrl: '', cssClass: '', title: '', altText: '', linkUrl: '', openInNewTab: true });
const emptyFormData = () => ({
    title: '',
    widgetType: 'LinkList',
    displayOrder: 0,
    isEnabled: true,
    links: [],
    imageLink: emptyImageLink(),
    buttons: []
});

Alpine.data('widgetManager', () => ({
    widgets: [],
    isLoading: true,
    currentWidgetId: window.emptyGuid,
    editCanvas: null,
    linkModal: null,
    buttonModal: null,
    deleteTarget: { widgetId: null, linkIndex: -1 },
    formData: emptyFormData(),
    ...createLinkMixin(),
    ...createButtonMixin(),

    async init() {
        this.editCanvas = new bootstrap.Offcanvas(this.$refs.editWidgetCanvas);
        this.linkModal = new bootstrap.Modal(this.$refs.linkDialogModal);
        this.buttonModal = new bootstrap.Modal(this.$refs.buttonDialogModal);
        await this.loadWidgets();
    },

    async loadWidgets() {
        this.isLoading = true;
        try {
            this.widgets = (await fetch2('/api/widgets/list', 'GET')) ?? [];
        } catch (err) {
            error(err);
        } finally {
            this.isLoading = false;
        }
    },

    get sortedWidgets() {
        return [...this.widgets].sort((a, b) => a.displayOrder - b.displayOrder);
    },

    get hasWidgets() {
        return this.widgets.length > 0;
    },

    get sortedLinks() {
        return [...this.formData.links].sort((a, b) => a.order - b.order);
    },

    get hasLinks() {
        return this.formData.links.length > 0;
    },

    renderWidgetContent,

    initCreateWidget() {
        this.currentWidgetId = window.emptyGuid;
        this.formData = emptyFormData();
        this.editCanvas.show();
    },

    async editWidget(id) {
        try {
            const widget = await fetch2(`/api/widgets/${id}`, 'GET');
            this.currentWidgetId = widget.id;

            let links = [];
            let imageLink = emptyImageLink();
            let buttons = [];

            if (widget.widgetType === 'LinkList' && widget.contentCode) {
                try { links = JSON.parse(widget.contentCode); } catch (e) { links = []; }
            } else if (widget.widgetType === 'ImageLink' && widget.contentCode) {
                try { imageLink = JSON.parse(widget.contentCode); } catch (e) { imageLink = emptyImageLink(); }
            } else if (widget.widgetType === 'ButtonLink' && widget.contentCode) {
                try { buttons = JSON.parse(widget.contentCode); } catch (e) { buttons = []; }
            }

            this.formData = {
                title: widget.title,
                widgetType: widget.widgetType,
                displayOrder: widget.displayOrder,
                isEnabled: widget.isEnabled,
                links,
                imageLink,
                buttons
            };

            this.editCanvas.show();
        } catch (err) {
            error(err);
        }
    },

    deleteWidget(id) {
        showConfirmModal({
            title: 'Delete Widget',
            body: getLocalizedString('deleteWidget'),
            confirmText: 'Delete',
            confirmClass: 'btn-outline-danger',
            confirmIcon: 'bi-trash',
            onConfirm: async () => {
                try {
                    await fetch2(`/api/widgets/${id}`, 'DELETE');
                    hideConfirmModal();
                    await this.loadWidgets();
                    success(getLocalizedString('widgetDeleted'));
                } catch (err) {
                    console.error(err);
                }
            }
        });
    },

    async handleSubmit() {
        const isCreate = this.currentWidgetId === window.emptyGuid;
        const apiAddress = isCreate ? '/api/widgets' : `/api/widgets/${this.currentWidgetId}`;
        const verb = isCreate ? 'POST' : 'PUT';

        if (this.formData.displayOrder < -30 || this.formData.displayOrder > 999) {
            alert('Display Order must be between -30 and 999.');
            return;
        }

        let contentCode = '';
        if (this.formData.widgetType === 'LinkList') {
            contentCode = JSON.stringify(this.formData.links);
        } else if (this.formData.widgetType === 'ImageLink') {
            if (!this.formData.imageLink.imageUrl) {
                alert(getLocalizedString('imageUrlRequired'));
                return;
            }
            contentCode = JSON.stringify(this.formData.imageLink);
        } else if (this.formData.widgetType === 'ButtonLink') {
            if (this.formData.buttons.length === 0) {
                alert(getLocalizedString('textUrlRequired'));
                return;
            }
            contentCode = JSON.stringify(this.formData.buttons);
        }

        try {
            await fetch2(apiAddress, verb, {
                title: this.formData.title,
                widgetType: this.formData.widgetType,
                displayOrder: this.formData.displayOrder,
                isEnabled: this.formData.isEnabled,
                contentCode
            });
            this.editCanvas.hide();
            await this.loadWidgets();
            success(isCreate ? getLocalizedString('widgetCreated') : getLocalizedString('widgetUpdated'));
        } catch (err) {
            error(err);
        }
    },

    onWidgetTypeChange() {
        if (this.formData.widgetType !== 'LinkList') this.formData.links = [];
        if (this.formData.widgetType !== 'ImageLink') this.formData.imageLink = emptyImageLink();
        if (this.formData.widgetType !== 'ButtonLink') this.formData.buttons = [];
    },

    getLocalizedString(key) {
        return getLocalizedString(key);
    }
}));

Alpine.start();