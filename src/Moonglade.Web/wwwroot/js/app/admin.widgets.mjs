import { fetch2 } from './httpService.mjs?v=1500'
import { success } from './toastService.mjs'

let editCanvas;
let linkItems = [];

async function loadWidgets() {
    const widgets = await fetch2('/api/widgets/list', 'GET');
    const widgetsList = document.querySelector('.widgets-list');
    
    widgetsList.innerHTML = '';
    
    widgets.forEach(widget => {
        const widgetEntry = document.createElement('div');
        widgetEntry.className = 'col-12 col-md-6 col-lg-4';
        widgetEntry.id = `tr-${widget.id}`;
        
        // Parse and render LinkList content
        let contentHtml = '';
        if (widget.widgetType === 'LinkList' && widget.contentCode) {
            try {
                const links = JSON.parse(widget.contentCode);
                contentHtml = '<div role="list" class="mt-2">';
                links
                    .sort((a, b) => a.order - b.order)
                    .forEach(link => {
                        const target = link.openInNewTab ? '_blank' : '_self';
                        const icon = link.icon ? `<i class="${link.icon} me-1"></i>` : '';
                        contentHtml += `
                                <a href="${link.url}" target="${target}" role="listitem" class="d-block mb-3 mt-2">
                                    ${icon}${link.name}
                                    ${link.openInNewTab ? '<i class="bi-box-arrow-up-right ms-1 small"></i>' : ''}
                                </a>
                        `;
                    });
                contentHtml += '</div>';
            } catch (e) {
                contentHtml = '<div class="text-muted small mt-2">Invalid link data</div>';
            }
        }
        
        widgetEntry.innerHTML = `
            <div class="widget-entry p-3 rounded-3 border h-100">
                <div class="d-flex justify-content-between align-items-center">
                    <h6 class="mb-0">
                        ${widget.title}
                        ${!widget.isEnabled ? '<span class="badge bg-secondary ms-1">Disabled</span>' : ''}
                    </h6>
                    <div class="gap-2">
                        <a class="btn btn-sm btn-outline-accent btn-edit flex-fill" data-widgetid="${widget.id}"><i class="bi-pen"></i> Edit Properties</a>
                        <a class="btn btn-sm btn-outline-danger btn-delete flex-fill" data-widgetid="${widget.id}"><i class="bi-trash"></i></a>
                    </div>
                </div>
                <hr />
                ${contentHtml}
            </div>
        `;
        
        widgetsList.appendChild(widgetEntry);
    });
    
    // Attach event listeners after rendering
    document.querySelectorAll('.btn-edit').forEach(button => {
        button.addEventListener('click', async function () {
            const wid = this.getAttribute('data-widgetid');
            await editWidget(wid);
        });
    });
    
    document.querySelectorAll('.btn-delete').forEach(button => {
        button.addEventListener('click', async function () {
            const wid = this.getAttribute('data-widgetid');
            await deleteWidgetConfirm(wid);
        });
    });
}

async function editWidget(id) {
    const data = await fetch2(`/api/widgets/${id}`, 'GET');
    
    // Populate the form
    document.getElementById('widget-id').value = data.id;
    document.getElementById('widget-title').value = data.title;
    document.getElementById('widget-type').value = data.widgetType;
    document.getElementById('widget-display-order').value = data.displayOrder;
    document.getElementById('widget-enabled').checked = data.isEnabled;
    
    // Load LinkList content if applicable
    linkItems = [];
    if (data.widgetType === 'LinkList' && data.contentCode) {
        try {
            linkItems = JSON.parse(data.contentCode);
        } catch (e) {
            linkItems = [];
        }
    }
    
    toggleLinkListEditor(data.widgetType);
    renderLinks();
    
    // Show the offcanvas
    editCanvas.show();
}

function openNewWidgetForm() {
    // Clear the form for new widget
    document.getElementById('widget-id').value = '';
    document.getElementById('widget-title').value = '';
    document.getElementById('widget-type').value = 'LinkList';
    document.getElementById('widget-display-order').value = '0';
    document.getElementById('widget-enabled').checked = true;
    
    // Reset links
    linkItems = [];
    toggleLinkListEditor('LinkList');
    renderLinks();
    
    // Show the offcanvas
    editCanvas.show();
}

function toggleLinkListEditor(widgetType) {
    const linkListEditor = document.getElementById('linklist-editor');
    linkListEditor.style.display = widgetType === 'LinkList' ? 'block' : 'none';
}

