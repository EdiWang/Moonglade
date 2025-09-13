import { handleSettingsSubmit } from './admin.settings.mjs';

let menuData = [];
let menuItemModal, subMenuItemModal;

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    initializeModals();
    loadExistingMenuData();
    bindEvents();
    updateDisplay();
});

function initializeModals() {
    menuItemModal = new bootstrap.Modal(document.getElementById('menuItemModal'));
    subMenuItemModal = new bootstrap.Modal(document.getElementById('subMenuItemModal'));
}

function loadExistingMenuData() {
    const jsonData = document.getElementById("textarea-menu-json").value;
    try {
        if (jsonData && jsonData.trim() !== '' && jsonData.trim() !== '[]') {
            menuData = JSON.parse(jsonData);
            console.log('Parsed menu data:', menuData); // Debug log
        }
    } catch (e) {
        console.error('Error parsing existing menu data:', e);
        menuData = [];
    }
}

function bindEvents() {
    // Add menu item button
    document.getElementById('btn-add-menu').addEventListener('click', () => {
        openMenuItemModal();
    });

    // Clear menus button
    document.getElementById('btn-clear-menus').addEventListener('click', clearMenus);

    // Save menu item
    document.getElementById('save-menu-item').addEventListener('click', saveMenuItem);

    // Save sub menu item
    document.getElementById('save-sub-menu-item').addEventListener('click', saveSubMenuItem);

    // Icon preview
    document.getElementById('menu-icon').addEventListener('input', updateIconPreview);

    // Form submission
    const form = document.querySelector('#form-settings');
    form.addEventListener('submit', handleSubmit);
}

function updateIconPreview() {
    const iconInput = document.getElementById('menu-icon');
    const iconPreview = document.getElementById('icon-preview');
    const iconClass = iconInput.value || 'bi-star';
    iconPreview.className = iconClass;
}

function openMenuItemModal(index = null) {
    const isEdit = index !== null;
    document.getElementById('menuItemModalLabel').textContent = isEdit ? 'Edit Menu Item' : 'Add Menu Item';
    document.getElementById('menu-item-index').value = index !== null ? index : '';

    if (isEdit && menuData[index]) {
        const item = menuData[index];
        document.getElementById('menu-title').value = item.title || '';
        document.getElementById('menu-url').value = item.url || '';
        document.getElementById('menu-icon').value = item.icon || '';
        document.getElementById('menu-order').value = item.displayOrder || 1;
        document.getElementById('menu-new-tab').checked = item.isOpenInNewTab || false;
    } else {
        // Reset form for new item
        document.getElementById('menu-item-form').reset();
        document.getElementById('menu-order').value = menuData.length + 1;
        document.getElementById('menu-icon').value = 'bi-star';
    }

    updateIconPreview();
    menuItemModal.show();
}

function openSubMenuItemModal(parentIndex, subIndex = null) {
    const isEdit = subIndex !== null;
    document.getElementById('subMenuItemModalLabel').textContent = isEdit ? 'Edit Sub Menu Item' : 'Add Sub Menu Item';
    document.getElementById('sub-menu-parent-index').value = parentIndex;
    document.getElementById('sub-menu-item-index').value = subIndex !== null ? subIndex : '';

    if (isEdit && menuData[parentIndex] && menuData[parentIndex].subMenus[subIndex]) {
        const item = menuData[parentIndex].subMenus[subIndex];
        document.getElementById('sub-menu-title').value = item.title || '';
        document.getElementById('sub-menu-url').value = item.url || '';
        document.getElementById('sub-menu-new-tab').checked = item.isOpenInNewTab || false;
    } else {
        // Reset form for new item
        document.getElementById('sub-menu-item-form').reset();
    }

    subMenuItemModal.show();
}

function saveMenuItem() {
    const indexValue = document.getElementById('menu-item-index').value;
    const isEdit = indexValue !== '';
    const index = isEdit ? parseInt(indexValue) : null;

    const menuItem = {
        title: document.getElementById('menu-title').value.trim(),
        url: document.getElementById('menu-url').value.trim(),
        icon: document.getElementById('menu-icon').value.trim() || 'bi-star',
        displayOrder: parseInt(document.getElementById('menu-order').value) || 1,
        isOpenInNewTab: document.getElementById('menu-new-tab').checked,
        subMenus: isEdit && menuData[index] ? menuData[index].subMenus : []
    };

    if (!menuItem.Title) {
        alert('Title is required');
        return;
    }

    if (isEdit) {
        menuData[index] = menuItem;
    } else {
        menuData.push(menuItem);
    }

    menuItemModal.hide();
    updateDisplay();
    updateJsonTextarea();
}

function saveSubMenuItem() {
    const parentIndex = parseInt(document.getElementById('sub-menu-parent-index').value);
    const subIndexValue = document.getElementById('sub-menu-item-index').value;
    const isEdit = subIndexValue !== '';
    const subIndex = isEdit ? parseInt(subIndexValue) : null;

    const subMenuItem = {
        title: document.getElementById('sub-menu-title').value.trim(),
        url: document.getElementById('sub-menu-url').value.trim(),
        isOpenInNewTab: document.getElementById('sub-menu-new-tab').checked
    };

    if (!subMenuItem.title || !subMenuItem.url) {
        alert('Title and URL are required');
        return;
    }

    if (!menuData[parentIndex].subMenus) {
        menuData[parentIndex].subMenus = [];
    }

    if (isEdit) {
        menuData[parentIndex].subMenus[subIndex] = subMenuItem;
    } else {
        menuData[parentIndex].subMenus.push(subMenuItem);
    }

    subMenuItemModal.hide();
    updateDisplay();
    updateJsonTextarea();
}

