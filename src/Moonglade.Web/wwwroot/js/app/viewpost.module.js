export function resizeImages() {
    $('.post-content img').removeAttr('height');
    $('.post-content img').removeAttr('width');
    $('.post-content img').addClass('img-fluid img-thumbnail');
}

export function renderCodeHighlighter() {
    $('pre').each(function (i, pre) {
        // Find <pre> that doesn't have a <code> inside it.
        if ($(pre).find('code')[0] === undefined) {
            $(pre).wrapInner('<code></code>');
        }

        // For code that can't be automatically detected, fall back to use XML
        if ($(pre).hasClass('language-markup')) {
            $(pre).children('code').addClass('lang-xml');
        }
    });

    $('pre code').each(function (i, block) {
        hljs.highlightElement(block);
    });
}

export function warnExtLink() {
    $.expr[':'].external = function (obj) {
        return !obj.href.match(/^mailto\\:/) && (obj.hostname != location.hostname);
    };

    var externalLinkModal = new bootstrap.Modal('#externalLinkModal');

    $('.post-content a:external').addClass('external');

    $('a.external').click(function (e) {
        e.preventDefault();
        var linkHref = $(this).attr('href');
        $('#extlink-url').html(linkHref);
        document.querySelector('#extlink-continue').href = linkHref;
        externalLinkModal.show();
    });

    $('#extlink-continue').click(function () {
        externalLinkModal.hide();
    });
}

export function getImageWidthInDevicePixelRatio(width) {
    if (width <= 0) return 0;
    var dpr = window.devicePixelRatio;
    if (dpr === 1) return width;
    return width / dpr;
}

export function applyImageZooming() {
    $('.post-content img').click(function (e) {
        var src = $(this).attr('src');

        document.querySelector('#imgzoom').src = src;

        if (window.fitImageToDevicePixelRatio) {
            setTimeout(function () {
                var w = $('#imgzoom')[0].naturalWidth;

                $('#imgzoom').css('width', getImageWidthInDevicePixelRatio(w));
            }, 100);
        }

        var imgzoomModal = new bootstrap.Modal('#imgzoomModal');
        imgzoomModal.show();
    });
}

export function resetCaptchaImage() {
    const d = new Date();
    document.querySelector('#img-captcha').src = `/captcha-image?${d.getTime()}`;
}

var btnSubmitComment = '#btn-submit-comment';

export function submitComment(pid) {
    document.querySelector('#thx-for-comment').style.display = 'none';
    document.querySelector('#thx-for-comment-non-review').style.display = 'none';

    document.querySelector('#loadingIndicator').style.display = 'block';
    document.querySelector(btnSubmitComment).classList.add('disabled');
    document.querySelector(btnSubmitComment).setAttribute('disabled', 'disabled');

    callApi(`/api/comment/${pid}`, 'POST',
        {
            username: document.querySelector('#input-comment-name').value,
            content: document.querySelector('#input-comment-content').value,
            email: document.querySelector('#input-comment-email').value,
            captchaCode: document.querySelector('#input-comment-captcha').value
        },
        (success) => {
            document.querySelector('#comment-form').reset();
            resetCaptchaImage();

            var httpCode = success.status;
            if (httpCode === 201) {
                document.querySelector('#thx-for-comment').style.display = 'block';
            }
            if (httpCode === 200) {
                document.querySelector('#thx-for-comment-non-review').style.display = 'block';
            }
        },
        (always) => {
            document.querySelector('#loadingIndicator').style.display = 'none';
            document.querySelector(btnSubmitComment).classList.remove('disabled');
            document.querySelector(btnSubmitComment).removeAttribute('disabled');
        });
}