function renderLinks() {
    const container = document.getElementById('links-container');
    
    if (linkItems.length === 0) {
        container.innerHTML = '<p class="text-muted small text-center py-3 mb-0">No links added yet</p>';
        return;
    }
    
    // Sort by order
    const sortedLinks = [...linkItems].sort((a, b) => a.order - b.order);
    
    container.innerHTML = sortedLinks.map((link, index) => `
        <div class="link-item border rounded p-2 mb-2 bg-light" data-index="${index}">
            <div class="d-flex justify-content-between align-items-start mb-2">
                <strong class="text-truncate me-2">${link.name || 'Unnamed Link'}</strong>
                <div class="btn-group btn-group-sm" role="group">
                    <button type="button" class="btn btn-outline-secondary btn-move-up" data-index="${index}" ${index === 0 ? 'disabled' : ''}>
                        <i class="bi-arrow-up"></i>
                    </button>
                    <button type="button" class="btn btn-outline-secondary btn-move-down" data-index="${index}" ${index === sortedLinks.length - 1 ? 'disabled' : ''}>
                        <i class="bi-arrow-down"></i>
                    </button>
                    <button type="button" class="btn btn-outline-primary btn-edit-link" data-index="${index}">
                        <i class="bi-pen"></i>
                    </button>
                    <button type="button" class="btn btn-outline-danger btn-remove-link" data-index="${index}">
                        <i class="bi-trash"></i>
                    </button>
                </div>
            </div>
            <div class="small text-muted">
                <div class="text-truncate">${link.icon ? `<i class="${link.icon} me-1"></i>` : ''}<a href="${link.url}" target="_blank">${link.url}</a></div>
                <div>${link.openInNewTab ? '<i class="bi-box-arrow-up-right"></i> Opens in new tab' : '<i class="bi-arrow-return-right"></i> Opens in same tab'} • Order: ${link.order}</div>
            </div>
        </div>
    `).join('');
    
    // Attach event listeners
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
        order: linkItems.length > 0 ? Math.max(...linkItems.map(l => l.order)) + 1 : 1
    };
    
    showLinkDialog(newLink, -1);
}

function editLink(index) {
    const sortedLinks = [...linkItems].sort((a, b) => a.order - b.order);
    const actualIndex = linkItems.findIndex(l => l === sortedLinks[index]);
    showLinkDialog(linkItems[actualIndex], actualIndex);
}

function removeLink(index) {
    const sortedLinks = [...linkItems].sort((a, b) => a.order - b.order);
    const actualIndex = linkItems.findIndex(l => l === sortedLinks[index]);
    
    if (confirm('Are you sure you want to remove this link?')) {
        linkItems.splice(actualIndex, 1);
        renderLinks();
    }
}

function moveLink(index, direction) {
    const sortedLinks = [...linkItems].sort((a, b) => a.order - b.order);
    const newIndex = index + direction;
    
    if (newIndex >= 0 && newIndex < sortedLinks.length) {
        // Swap orders
        const temp = sortedLinks[index].order;
        sortedLinks[index].order = sortedLinks[newIndex].order;
        sortedLinks[newIndex].order = temp;
        
        renderLinks();
    }
}

