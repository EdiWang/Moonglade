import { Alpine } from '/js/app/alpine-init.mjs';

function escapeHtml(str) {
    const div = document.createElement('div');
    div.appendChild(document.createTextNode(str));
    return div.innerHTML;
}

export function showConfirmModal(options) {
    Alpine.store('modal').show(options);
}

export function hideConfirmModal() {
    Alpine.store('modal').hide();
}

export { escapeHtml };
