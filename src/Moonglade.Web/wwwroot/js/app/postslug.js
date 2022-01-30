var hasLiked = false;
var postSlug = {
    getStatistics: function (pid) {
        const uri = `/api/statistics/${pid}`;
        fetch(uri)
            .then(response => response.json())
            .then(data => {
                document.querySelector('.post-hit-number-text').innerText = data.hits;
                var likehitsNum = document.querySelector('.likehits-num');
                if (likehitsNum) {
                    likehitsNum.innerText = data.likes;
                }
            })
            .catch(err => {
                blogToast.error(err);
                console.error(err);
            });
    },
    postStatistics: function (pid, isLike) {
        const req = {
            postId: pid,
            isLike: isLike
        };

        callApi('/api/statistics', 'POST', req,
            (success) => {
                if (isLike) {
                    let oldVal = parseInt(document.querySelector('.likehits-num').innerText, 10);
                    document.querySelector('.likehits-num').innerHTML = ++oldVal;
                    document.querySelector('.btn-ratings').setAttribute('disabled', 'disabled');
                }
            });
    },
    registerRatingButtons: function (pid) {
        $('.btn-ratings').click(function () {
            if (!hasLiked) {
                postSlug.postStatistics(pid, true);
                hasLiked = true;
            }
        });
    },
    resetCaptchaImage: function () {
        d = new Date();
        document.querySelector('#img-captcha').src = `/captcha-image?${d.getTime()}`;
    },
    resizeImages: function () {
        $('.post-content img').removeAttr('height');
        $('.post-content img').removeAttr('width');
        $('.post-content img').addClass('img-fluid img-thumbnail');
    },
    applyImageZooming: function () {
        $('.post-content img').click(function (e) {
            var src = $(this).attr('src');

            document.querySelector('#imgzoom').src = src;

            if (fitImageToDevicePixelRatio) {
                setTimeout(function () {
                    var w = $('#imgzoom')[0].naturalWidth;

                    $('#imgzoom').css('width', getImageWidthInDevicePixelRatio(w));
                }, 100);
            }

            var imgzoomModal = new bootstrap.Modal('#imgzoomModal');
            imgzoomModal.show();
        });
    },
    renderCodeHighlighter: function () {
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
            hljs.highlightBlock(block);
        });
    },
    warnExtLink: function () {
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
};

function getImageWidthInDevicePixelRatio(width) {
    if (width <= 0) return 0;
    var dpr = window.devicePixelRatio;
    if (dpr === 1) return width;
    return width / dpr;
}