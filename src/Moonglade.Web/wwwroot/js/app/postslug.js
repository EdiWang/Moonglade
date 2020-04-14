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
    useDarkMode: function () {
        $('#moonglade-nav').removeClass('bg-moonglade-accent1');
        $('#moonglade-nav, #moonglade-footer').addClass('bg-dark');
        $('.post-publish-info-mobile').removeClass('bg-light');
        $('.post-publish-info-mobile').addClass('bg-dark');

        $('#moonglade-footer').removeClass('bg-moonglade-accent2');
        $('').addClass('bg-dark');

        $('.post-content').addClass('darkmode');

        $('body').addClass('bg-moca-dark text-light');
        $('.card').addClass('text-white bg-dark');
        $('.list-group-item, .card-body').addClass('bg-moca-dark text-light');

        $('.post-content img.img-thumbnail').addClass('bg-dark border-secondary');
        $('.post-content table.table').addClass('table-dark');

        $('.comment-form-containter .form-control').addClass('bg-transparent');

        this.isDarkMode = true;
        $('.lightswitch').addClass('bg-dark text-light border-secondary');
        $('hr').addClass('hr-dark');
        $('#lighticon').removeClass('icon-sun-o');
        $('#lighticon').addClass('icon-moon-o');

        console.info('Switched to dark mode');
    },
    useLightMode: function () {
        $('#moonglade-nav').addClass('bg-moonglade-accent1');
        $('#moonglade-nav, #moonglade-footer').removeClass('bg-dark');
        $('.post-publish-info-mobile').removeClass('bg-dark');
        $('.post-publish-info-mobile').addClass('bg-light');

        $('#moonglade-footer').addClass('bg-moonglade-accent2');

        $('.post-content').removeClass('darkmode');

        $('body').removeClass('bg-moca-dark text-light');
        $('.card').removeClass('text-white bg-dark');
        $('.list-group-item, .card-body').removeClass('bg-moca-dark text-light');

        $('.post-content img.img-thumbnail').removeClass('bg-dark border-secondary');
        $('.post-content table.table').removeClass('table-dark');

        $('.comment-form-containter .form-control').removeClass('bg-transparent');

        this.isDarkMode = false;
        $('.lightswitch').removeClass('bg-dark text-light border-secondary');
        $('hr').removeClass('hr-dark');
        $('#lighticon').addClass('icon-sun-o');
        $('#lighticon').removeClass('icon-moon-o');
        console.info('Switched to light mode');
    }
};