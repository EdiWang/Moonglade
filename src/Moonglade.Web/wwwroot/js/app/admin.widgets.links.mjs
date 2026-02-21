import { getLocalizedString } from './utils.module.mjs';
import { showConfirmModal, hideConfirmModal } from './adminModal.mjs';

export function createLinkMixin() {
    return {
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
        }
    };
}
