import { Alpine } from './alpine-init.mjs';

function escapeHtml(str) {
    const div = document.createElement('div');
    div.appendChild(document.createTextNode(str));
    return div.innerHTML;
}

export function showConfirmModal(options) {
    Alpine.store('modal').show(options);
}

export function showDeleteConfirmModal(body, onConfirm) {
    showConfirmModal({
        title: 'Confirm Delete',
        body,
        confirmText: 'Delete',
        confirmClass: 'btn-outline-danger',
        confirmIcon: 'bi-trash',
        onConfirm
    });
}

export function hideConfirmModal() {
    Alpine.store('modal').hide();
}

export { escapeHtml };
