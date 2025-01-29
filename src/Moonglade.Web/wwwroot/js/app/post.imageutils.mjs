import { parseMetaContent } from './utils.module.mjs';

function getImageWidthInDevicePixelRatio(width) {
    if (width <= 0) return 0;
    var dpr = window.devicePixelRatio;
    if (dpr === 1) return width;
    return width / dpr;
}

export function resizeImages(selector) {
    const images = document.querySelectorAll(selector);
    images.forEach(img => {
        img.removeAttribute('height');
        img.removeAttribute('width');
        img.classList.add('img-fluid', 'img-thumbnail');
    });
}

export function applyImageZooming(selector) {
    const fitImageToDevicePixelRatio = parseMetaContent("image-device-dpi");

    document.querySelectorAll(selector).forEach(function (img) {
        img.addEventListener('click', function (e) {
            var src = img.getAttribute('src');
            document.querySelector('#imgzoom').src = src;

            if (fitImageToDevicePixelRatio) {
                setTimeout(function () {
                    var w = document.querySelector('#imgzoom').naturalWidth;
                    document.querySelector('#imgzoom').style.width = getImageWidthInDevicePixelRatio(w) + 'px';
                }, 100);
            }

            var imgzoomModal = new bootstrap.Modal(document.querySelector('#imgzoomModal'));
            imgzoomModal.show();
        });
    });
}