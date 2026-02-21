import { getLocalizedString } from './utils.module.mjs';
import { showConfirmModal, hideConfirmModal } from './adminModal.mjs';

export function createButtonMixin() {
    return {
        buttonDialog: {
            isNew: true,
            index: -1,
            data: {
                text: '',
                url: '',
                cssClass: 'btn-outline-primary',
                openInNewTab: true
            }
        },

        addNewButton() {
            if (this.formData.buttons.length >= 3) {
                alert(getLocalizedString('maxButtons'));
                return;
            }
            this.buttonDialog = {
                isNew: true,
                index: -1,
                data: {
                    text: '',
                    url: '',
                    cssClass: 'btn-outline-primary',
                    openInNewTab: true
                }
            };
            this.buttonModal.show();
        },

        editButton(index) {
            this.buttonDialog = {
                isNew: false,
                index: index,
                data: { ...this.formData.buttons[index] }
            };
            this.buttonModal.show();
        },

        removeButton(index) {
            showConfirmModal({
                title: 'Remove Button',
                body: getLocalizedString('removeButton'),
                confirmText: 'Remove',
                confirmClass: 'btn-outline-danger',
                confirmIcon: 'bi-trash',
                onConfirm: () => {
                    this.formData.buttons.splice(index, 1);
                    hideConfirmModal();
                }
            });
        },

        saveButtonDialog() {
            if (!this.buttonDialog.data.text || !this.buttonDialog.data.url) {
                alert(getLocalizedString('textUrlRequired'));
                return;
            }

            if (this.buttonDialog.isNew) {
                this.formData.buttons.push({ ...this.buttonDialog.data });
            } else {
                this.formData.buttons[this.buttonDialog.index] = { ...this.buttonDialog.data };
            }

            this.buttonModal.hide();
        }
    };
}
