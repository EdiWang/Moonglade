var postSlug = {
    registerRatingButtons: function (pid) {
        $('.btn-ratings').click(function () {
            ajaxPostWithCSRFToken('/post/like', { postId: pid }, function (data) {
                if (data.isSuccess) {
                    var oldVal = parseInt($('.likehits-num').text(), 10);
                    $('.likehits-num').html(++oldVal);
                    $('.btn-ratings').attr('disabled', 'disabled');
                } else {
                    window.toastr.warning(data.message);
                }
            });
        });
    },
    resetCaptchaImage: function () {
        d = new Date();
        $('#img-captcha').attr('src', `/get-captcha-image?${d.getTime()}`);
    },
    resizeImages: function () {
        $('.post-content img').removeAttr('height');
        $('.post-content img').removeAttr('width');
        $('.post-content img').addClass('img-fluid img-thumbnail');
    },
    applyImageZooming: function () {
        if (getResponsiveBreakpoint() !== 'xs') {
            $('.post-content img').click(function (e) {
                var src = $(this).attr('src');
                $('#imgzoom').attr('src', src);
                $('#imgzoomModal').modal();
            });
        }
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

        $('.post-content a:external').addClass('external');

        $('a.external').click(function (e) {
            e.preventDefault();
            var linkHref = $(this).attr('href');
            $('#extlink-url').html(linkHref);
            $('#extlink-continue').attr('href', linkHref);
            $('#externalLinkModal').modal('show');
        });

        $('#extlink-continue').click(function () {
            $('#externalLinkModal').modal('hide');
        });
    }
};