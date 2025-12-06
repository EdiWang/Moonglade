import { fetch2 } from './httpService.mjs?v=1500'
import { success } from './toastService.mjs'

let editCanvas;

function getWidgetTypeDisplayName(widgetType) {
    const typeMap = {
        'LinkList': 'Link List'
    };
    
    return typeMap[widgetType] || widgetType || '';
}

async function loadWidgets() {
    const widgets = await fetch2('/api/widgets/list', 'GET');
    const widgetsList = document.querySelector('.widgets-list');
    
    widgetsList.innerHTML = '';
    
    widgets.forEach(widget => {
        const widgetEntry = document.createElement('div');
        widgetEntry.className = 'widget-entry p-3 rounded-3 border mb-1';
        widgetEntry.id = `tr-${widget.id}`;
        
        widgetEntry.innerHTML = `
            <div class="row">
                <div class="col">
                    <h6>
                        <span class="badge bg-accent1 me-1">${widget.displayOrder}</span> ${widget.title}
                        ${!widget.isEnabled ? '<span class="badge bg-secondary ms-1">Disabled</span>' : ''}
                    </h6>
                    <div class="text-muted small">${getWidgetTypeDisplayName(widget.widgetType)}</div>
                </div>
                <div class="col-auto">
                    <a class="btn btn-sm btn-outline-accent btn-edit" data-widgetid="${widget.id}"><i class="bi-pen"></i></a>
                    <a class="btn btn-sm btn-outline-danger btn-delete" data-widgetid="${widget.id}"><i class="bi-trash"></i></a>
                </div>
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