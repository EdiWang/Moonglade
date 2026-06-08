import { showConfirmModal, hideConfirmModal } from './adminModal.mjs';
import { getLocalizedString, slugify } from './utils.module.mjs';

export function createSlugMixin() {
    return {
        warnSlugModification: false,
        slugUnlocked: false,

        onTitleChange() {
            if (!this.warnSlugModification || this.slugUnlocked) {
                const newSlug = slugify(this.formData.title);
                if (newSlug) {
                    this.formData.slug = newSlug;
                }
            }
        },

        unlockSlug() {
            showConfirmModal({
                title: getLocalizedString('modifySlug'),
                body: `<div class="alert alert-warning">${getLocalizedString('modifySlugWarning')}</div>`,
                confirmText: getLocalizedString('modify'),
                confirmClass: 'btn-warning',
                onConfirm: () => {
                    this.slugUnlocked = true;
                    hideConfirmModal();
                }
            });
        }
    };
}
