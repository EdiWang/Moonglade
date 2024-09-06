export let bsToast = new bootstrap.Toast(document.getElementById('liveToast'));
export let blogtoastMessage = document.querySelector('#blogtoast-message');

let lt = document.querySelector('#liveToast');

const bgClasses = [
    'bg-success',
    'bg-warning',
    'bg-danger',
    'bg-info',
    'bg-primary',
    'bg-secondary'
];

function removeToastBgColor() {
    bgClasses.forEach(bgClass => lt.classList.remove(bgClass));
}

function showToast(message, bgClass) {
    removeToastBgColor();
    lt.classList.add(bgClass);
    blogtoastMessage.innerHTML = message;
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
