import { default as Alpine } from '/lib/alpinejs/alpinejs.3.15.0.module.esm.min.js';
import { fetch2 } from '/js/app/httpService.mjs?v=1500';
import { success } from '/js/app/toastService.mjs';

Alpine.data('widgetManager', () => ({
    widgets: [],
    isLoading: true,
    currentWidgetId: window.emptyGuid,
    editCanvas: null,
    linkModal: null,
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
        await this.loadWidgets();
    },

    async loadWidgets() {
        this.isLoading = true;
        try {
            this.widgets = (await fetch2('/api/widgets/list', 'GET')) ?? [];
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
    },

    async deleteWidget(id) {
        if (confirm('Delete this widget?')) {
            await fetch2(`/api/widgets/${id}`, 'DELETE');
            await this.loadWidgets();
            success('Widget deleted');
        }
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

        await fetch2(apiAddress, verb, requestData);

        this.editCanvas.hide();
        await this.loadWidgets();
        success(isCreate ? 'Widget created' : 'Widget updated');
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
        if (!confirm('Are you sure you want to remove this link?')) return;
        
        const sortedLinks = this.sortedLinks;
        const actualIndex = this.formData.links.findIndex(l => l === sortedLinks[index]);
        
        this.formData.links.splice(actualIndex, 1);
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
            alert('Name and URL are required');
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
    }
}));

Alpine.start();