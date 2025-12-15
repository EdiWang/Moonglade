import { fetch2 } from './httpService.mjs?v=1500'
import { success } from './toastService.mjs'

// State management
const state = {
    editCanvas: null,
    currentWidgetData: null
};

// Widget type configurations
const WIDGET_TYPES = {
    LinkList: {
        contentType: 'JSON',
        editorId: 'linklist-editor'
    }
    // Future widget types can be added here:
    // HtmlContent: { contentType: 'HTML', editorId: 'html-editor' },
    // MarkdownContent: { contentType: 'Markdown', editorId: 'markdown-editor' }
};

// ========================================
// Widget List Management
// ========================================

async function loadWidgets() {
    const widgets = await fetch2('/api/widgets/list', 'GET');
    const widgetsList = document.querySelector('.widgets-list');
    
    widgetsList.innerHTML = '';
    
    // Sort widgets by displayOrder before rendering
    const sortedWidgets = widgets.sort((a, b) => a.displayOrder - b.displayOrder);
    sortedWidgets.forEach(widget => renderWidgetCard(widget, widgetsList));
    
    attachWidgetEventListeners();
}

function renderWidgetCard(widget, container) {
    const widgetEntry = document.createElement('div');
    widgetEntry.className = 'col-md-4';
    widgetEntry.id = `tr-${widget.id}`;
    
    widgetEntry.innerHTML = `
        <div class="widget-entry bg-white p-3 rounded-3 border h-100">
            ${buildWidgetHeader(widget)}
            <hr />
            ${buildWidgetContent(widget)}
        </div>
    `;
    
    container.appendChild(widgetEntry);
}

function buildWidgetHeader(widget) {
    const statusBadge = widget.isEnabled 
        ? '' 
        : '<span class="badge bg-secondary ms-1">Disabled</span>';
    
    return `
        <div class="d-flex justify-content-between align-items-center">
            <h6 class="mb-0">${widget.title}${statusBadge}</h6>
            <div class="gap-2">
                <a class="btn btn-sm btn-outline-accent btn-edit flex-fill" data-widgetid="${widget.id}">
                    <i class="bi-pen"></i> Edit
                </a>
                <a class="btn btn-sm btn-outline-danger btn-delete flex-fill" data-widgetid="${widget.id}">
                    <i class="bi-trash"></i>
                </a>
            </div>
        </div>
    `;
}

function buildWidgetContent(widget) {
    const config = WIDGET_TYPES[widget.widgetType];
    
    if (!config) return '<div class="text-muted small mt-2">Unknown widget type</div>';
    
    // Route to appropriate content builder based on widget type
    switch (widget.widgetType) {
        case 'LinkList':
            return buildLinkListContent(widget.contentCode);
        // Future widget types:
        // case 'HtmlContent': return buildHtmlContent(widget.contentCode);
        // case 'MarkdownContent': return buildMarkdownContent(widget.contentCode);
        default:
            return '';
    }
}

function buildLinkListContent(contentCode) {
    if (!contentCode) return '';
    
    try {
        const links = JSON.parse(contentCode);
        const sortedLinks = links.sort((a, b) => a.order - b.order);
        
        const linkItems = sortedLinks.map(link => {
            const target = link.openInNewTab ? '_blank' : '_self';
            const icon = link.icon ? `<i class="${link.icon} me-1"></i>` : '';
            const externalIcon = link.openInNewTab 
                ? '<i class="bi-box-arrow-up-right ms-1 small"></i>' 
                : '';
            
            return `
                <a href="${link.url}" target="${target}" role="listitem" class="d-block mb-3 mt-2">
                    ${icon}${link.name}${externalIcon}
                </a>
            `;
        }).join('');
        
        return `<div role="list" class="mt-2">${linkItems}</div>`;
    } catch (e) {
        return '<div class="text-muted small mt-2">Invalid link data</div>';
    }
}

function attachWidgetEventListeners() {
    document.querySelectorAll('.btn-edit').forEach(button => {
        button.addEventListener('click', function () {
            const widgetId = this.getAttribute('data-widgetid');
            editWidget(widgetId);
        });
    });
    
    document.querySelectorAll('.btn-delete').forEach(button => {
        button.addEventListener('click', function () {
            const widgetId = this.getAttribute('data-widgetid');
            deleteWidgetConfirm(widgetId);
        });
    });
}

