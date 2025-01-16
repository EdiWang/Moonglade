const bgClasses = [
    'bg-success',
    'bg-warning',
    'bg-danger',
    'bg-info',
    'bg-primary',
    'bg-secondary'
];

function getToastElements() {
    return {
        bsToast: new bootstrap.Toast(document.getElementById('liveToast')),
        blogtoastMessage: document.querySelector('#blogtoast-message'),
        liveToast: document.querySelector('#liveToast')
    };
}

function removeToastBgColor(toastElement) {
    bgClasses.forEach(bgClass => toastElement.classList.remove(bgClass));
}

function showToast(message, bgClass) {
    const { bsToast, blogtoastMessage, liveToast } = getToastElements();

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