function showLinkDialog(link, index) {
    const isNew = index === -1;
    const dialogHtml = `
        <div class="modal fade" id="linkDialog" tabindex="-1" aria-hidden="true">
            <div class="modal-dialog">
                <div class="modal-content">
                    <div class="modal-header">
                        <h5 class="modal-title">${isNew ? 'Add New Link' : 'Edit Link'}</h5>
                        <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                    </div>
                    <div class="modal-body">
                        <div class="mb-3">
                            <label for="link-name" class="form-label">Link Name *</label>
                            <input type="text" id="link-name" class="form-control" value="${link.name}" required />
                        </div>
                        <div class="mb-3">
                            <label for="link-url" class="form-label">URL *</label>
                            <input type="url" id="link-url" class="form-control" value="${link.url}" placeholder="https://example.com" required />
                        </div>
                        <div class="mb-3">
                            <label for="link-icon" class="form-label">Icon Class (optional)</label>
                            <input type="text" id="link-icon" class="form-control" value="${link.icon}" placeholder="bi-link-45deg" />
                            <small class="form-text text-muted">Use Bootstrap Icons classes (e.g., bi-link-45deg, bi-github)</small>
                        </div>
                        <div class="mb-3">
                            <label for="link-order" class="form-label">Display Order</label>
                            <input type="number" id="link-order" class="form-control" value="${link.order}" min="0" required />
                        </div>
                        <div class="mb-3 form-check">
                            <input type="checkbox" id="link-new-tab" class="form-check-input" ${link.openInNewTab ? 'checked' : ''} />
                            <label for="link-new-tab" class="form-check-label">Open in new tab</label>
                        </div>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                        <button type="button" class="btn btn-primary" id="btn-save-link">Save Link</button>
                    </div>
                </div>
            </div>
        </div>
    `;
    
    // Remove existing dialog if any
    const existingDialog = document.getElementById('linkDialog');
    if (existingDialog) {
        existingDialog.remove();
    }
    
    // Add dialog to body
    document.body.insertAdjacentHTML('beforeend', dialogHtml);
    
    const dialog = new bootstrap.Modal(document.getElementById('linkDialog'));
    
    // Handle save
    document.getElementById('btn-save-link').addEventListener('click', () => {
        const name = document.getElementById('link-name').value.trim();
        const url = document.getElementById('link-url').value.trim();
        const icon = document.getElementById('link-icon').value.trim();
        const order = parseInt(document.getElementById('link-order').value);
        const openInNewTab = document.getElementById('link-new-tab').checked;
        
        if (!name || !url) {
            alert('Name and URL are required');
            return;
        }
        
        const updatedLink = { name, url, icon, order, openInNewTab };
        
        if (isNew) {
            linkItems.push(updatedLink);
        } else {
            linkItems[index] = updatedLink;
        }
        
        renderLinks();
        dialog.hide();
    });
    
    // Clean up on hide
    document.getElementById('linkDialog').addEventListener('hidden.bs.modal', () => {
        document.getElementById('linkDialog').remove();
    });
    
    dialog.show();
}

async function saveWidget(formData) {
    const widgetId = document.getElementById('widget-id').value;
    const widgetType = formData.get('widgetType');
    
    const requestData = {
        title: formData.get('title'),
        widgetType: widgetType,
        displayOrder: parseInt(formData.get('displayOrder')),
        isEnabled: formData.get('isEnabled') === 'true'
    };
    
    // Add contentCode for LinkList widgets
    if (widgetType === 'LinkList') {
        requestData.contentCode = JSON.stringify(linkItems);
    }
    
    if (widgetId) {
        // Update existing widget
        await fetch2(`/api/widgets/${widgetId}`, 'PUT', requestData);
        success('Widget updated');
    } else {
        // Create new widget
        await fetch2('/api/widgets', 'POST', requestData);
        success('Widget created');
    }
    
    editCanvas.hide();
    await loadWidgets();
}

async function deleteWidget(widgetId) {
    await fetch2(`/api/widgets/${widgetId}`, 'DELETE');
    
    document.querySelector(`#tr-${widgetId}`).remove();
    success('Widget deleted');
}

async function deleteWidgetConfirm(id) {
    var cfm = confirm("Delete Confirmation?");
    if (cfm) await deleteWidget(id);
}

// Initialize on page load
document.addEventListener('DOMContentLoaded', function() {
    // Initialize the offcanvas
    const editCanvasElement = document.getElementById('editWidgetCanvas');
    editCanvas = new bootstrap.Offcanvas(editCanvasElement);
    
    // Handle new widget button
    document.querySelector('#btn-new-widget').addEventListener('click', function() {
        openNewWidgetForm();
    });
    
    // Handle widget type change
    document.getElementById('widget-type').addEventListener('change', function() {
        toggleLinkListEditor(this.value);
    });
    
    // Handle add link button
    document.getElementById('btn-add-link').addEventListener('click', function() {
        addNewLink();
    });
    
    // Handle form submission
    const editForm = document.getElementById('edit-form');
    editForm.addEventListener('submit', async function(e) {
        e.preventDefault();
        
        const formData = new FormData();
        formData.append('title', document.getElementById('widget-title').value);
        formData.append('widgetType', document.getElementById('widget-type').value);
        formData.append('displayOrder', document.getElementById('widget-display-order').value);
        formData.append('isEnabled', document.getElementById('widget-enabled').checked);
        
        await saveWidget(formData);
    });
    
    // Load widgets
    loadWidgets();
});