// ========================================
// Widget CRUD Operations
// ========================================

async function editWidget(id) {
    const widget = await fetch2(`/api/widgets/${id}`, 'GET');
    populateWidgetForm(widget);
    showContentEditor(widget.widgetType, widget.contentCode);
    state.editCanvas.show();
}

function openNewWidgetForm() {
    const defaultWidget = {
        id: '',
        title: '',
        widgetType: 'LinkList',
        displayOrder: 0,
        isEnabled: true,
        contentCode: ''
    };
    
    populateWidgetForm(defaultWidget);
    showContentEditor(defaultWidget.widgetType, defaultWidget.contentCode);
    state.editCanvas.show();
}

function populateWidgetForm(widget) {
    document.getElementById('widget-id').value = widget.id;
    document.getElementById('widget-title').value = widget.title;
    document.getElementById('widget-type').value = widget.widgetType;
    document.getElementById('widget-display-order').value = widget.displayOrder;
    document.getElementById('widget-enabled').checked = widget.isEnabled;
}

async function saveWidget(formData) {
    const widgetId = document.getElementById('widget-id').value;
    const widgetType = formData.get('widgetType');
    
    const requestData = {
        title: formData.get('title'),
        widgetType: widgetType,
        displayOrder: parseInt(formData.get('displayOrder')),
        isEnabled: formData.get('isEnabled') === 'true',
        contentCode: getContentFromEditor(widgetType)
    };
    
    const endpoint = widgetId ? `/api/widgets/${widgetId}` : '/api/widgets';
    const method = widgetId ? 'PUT' : 'POST';
    const message = widgetId ? 'Widget updated' : 'Widget created';
    
    await fetch2(endpoint, method, requestData);
    success(message);
    
    state.editCanvas.hide();
    await loadWidgets();
}

async function deleteWidget(widgetId) {
    await fetch2(`/api/widgets/${widgetId}`, 'DELETE');
    document.querySelector(`#tr-${widgetId}`).remove();
    success('Widget deleted');
}

async function deleteWidgetConfirm(id) {
    if (confirm("Delete this widget?")) {
        await deleteWidget(id);
    }
}

// ========================================
// Content Editor Management
// ========================================

function showContentEditor(widgetType, contentCode) {
    // Hide all editors
    Object.values(WIDGET_TYPES).forEach(config => {
        const editor = document.getElementById(config.editorId);
        if (editor) editor.style.display = 'none';
    });
    
    // Show the appropriate editor
    const config = WIDGET_TYPES[widgetType];
    if (config) {
        const editor = document.getElementById(config.editorId);
        if (editor) {
            editor.style.display = 'block';
            initializeEditor(widgetType, contentCode);
        }
    }
}

function initializeEditor(widgetType, contentCode) {
    switch (widgetType) {
        case 'LinkList':
            initializeLinkListEditor(contentCode);
            break;
        // Future editors:
        // case 'HtmlContent': initializeHtmlEditor(contentCode); break;
        // case 'MarkdownContent': initializeMarkdownEditor(contentCode); break;
    }
}

function getContentFromEditor(widgetType) {
    switch (widgetType) {
        case 'LinkList':
            return JSON.stringify(state.currentWidgetData || []);
        // Future editors:
        // case 'HtmlContent': return getHtmlEditorContent();
        // case 'MarkdownContent': return getMarkdownEditorContent();
        default:
            return '';
    }
}

// ========================================
// LinkList Editor
// ========================================

function initializeLinkListEditor(contentCode) {
    try {
        state.currentWidgetData = contentCode ? JSON.parse(contentCode) : [];
    } catch (e) {
        state.currentWidgetData = [];
    }
    renderLinkList();
}

function renderLinkList() {
    const container = document.getElementById('links-container');
    const links = state.currentWidgetData;
    
    if (links.length === 0) {
        container.innerHTML = '<p class="text-muted small text-center py-3 mb-0">No links added yet</p>';
        return;
    }
    
    const sortedLinks = [...links].sort((a, b) => a.order - b.order);
    container.innerHTML = sortedLinks.map((link, index) => buildLinkItem(link, index, sortedLinks.length)).join('');
    attachLinkEventListeners();
}

