import { default as Alpine } from '/lib/alpinejs/alpinejs.3.15.0.module.esm.min.js';
import { fetch2 } from '/js/app/httpService.mjs?v=1500';
import { success } from '/js/app/toastService.mjs';

let menuItemModal, subMenuItemModal;

Alpine.data('menuManager', () => ({
    settings: {
        isEnabled: false,
        menus: []
    },
    isLoading: true,
    currentMenuItem: {},
    currentSubMenuItem: {},
    editingIndex: null,
    editingSubIndex: null,
    editingParentIndex: null,

    async init() {
        this.initializeModals();
        await this.loadSettings();
    },

    initializeModals() {
        menuItemModal = new bootstrap.Modal(document.getElementById('menuItemModal'));
        subMenuItemModal = new bootstrap.Modal(document.getElementById('subMenuItemModal'));
    },

    async loadSettings() {
        this.isLoading = true;
        try {
            const data = await fetch2('/api/settings/custom-menu', 'GET');
            if (data) {
                this.settings = {
                    isEnabled: data.isEnabled || false,
                    menus: data.menus || []
                };
            }
        } finally {
            this.isLoading = false;
        }
    },

    async saveSettings() {
        const payload = {
            isEnabled: this.settings.isEnabled,
            menuJson: JSON.stringify(this.settings.menus)
        };

        try {
            await fetch2('/api/settings/custom-menu', 'POST', payload);
            success('Settings saved successfully');
        } catch (error) {
            console.error('Error saving settings:', error);
        }
    },

    openMenuItemModal(index = null) {
        this.editingIndex = index;
        
        if (index !== null && this.settings.menus[index]) {
            // Edit mode
            this.currentMenuItem = { ...this.settings.menus[index] };
        } else {
            // Add mode
            this.currentMenuItem = {
                title: '',
                url: '',
                icon: 'bi-star',
                displayOrder: this.settings.menus.length + 1,
                isOpenInNewTab: false,
                subMenus: []
            };
        }

        menuItemModal.show();
    },

    saveMenuItem() {
        if (!this.currentMenuItem.title?.trim()) {
            alert('Title is required');
            return;
        }

        const menuItem = {
            title: this.currentMenuItem.title.trim(),
            url: this.currentMenuItem.url?.trim() || '',
            icon: this.currentMenuItem.icon?.trim() || 'bi-star',
            displayOrder: this.currentMenuItem.displayOrder || 1,
            isOpenInNewTab: this.currentMenuItem.isOpenInNewTab || false,
            subMenus: this.editingIndex !== null ? this.settings.menus[this.editingIndex].subMenus : []
        };

        if (this.editingIndex !== null) {
            this.settings.menus[this.editingIndex] = menuItem;
        } else {
            this.settings.menus.push(menuItem);
        }

        menuItemModal.hide();
        this.currentMenuItem = {};
        this.editingIndex = null;
    },

    openSubMenuItemModal(parentIndex, subIndex = null) {
        this.editingParentIndex = parentIndex;
        this.editingSubIndex = subIndex;

        if (subIndex !== null && this.settings.menus[parentIndex]?.subMenus[subIndex]) {
            // Edit mode
            this.currentSubMenuItem = { ...this.settings.menus[parentIndex].subMenus[subIndex] };
        } else {
            // Add mode
            this.currentSubMenuItem = {
                title: '',
                url: '',
                isOpenInNewTab: false
            };
        }

        subMenuItemModal.show();
    },

    saveSubMenuItem() {
        if (!this.currentSubMenuItem.title?.trim() || !this.currentSubMenuItem.url?.trim()) {
            alert('Title and URL are required');
            return;
        }

        const subMenuItem = {
            title: this.currentSubMenuItem.title.trim(),
            url: this.currentSubMenuItem.url.trim(),
            isOpenInNewTab: this.currentSubMenuItem.isOpenInNewTab || false
        };

        if (!this.settings.menus[this.editingParentIndex].subMenus) {
            this.settings.menus[this.editingParentIndex].subMenus = [];
        }

        if (this.editingSubIndex !== null) {
            this.settings.menus[this.editingParentIndex].subMenus[this.editingSubIndex] = subMenuItem;
        } else {
            this.settings.menus[this.editingParentIndex].subMenus.push(subMenuItem);
        }

        subMenuItemModal.hide();
        this.currentSubMenuItem = {};
        this.editingSubIndex = null;
        this.editingParentIndex = null;
    },

    deleteMenuItem(index) {
        if (!confirm('Are you sure you want to delete this menu item?')) return;

        this.settings.menus.splice(index, 1);
    },

    deleteSubMenuItem(parentIndex, subIndex) {
        if (!confirm('Are you sure you want to delete this sub menu item?')) return;

        this.settings.menus[parentIndex].subMenus.splice(subIndex, 1);
    },

    moveMenuItem(index, direction) {
        const newIndex = direction === 'up' ? index - 1 : index + 1;
        if (newIndex >= 0 && newIndex < this.settings.menus.length) {
            const temp = this.settings.menus[index];
            this.settings.menus[index] = this.settings.menus[newIndex];
            this.settings.menus[newIndex] = temp;

            // Update display orders
            this.settings.menus[index].displayOrder = index + 1;
            this.settings.menus[newIndex].displayOrder = newIndex + 1;
        }
    },

    clearMenus() {
        if (!confirm('Are you sure you want to clear all menu items?')) return;

        this.settings.menus = [];
    }
}));

Alpine.start();