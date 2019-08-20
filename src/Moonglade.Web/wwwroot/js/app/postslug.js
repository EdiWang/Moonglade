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
    }
};