function buildLinkItem(link, index, totalCount) {
    const iconHtml = link.icon ? `<i class="${link.icon} me-1"></i>` : '';
    const tabIcon = link.openInNewTab 
        ? '<i class="bi-box-arrow-up-right"></i> Opens in new tab' 
        : '<i class="bi-arrow-return-right"></i> Opens in same tab';
    
    return `
        <div class="link-item border rounded p-2 mb-2 bg-light" data-index="${index}">
            <div class="d-flex justify-content-between align-items-start mb-2">
                <strong class="text-truncate me-2">${link.name || 'Unnamed Link'}</strong>
                <div class="btn-group btn-group-sm" role="group">
                    <button type="button" class="btn btn-outline-secondary btn-move-up" 
                            data-index="${index}" ${index === 0 ? 'disabled' : ''}>
                        <i class="bi-arrow-up"></i>
                    </button>
                    <button type="button" class="btn btn-outline-secondary btn-move-down" 
                            data-index="${index}" ${index === totalCount - 1 ? 'disabled' : ''}>
                        <i class="bi-arrow-down"></i>
                    </button>
                    <button type="button" class="btn btn-outline-accent btn-edit-link" 
                            data-index="${index}">
                        <i class="bi-pen"></i>
                    </button>
                    <button type="button" class="btn btn-outline-danger btn-remove-link" 
                            data-index="${index}">
                        <i class="bi-trash"></i>
                    </button>
                </div>
            </div>
            <div class="small text-muted">
                <div class="text-truncate">
                    ${iconHtml}<a href="${link.url}" target="_blank">${link.url}</a>
                </div>
                <div>${tabIcon} #${link.order}</div>
            </div>
        </div>
    `;
}

function attachLinkEventListeners() {
    const container = document.getElementById('links-container');
    
    container.querySelectorAll('.btn-edit-link').forEach(btn => {
        btn.addEventListener('click', () => editLink(parseInt(btn.dataset.index)));
    });
    
    container.querySelectorAll('.btn-remove-link').forEach(btn => {
        btn.addEventListener('click', () => removeLink(parseInt(btn.dataset.index)));
    });
    
    container.querySelectorAll('.btn-move-up').forEach(btn => {
        btn.addEventListener('click', () => moveLink(parseInt(btn.dataset.index), -1));
    });
    
    container.querySelectorAll('.btn-move-down').forEach(btn => {
        btn.addEventListener('click', () => moveLink(parseInt(btn.dataset.index), 1));
    });
}

function addNewLink() {
    const newLink = {
        name: '',
        icon: '',
        url: '',
        openInNewTab: true,
        order: getNextLinkOrder()
    };
    
    showLinkDialog(newLink, -1);
}

function editLink(index) {
    const sortedLinks = [...state.currentWidgetData].sort((a, b) => a.order - b.order);
    const actualIndex = state.currentWidgetData.findIndex(l => l === sortedLinks[index]);
    showLinkDialog(state.currentWidgetData[actualIndex], actualIndex);
}

function removeLink(index) {
    if (!confirm('Are you sure you want to remove this link?')) return;
    
    const sortedLinks = [...state.currentWidgetData].sort((a, b) => a.order - b.order);
    const actualIndex = state.currentWidgetData.findIndex(l => l === sortedLinks[index]);
    
    state.currentWidgetData.splice(actualIndex, 1);
    renderLinkList();
}

function moveLink(index, direction) {
    const sortedLinks = [...state.currentWidgetData].sort((a, b) => a.order - b.order);
    const newIndex = index + direction;
    
    if (newIndex >= 0 && newIndex < sortedLinks.length) {
        // Swap orders
        const tempOrder = sortedLinks[index].order;
        sortedLinks[index].order = sortedLinks[newIndex].order;
        sortedLinks[newIndex].order = tempOrder;
        
        renderLinkList();
    }
}

function getNextLinkOrder() {
    return state.currentWidgetData.length > 0 
        ? Math.max(...state.currentWidgetData.map(l => l.order)) + 1 
        : 1;
}

// ========================================
// Link Dialog
// ========================================

