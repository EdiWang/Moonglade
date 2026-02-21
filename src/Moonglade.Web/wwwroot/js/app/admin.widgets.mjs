import { Alpine } from '/js/app/alpine-init.mjs';
import { fetch2 } from '/js/app/httpService.mjs?v=1500';
import { success, error } from '/js/app/toastService.mjs';
import { getLocalizedString } from './utils.module.mjs';
import { showConfirmModal, hideConfirmModal } from './adminModal.mjs';

Alpine.data('widgetManager', () => ({
widgets: [],
isLoading: true,
currentWidgetId: window.emptyGuid,
editCanvas: null,
linkModal: null,
buttonModal: null,
deleteTarget: {
    widgetId: null,
    linkIndex: -1
},
formData: {
        title: '',
        widgetType: 'LinkList',
        displayOrder: 0,
        isEnabled: true,
        links: [],
        imageLink: {
            imageUrl: '',
            cssClass: '',
            title: '',
            altText: '',
            linkUrl: '',
            openInNewTab: true
        },
        buttons: []
    },
    linkDialog: {
        isNew: true,
        index: -1,
        data: {
            name: '',
            url: '',
            icon: '',
            order: 1,
            openInNewTab: true
        }
    },
    buttonDialog: {
        isNew: true,
        index: -1,
        data: {
            text: '',
            url: '',
            cssClass: 'btn-outline-primary',
            openInNewTab: true
        }
    },

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

    renderWidgetContent(widget) {
        if (widget.widgetType === 'LinkList' && widget.contentCode) {
            try {
                const links = JSON.parse(widget.contentCode);
                const sortedLinks = links.sort((a, b) => a.order - b.order);

                return sortedLinks.map(link => {
                    const target = link.openInNewTab ? '_blank' : '_self';
                    const icon = link.icon ? `<i class="${link.icon} me-1"></i>` : '';
                    const externalIcon = link.openInNewTab ? '<i class="bi-box-arrow-up-right ms-1 small"></i>' : '';

                    return `<a href="${link.url}" target="${target}" class="d-block mb-2">${icon}${link.name}${externalIcon}</a>`;
                }).join('');
            } catch (e) {
                return '<div class="text-muted small">Invalid link data</div>';
            }
        }
        if (widget.widgetType === 'ImageLink' && widget.contentCode) {
            try {
                const data = JSON.parse(widget.contentCode);
                const imgTag = `<img src="${data.imageUrl}" class="${data.cssClass || ''}" title="${data.title || ''}" alt="${data.altText || ''}" style="max-width:100%" />`;
                if (data.linkUrl) {
                    const target = data.openInNewTab ? '_blank' : '_self';
                    const rel = data.openInNewTab ? 'noopener noreferrer' : '';
                    return `<a href="${data.linkUrl}" target="${target}" rel="${rel}">${imgTag}</a>`;
                }
                return imgTag;
            } catch (e) {
                return '<div class="text-muted small">Invalid image link data</div>';
            }
        }
        if (widget.widgetType === 'ButtonLink' && widget.contentCode) {
            try {
                const buttons = JSON.parse(widget.contentCode);
                return '<div class="btn-group">' + buttons.map(btn => {
                    const target = btn.openInNewTab ? '_blank' : '_self';
                    const rel = btn.openInNewTab ? 'noopener noreferrer' : '';
                    return `<a href="${btn.url}" target="${target}" rel="${rel}" class="btn ${btn.cssClass || 'btn-outline-primary'}">${btn.text}</a>`;
                }).join('') + '</div>';
            } catch (e) {
                return '<div class="text-muted small">Invalid button link data</div>';
            }
        }
        return '';
    },

    initCreateWidget() {
        this.currentWidgetId = window.emptyGuid;
        this.formData = {
            title: '',
            widgetType: 'LinkList',
            displayOrder: 0,
            isEnabled: true,
            links: [],
            imageLink: {
                imageUrl: '',
                cssClass: '',
                title: '',
                altText: '',
                linkUrl: '',
                openInNewTab: true
            },
            buttons: []
        };
        this.editCanvas.show();
    },

    async editWidget(id) {
        try {
            const widget = await fetch2(`/api/widgets/${id}`, 'GET');
            this.currentWidgetId = widget.id;

            let links = [];
            let imageLink = { imageUrl: '', cssClass: '', title: '', altText: '', linkUrl: '', openInNewTab: true };
            let buttons = [];

            if (widget.widgetType === 'LinkList' && widget.contentCode) {
                try {
                    links = JSON.parse(widget.contentCode);
                } catch (e) {
                    links = [];
                }
            } else if (widget.widgetType === 'ImageLink' && widget.contentCode) {
                try {
                    imageLink = JSON.parse(widget.contentCode);
                } catch (e) {
                    imageLink = { imageUrl: '', cssClass: '', title: '', altText: '', linkUrl: '', openInNewTab: true };
                }
            } else if (widget.widgetType === 'ButtonLink' && widget.contentCode) {
                try {
                    buttons = JSON.parse(widget.contentCode);
                } catch (e) {
                    buttons = [];
                }
            }

            this.formData = {
                title: widget.title,
                widgetType: widget.widgetType,
                displayOrder: widget.displayOrder,
                isEnabled: widget.isEnabled,
                links: links,
                imageLink: imageLink,
                buttons: buttons
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

        const requestData = {
            title: this.formData.title,
            widgetType: this.formData.widgetType,
            displayOrder: this.formData.displayOrder,
            isEnabled: this.formData.isEnabled,
            contentCode: contentCode
        };

        try {
            await fetch2(apiAddress, verb, requestData);

            this.editCanvas.hide();
            await this.loadWidgets();
            success(isCreate ? getLocalizedString('widgetCreated') : getLocalizedString('widgetUpdated'));
        } catch (err) {
            error(err);
        }
    },

    onWidgetTypeChange() {
        if (this.formData.widgetType !== 'LinkList') {
            this.formData.links = [];
        }
        if (this.formData.widgetType !== 'ImageLink') {
            this.formData.imageLink = { imageUrl: '', cssClass: '', title: '', altText: '', linkUrl: '', openInNewTab: true };
        }
        if (this.formData.widgetType !== 'ButtonLink') {
            this.formData.buttons = [];
        }
    },

    // Button management methods
    addNewButton() {
        if (this.formData.buttons.length >= 3) {
            alert(getLocalizedString('maxButtons'));
            return;
        }
        this.buttonDialog = {
            isNew: true,
            index: -1,
            data: {
                text: '',
                url: '',
                cssClass: 'btn-outline-primary',
                openInNewTab: true
            }
        };
        this.buttonModal.show();
    },

    editButton(index) {
        this.buttonDialog = {
            isNew: false,
            index: index,
            data: { ...this.formData.buttons[index] }
        };
        this.buttonModal.show();
    },

    removeButton(index) {
        showConfirmModal({
            title: 'Remove Button',
            body: getLocalizedString('removeButton'),
            confirmText: 'Remove',
            confirmClass: 'btn-outline-danger',
            confirmIcon: 'bi-trash',
            onConfirm: () => {
                this.formData.buttons.splice(index, 1);
                hideConfirmModal();
            }
        });
    },

    saveButtonDialog() {
        if (!this.buttonDialog.data.text || !this.buttonDialog.data.url) {
            alert(getLocalizedString('textUrlRequired'));
            return;
        }

        if (this.buttonDialog.isNew) {
            this.formData.buttons.push({ ...this.buttonDialog.data });
        } else {
            this.formData.buttons[this.buttonDialog.index] = { ...this.buttonDialog.data };
        }

        this.buttonModal.hide();
    },

    // Link management methods
    addNewLink() {
        this.linkDialog = {
            isNew: true,
            index: -1,
            data: {
                name: '',
                url: '',
                icon: '',
                order: this.getNextLinkOrder(),
                openInNewTab: true
            }
        };
        this.linkModal.show();
    },

    editLink(index) {
        const sortedLinks = this.sortedLinks;
        const actualIndex = this.formData.links.findIndex(l => l === sortedLinks[index]);
        
        this.linkDialog = {
            isNew: false,
            index: actualIndex,
            data: { ...this.formData.links[actualIndex] }
        };
        this.linkModal.show();
    },

    removeLink(index) {
        showConfirmModal({
            title: 'Remove Link',
            body: getLocalizedString('removeLink'),
            confirmText: 'Remove',
            confirmClass: 'btn-outline-danger',
            confirmIcon: 'bi-trash',
            onConfirm: () => {
                const sortedLinks = this.sortedLinks;
                const actualIndex = this.formData.links.findIndex(l => l === sortedLinks[index]);
                this.formData.links.splice(actualIndex, 1);
                hideConfirmModal();
            }
        });
    },

    moveLink(index, direction) {
        const sortedLinks = this.sortedLinks;
        const newIndex = index + direction;
        
        if (newIndex >= 0 && newIndex < sortedLinks.length) {
            const tempOrder = sortedLinks[index].order;
            sortedLinks[index].order = sortedLinks[newIndex].order;
            sortedLinks[newIndex].order = tempOrder;
        }
    },

    saveLinkDialog() {
        if (!this.linkDialog.data.name || !this.linkDialog.data.url) {
            alert(getLocalizedString('nameUrlRequired'));
            return;
        }
        
        if (this.linkDialog.isNew) {
            this.formData.links.push({ ...this.linkDialog.data });
        } else {
            this.formData.links[this.linkDialog.index] = { ...this.linkDialog.data };
        }
        
        this.linkModal.hide();
    },

    getNextLinkOrder() {
        return this.formData.links.length > 0 
            ? Math.max(...this.formData.links.map(l => l.order)) + 1 
            : 1;
    },

    getLocalizedString(key) {
        return getLocalizedString(key);
    }
}));

Alpine.start();