function deleteMenuItem(index) {
    if (confirm('Are you sure you want to delete this menu item?')) {
        menuData.splice(index, 1);
        updateDisplay();
        updateJsonTextarea();
    }
}

function deleteSubMenuItem(parentIndex, subIndex) {
    if (confirm('Are you sure you want to delete this sub menu item?')) {
        menuData[parentIndex].subMenus.splice(subIndex, 1);
        updateDisplay();
        updateJsonTextarea();
    }
}

function moveMenuItem(index, direction) {
    const newIndex = direction === 'up' ? index - 1 : index + 1;
    if (newIndex >= 0 && newIndex < menuData.length) {
        const temp = menuData[index];
        menuData[index] = menuData[newIndex];
        menuData[newIndex] = temp;
        
        // Update display orders
        menuData[index].displayOrder = index + 1;
        menuData[newIndex].displayOrder = newIndex + 1;
        
        updateDisplay();
        updateJsonTextarea();
    }
}

function updateDisplay() {
    const container = document.getElementById('menu-list');
    const emptyState = document.getElementById('empty-state');

    if (menuData.length === 0) {
        container.innerHTML = '';
        emptyState.style.display = 'block';
        return;
    }

    emptyState.style.display = 'none';
    
    // Display menus in their natural array order, don't sort
    container.innerHTML = menuData.map((item, index) => {
        return createMenuItemHtml(item, index);
    }).join('');
}

function createMenuItemHtml(item, index) {
    const hasSubMenus = item.subMenus && item.subMenus.length > 0;
    const iconClass = item.icon || 'bi-star';
    
    return `
        <div class="menu-item-card" data-index="${index}">
            <div class="menu-item-header">
                <div class="d-flex align-items-center flex-grow-1">
                    <i class="drag-handle bi-grip-vertical me-2"></i>
                    <i class="${iconClass} icon-preview"></i>
                    <div class="ms-2">
                        <strong>${escapeHtml(item.title)}</strong>
                        
                        <div class="text-muted small">
                            #${item.displayOrder || 1}
                            ${item.isOpenInNewTab ? ' • Opens in new tab' : ''}

                            ${item.url ? `<code>${escapeHtml(item.url)}</code>` : ''}
                        </div>
                    </div>
                </div>
                <div class="btn-group">
                    <button type="button" class="btn btn-sm btn-outline-secondary" onclick="moveMenuItem(${index}, 'up')" ${index === 0 ? 'disabled' : ''}>
                        <i class="bi-arrow-up"></i>
                    </button>
                    <button type="button" class="btn btn-sm btn-outline-secondary" onclick="moveMenuItem(${index}, 'down')" ${index === menuData.length - 1 ? 'disabled' : ''}>
                        <i class="bi-arrow-down"></i>
                    </button>
                    <button type="button" class="btn btn-sm btn-outline-accent" onclick="openMenuItemModal(${index})">
                        <i class="bi-pencil"></i>
                    </button>
                    <button type="button" class="btn btn-sm btn-outline-danger" onclick="deleteMenuItem(${index})">
                        <i class="bi-trash"></i>
                    </button>
                </div>
            </div>
            
            ${hasSubMenus ? createSubMenusHtml(item.subMenus, index) : ''}
            
            <div class="submenu-container">
                <button type="button" class="btn btn-sm btn-outline-accent" onclick="openSubMenuItemModal(${index})">
                    <i class="bi-plus"></i> Add Sub Menu Item
                </button>
            </div>
        </div>
    `;
}

function createSubMenusHtml(subMenus, parentIndex) {
    if (!subMenus || subMenus.length === 0) return '';
    
    return `
        <div class="submenu-container">
            ${subMenus.map((subItem, subIndex) => `
                <div class="submenu-item">
                    <div>
                        <strong>${escapeHtml(subItem.title)}</strong>
                        <code>${escapeHtml(subItem.url)}</code>
                        ${subItem.isOpenInNewTab ? '<span class="badge bg-secondary">New Tab</span>' : ''}
                    </div>
                    <div class="btn-group">
                        <button type="button" class="btn btn-sm btn-outline-primary" onclick="openSubMenuItemModal(${parentIndex}, ${subIndex})">
                            <i class="bi-pencil"></i>
                        </button>
                        <button type="button" class="btn btn-sm btn-outline-danger" onclick="deleteSubMenuItem(${parentIndex}, ${subIndex})">
                            <i class="bi-trash"></i>
                        </button>
                    </div>
                </div>
            `).join('')}
        </div>
    `;
}

function clearMenus() {
    if (confirm('Are you sure you want to clear all menu items?')) {
        menuData = [];
        updateDisplay();
        updateJsonTextarea();
    }
}

function updateJsonTextarea() {
    const jsonTextarea = document.getElementById("textarea-menu-json");
    jsonTextarea.value = JSON.stringify(menuData, null, 2);
}

function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

async function handleSubmit(event) {
    updateJsonTextarea();
    await handleSettingsSubmit(event);
}

// Make functions globally available for onclick handlers
window.openMenuItemModal = openMenuItemModal;
window.openSubMenuItemModal = openSubMenuItemModal;
window.deleteMenuItem = deleteMenuItem;
window.deleteSubMenuItem = deleteSubMenuItem;
window.moveMenuItem = moveMenuItem;