function showLinkDialog(link, index) {
    const isNew = index === -1;
    const dialog = createLinkDialog(link, isNew);
    
    document.body.appendChild(dialog);
    const modalInstance = new bootstrap.Modal(dialog);
    
    setupLinkDialogHandlers(dialog, modalInstance, link, index, isNew);
    modalInstance.show();
}

function createLinkDialog(link, isNew) {
    const dialog = document.createElement('div');
    dialog.className = 'modal fade';
    dialog.id = 'linkDialog';
    dialog.tabIndex = -1;
    dialog.setAttribute('aria-hidden', 'true');
    
    dialog.innerHTML = `
        <div class="modal-dialog">
            <div class="modal-content">
                <div class="modal-header">
                    <h5 class="modal-title">${isNew ? 'Add New Link' : 'Edit Link'}</h5>
                    <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                </div>
                <div class="modal-body">
                    ${buildLinkFormFields(link)}
                </div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-outline-secondary" data-bs-dismiss="modal">Cancel</button>
                    <button type="button" class="btn btn-outline-accent" id="btn-save-link">Save</button>
                </div>
            </div>
        </div>
    `;
    
    return dialog;
}

function buildLinkFormFields(link) {
    return `
        <div class="mb-3">
            <label for="link-name" class="form-label">Link Name *</label>
            <input type="text" id="link-name" class="form-control" value="${link.name}" required />
        </div>
        <div class="mb-3">
            <label for="link-url" class="form-label">URL *</label>
            <input type="url" id="link-url" class="form-control" value="${link.url}" 
                   placeholder="https://example.com" required />
        </div>
        <div class="mb-3">
            <label for="link-icon" class="form-label">Icon Class (optional)</label>
            <input type="text" id="link-icon" class="form-control" value="${link.icon}" 
                   placeholder="bi-link-45deg" />
            <small class="form-text text-muted">Use Bootstrap Icons classes (e.g., bi-link-45deg, bi-github)</small>
        </div>
        <div class="mb-3">
            <label for="link-order" class="form-label">Display Order</label>
            <input type="number" id="link-order" class="form-control" value="${link.order}" 
                   min="0" required />
        </div>
        <div class="mb-3 form-check">
            <input type="checkbox" id="link-new-tab" class="form-check-input" 
                   ${link.openInNewTab ? 'checked' : ''} />
            <label for="link-new-tab" class="form-check-label">Open in new tab</label>
        </div>
    `;
}

function setupLinkDialogHandlers(dialog, modalInstance, link, index, isNew) {
    dialog.querySelector('#btn-save-link').addEventListener('click', () => {
        const updatedLink = {
            name: dialog.querySelector('#link-name').value.trim(),
            url: dialog.querySelector('#link-url').value.trim(),
            icon: dialog.querySelector('#link-icon').value.trim(),
            order: parseInt(dialog.querySelector('#link-order').value),
            openInNewTab: dialog.querySelector('#link-new-tab').checked
        };
        
        if (!updatedLink.name || !updatedLink.url) {
            alert('Name and URL are required');
            return;
        }
        
        if (isNew) {
            state.currentWidgetData.push(updatedLink);
        } else {
            state.currentWidgetData[index] = updatedLink;
        }
        
        renderLinkList();
        modalInstance.hide();
    });
    
    dialog.addEventListener('hidden.bs.modal', () => {
        dialog.remove();
    });
}

// ========================================
// Initialization
// ========================================

document.addEventListener('DOMContentLoaded', function() {
    state.editCanvas = new bootstrap.Offcanvas(document.getElementById('editWidgetCanvas'));
    
    document.querySelector('#btn-new-widget').addEventListener('click', openNewWidgetForm);
    document.getElementById('widget-type').addEventListener('change', function() {
        showContentEditor(this.value, '');
    });
    document.getElementById('btn-add-link').addEventListener('click', addNewLink);
    
    document.getElementById('edit-form').addEventListener('submit', async function(e) {
        e.preventDefault();
        
        const formData = new FormData();
        formData.append('title', document.getElementById('widget-title').value);
        formData.append('widgetType', document.getElementById('widget-type').value);
        formData.append('displayOrder', document.getElementById('widget-display-order').value);
        formData.append('isEnabled', document.getElementById('widget-enabled').checked);
        
        await saveWidget(formData);
    });
    
    loadWidgets();
});