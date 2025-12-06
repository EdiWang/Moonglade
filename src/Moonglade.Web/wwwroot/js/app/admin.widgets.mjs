import { fetch2 } from './httpService.mjs?v=1500'
import { success } from './toastService.mjs'

function getWidgetTypeDisplayName(widgetType) {
    const typeMap = {
        'LinkList': 'Link List'
        // Add more mappings here as needed
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
                    <h6>${widget.title}</h6>
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
    const data = await fetch2(`/api/widgets/${id}`, 'GET', {});
    
    // TODO: Populate edit form when implemented
    console.log('Edit widget:', data);
}

async function deleteWidget(widgetId) {
    await fetch2(`/api/widgets/${widgetId}`, 'DELETE', {});
    
    document.querySelector(`#tr-${widgetId}`).remove();
    success('Widget deleted');
}

async function deleteWidgetConfirm(id) {
    var cfm = confirm("Delete Confirmation?");
    if (cfm) await deleteWidget(id);
}

document.querySelector('#btn-new-widget').addEventListener('click', function() {
    // TODO: Implement new widget creation
    console.log('New widget clicked');
});

// Load widgets on page load
loadWidgets();