import { fetch2 } from './httpService.mjs?v=1500'
import { success } from './toastService.mjs'

let editCanvas;

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
    
    // Show the offcanvas
    editCanvas.show();
}

async function saveWidget(formData) {
    const widgetId = document.getElementById('widget-id').value;
    const requestData = {
        title: formData.get('title'),
        widgetType: formData.get('widgetType'),
        displayOrder: parseInt(formData.get('displayOrder')),
        isEnabled: formData.get('isEnabled') === 'true'
    };
    
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