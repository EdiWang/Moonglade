import { Alpine } from '/js/app/alpine-init.mjs';
import { fetch2 } from '/js/app/httpService.mjs?v=1500';
import { success, error } from '/js/app/toastService.mjs';
import { getLocalizedString } from './utils.module.mjs';

Alpine.data('widgetManager', () => ({
widgets: [],
isLoading: true,
currentWidgetId: window.emptyGuid,
editCanvas: null,
linkModal: null,
confirmDeleteModal: null,
deleteConfirm: {
    title: '',
    message: '',
    buttonText: '',
    callback: null
},
deleteTarget: {
    widgetId: null,
    linkIndex: -1
},
formData: {
        title: '',
        widgetType: 'LinkList',
        displayOrder: 0,
        isEnabled: true,
        links: []
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

    async init() {
        this.editCanvas = new bootstrap.Offcanvas(this.$refs.editWidgetCanvas);
        this.linkModal = new bootstrap.Modal(this.$refs.linkDialogModal);
        this.confirmDeleteModal = new bootstrap.Modal(this.$refs.confirmDeleteModal);
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
        return '';
    },

    initCreateWidget() {
        this.currentWidgetId = window.emptyGuid;
        this.formData = {
            title: '',
            widgetType: 'LinkList',
            displayOrder: 0,
            isEnabled: true,
            links: []
        };
        this.editCanvas.show();
    },

    async editWidget(id) {
        try {
            const widget = await fetch2(`/api/widgets/${id}`, 'GET');
            this.currentWidgetId = widget.id;
        
            let links = [];
            if (widget.widgetType === 'LinkList' && widget.contentCode) {
                try {
                    links = JSON.parse(widget.contentCode);
                } catch (e) {
                    links = [];
                }
            }
        
            this.formData = {
                title: widget.title,
                widgetType: widget.widgetType,
                displayOrder: widget.displayOrder,
                isEnabled: widget.isEnabled,
                links: links
            };
        
            this.editCanvas.show();
        } catch (err) {
            error(err);
        }
    },

    deleteWidget(id) {
        this.deleteTarget.widgetId = id;
        this.deleteConfirm = {
            title: 'Delete Widget',
            message: getLocalizedString('deleteWidget'),
            buttonText: 'Delete',
            callback: async () => {
                try {
                    await fetch2(`/api/widgets/${this.deleteTarget.widgetId}`, 'DELETE');
                    this.confirmDeleteModal.hide();
                    await this.loadWidgets();
                    success(getLocalizedString('widgetDeleted'));
                } catch (err) {
                    console.error(err);
                }
            }
        };
        this.confirmDeleteModal.show();
    },

    async handleSubmit() {
        const isCreate = this.currentWidgetId === window.emptyGuid;
        const apiAddress = isCreate ? '/api/widgets' : `/api/widgets/${this.currentWidgetId}`;
        const verb = isCreate ? 'POST' : 'PUT';

        const requestData = {
            title: this.formData.title,
            widgetType: this.formData.widgetType,
            displayOrder: this.formData.displayOrder,
            isEnabled: this.formData.isEnabled,
            contentCode: this.formData.widgetType === 'LinkList' 
                ? JSON.stringify(this.formData.links) 
                : ''
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
        // Reset links when widget type changes
        if (this.formData.widgetType !== 'LinkList') {
            this.formData.links = [];
        }
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
        this.deleteTarget.linkIndex = index;
        this.deleteConfirm = {
            title: 'Remove Link',
            message: getLocalizedString('removeLink'),
            buttonText: 'Remove',
            callback: () => {
                const sortedLinks = this.sortedLinks;
                const actualIndex = this.formData.links.findIndex(l => l === sortedLinks[this.deleteTarget.linkIndex]);
                
                this.formData.links.splice(actualIndex, 1);
                this.confirmDeleteModal.hide();
            }
        };
        this.confirmDeleteModal.show();
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