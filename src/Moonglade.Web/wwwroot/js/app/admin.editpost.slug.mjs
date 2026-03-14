import { showConfirmModal, hideConfirmModal } from './adminModal.mjs';

function slugify(text) {
    if (!/^[A-Za-z][A-Za-z0-9 \(\)#,\.\?]*$/.test(text)) {
        return '';
    }
    return text
        .toLowerCase()
        .replace(/[()#,.?]/g, '')
        .replace(/[^\w ]+/g, '')
        .replace(/ +/g, '-');
}

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
                title: 'Modify Slug',
                body: '<div class="alert alert-warning">This post was published for a period of time, changing slug will result in breaking SEO, would you like to continue?</div>',
                confirmText: 'Modify',
                confirmClass: 'btn-warning',
                onConfirm: () => {
                    this.slugUnlocked = true;
                    hideConfirmModal();
                }
            });
        }
    };
}
