let modalInstance = null;
let currentCallback = null;

function getModalElement() {
    return document.getElementById('adminSharedModal');
}

function ensureModalInstance() {
    if (!modalInstance) {
        const el = getModalElement();
        if (!el) return null;
        modalInstance = new bootstrap.Modal(el);
    }
    return modalInstance;
}

export function showConfirmModal({ title, body, confirmText, confirmClass, confirmIcon, onConfirm }) {
    const el = getModalElement();
    if (!el) return;

    el.querySelector('#adminSharedModalLabel').textContent = title || 'Confirm';
    el.querySelector('.admin-modal-body-content').innerHTML = body || '';

    const confirmBtn = el.querySelector('.btn-admin-modal-confirm');
    confirmBtn.className = `btn ${confirmClass || 'btn-outline-danger'} btn-admin-modal-confirm`;
    confirmBtn.innerHTML = (confirmIcon ? `<i class="${confirmIcon} me-1"></i>` : '') + (confirmText || 'Confirm');

    currentCallback = onConfirm;

    ensureModalInstance()?.show();
}

export function hideConfirmModal() {
    modalInstance?.hide();
}

window._adminSharedModalConfirm = async function () {
    if (currentCallback) {
        await currentCallback();
    }
};
