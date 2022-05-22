export let bsToast = new bootstrap.Toast(document.getElementById('liveToast'));
export let blogtoastMessage = document.querySelector('#blogtoast-message');

let lt = document.querySelector('#liveToast');

function removeToastBgColor() {
    const bgClasses = [
        'bg-success',
        'bg-warning',
        'bg-danger',
        'bg-info',
        'bg-primary',
        'bg-secondary'
    ];

    for (var i = 0; i < bgClasses.length; i++) {
        lt.classList.remove(bgClasses[i]);
    }
}

export function success(message) {
    removeToastBgColor();
    lt.classList.add('bg-success');
    blogtoastMessage.innerHTML = message;
    bsToast.show();
}

export function info(message) {
    removeToastBgColor();
    lt.classList.add('bg-info');
    blogtoastMessage.innerHTML = message;
    bsToast.show();
}

export function warning(message) {
    removeToastBgColor();
    lt.classList.add('bg-warning');
    blogtoastMessage.innerHTML = message;
    bsToast.show();
}

export function error(message) {
    removeToastBgColor();
    lt.classList.add('bg-danger');
    blogtoastMessage.innerHTML = message;
    bsToast.show();
}