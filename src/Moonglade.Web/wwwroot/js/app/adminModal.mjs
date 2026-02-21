import { Alpine } from '/js/app/alpine-init.mjs';

function escapeHtml(str) {
    const div = document.createElement('div');
    div.appendChild(document.createTextNode(str));
    return div.innerHTML;
}

Alpine.store('modal', {
    open: false,
    title: '',
    body: '',
    confirmText: 'Confirm',
    confirmClass: 'btn-outline-danger',
    confirmIcon: '',
    _onConfirm: null,
    _bsModal: null,

    _ensureBsModal() {
        if (!this._bsModal) {
            const el = document.getElementById('adminSharedModal');
            if (el) this._bsModal = new bootstrap.Modal(el);
        }
        return this._bsModal;
    },

    show({ title, body, confirmText, confirmClass, confirmIcon, onConfirm }) {
        this.title = title || 'Confirm';
        this.body = body || '';
        this.confirmText = confirmText || 'Confirm';
        this.confirmClass = confirmClass || 'btn-outline-danger';
        this.confirmIcon = confirmIcon || '';
        this._onConfirm = onConfirm;
        this.open = true;
        this._ensureBsModal()?.show();
    },

    async confirm() {
        if (this._onConfirm) {
            await this._onConfirm();
        }
    },

    hide() {
        this.open = false;
        this._bsModal?.hide();
    }
});

export function showConfirmModal(options) {
    Alpine.store('modal').show(options);
}

export function hideConfirmModal() {
    Alpine.store('modal').hide();
}

export { escapeHtml };
