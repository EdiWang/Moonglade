export function resizeImages(selector) {
    const images = document.querySelectorAll(selector);
    images.forEach(img => {
        img.removeAttribute('height');
        img.removeAttribute('width');
        img.classList.add('img-fluid', 'img-thumbnail');
    });
}

export function applyImageZooming(selector) {
    document.querySelectorAll(selector).forEach(function (img) {
        img.addEventListener('click', function (e) {
            var src = img.getAttribute('src');
            document.querySelector('#imgzoom').src = src;

            var imgzoomModal = new bootstrap.Modal(document.querySelector('#imgzoomModal'));
            imgzoomModal.show();
        });
    });
}