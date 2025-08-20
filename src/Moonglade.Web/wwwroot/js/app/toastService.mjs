const bgClasses = [
    'bg-success',
    'bg-warning',
    'bg-danger',
    'bg-info',
    'bg-primary',
    'bg-secondary'
];

// Cache DOM elements
const liveToast = document.getElementById('liveToast');
const blogtoastMessage = document.getElementById('blogtoast-message');
const bsToast = liveToast ? new bootstrap.Toast(liveToast) : null;

function removeToastBgColor(toastElement) {
    bgClasses.forEach(bgClass => toastElement.classList.remove(bgClass));
}

function showToast(message, bgClass) {
    if (!liveToast || !blogtoastMessage || !bsToast) return;

    // Validate bgClass
    if (!bgClasses.includes(bgClass)) bgClass = 'bg-info';

    removeToastBgColor(liveToast);
    liveToast.classList.add(bgClass);
    blogtoastMessage.textContent = message;
    bsToast.show();
}

export function success(message) {
    showToast(message, 'bg-success');
}

export function info(message) {
    showToast(message, 'bg-info');
}

export function warning(message) {
    showToast(message, 'bg-warning');
}

export function error(message) {
    showToast(message, 'bg-danger');
}
