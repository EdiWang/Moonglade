var themeModeSwitcher = {
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

        isDarkMode = true;
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

        isDarkMode = false;
        $('.lightswitch').removeClass('bg-dark text-light border-secondary');
        $('hr').removeClass('hr-dark');
        $('#lighticon').addClass('icon-sun-o');
        $('#lighticon').removeClass('icon-moon-o');
        console.info('Switched to light mode');
    }
}