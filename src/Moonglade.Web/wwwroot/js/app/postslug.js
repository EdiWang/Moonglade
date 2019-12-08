var postSlug = {
    isDarkMode: false,
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
    },
    useDarkMode: function () {
        $('#moonglade-nav').removeClass('bg-moonglade-accent1');
        $('#moonglade-nav').addClass('bg-dark');

        $('#moonglade-footer').removeClass('bg-moonglade-accent2');
        $('#moonglade-footer').addClass('bg-dark');

        $('.post-content').addClass('darkmode');

        $('body').addClass('bg-moca-dark text-light');
        $('.card').addClass('text-white bg-dark');
        $('.list-group-item, .card-body').addClass('bg-moca-dark text-light');

        $('.post-content img.img-thumbnail').addClass('bg-dark border-secondary');
        $('.post-content table.table').addClass('table-dark');

        $('#css-simplemde').attr('href', '/css/theme/simplemde-theme-dark.min.css');

        $('.comment-form-containter .form-control').addClass('bg-transparent');

        this.isDarkMode = true;
        $('.lightswitch').addClass('bg-dark text-light border-secondary');
        $('#lighticon').removeClass('icon-sun-o');
        $('#lighticon').addClass('icon-moon-o');
        console.info('Switched to dark mode');
    },
    useLightMode: function () {
        $('#moonglade-nav').addClass('bg-moonglade-accent1');
        $('#moonglade-nav').removeClass('bg-dark');

        $('#moonglade-footer').addClass('bg-moonglade-accent2');
        $('#moonglade-footer').removeClass('bg-dark');

        $('.post-content').removeClass('darkmode');

        $('body').removeClass('bg-moca-dark text-light');
        $('.card').removeClass('text-white bg-dark');
        $('.list-group-item, .card-body').removeClass('bg-moca-dark text-light');

        $('.post-content img.img-thumbnail').removeClass('bg-dark border-secondary');
        $('.post-content table.table').removeClass('table-dark');

        $('#css-simplemde').attr('href', '/lib/simplemde/simplemde.min.css');

        $('.comment-form-containter .form-control').removeClass('bg-transparent');

        this.isDarkMode = false;
        $('.lightswitch').removeClass('bg-dark text-light border-secondary');
        $('#lighticon').addClass('icon-sun-o');
        $('#lighticon').removeClass('icon-moon-o');
        console.info('Switched to light mode');
    }
};