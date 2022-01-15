var themeModeSwitcher = {
    useDarkMode: function () {
        $('#blog-nav').removeClass('bg-accent1');
        $('#blog-nav, #moonglade-footer').addClass('bg-dark');
        $('.post-publish-info-mobile').removeClass('bg-light');
        $('.post-publish-info-mobile').addClass('bg-dark');

        document.querySelector('#blog-footer').classList.remove('bg-accent2');
        $('').addClass('bg-dark');

        $('body').addClass('bg-moca-dark text-light darkmode');
        $('body.body-post-slug').removeClass('bg-gray-1');
        $('.article-post-slug').removeClass('box border');

        $('.card').addClass('text-white bg-dark');
        $('.list-group-item, .card-body').addClass('bg-moca-dark text-light');

        $('.post-content img.img-thumbnail').addClass('bg-dark border-secondary');
        $('.post-content table.table').addClass('table-dark');

        $('.comment-form-containter .form-control, aside .form-control').addClass('bg-transparent');
        $('aside .btn-light').removeClass('btn-light').addClass('btn-dark');
        $('#aside-tags .btn-accent').removeClass('btn-accent').addClass('btn-dark');
        $('.post-summary-tags .btn-accent').removeClass('btn-accent').addClass('btn-dark');

        isDarkMode = true;
        $('.lightswitch').addClass('bg-dark text-light border-secondary');
        $('hr').addClass('hr-dark');
        document.querySelector('#lighticon').classList.remove('bi-brightness-high');
        document.querySelector('#lighticon').classList.add('bi-moon');
    },
    useLightMode: function () {
        $('#blog-nav').addClass('bg-accent1');
        $('#blog-nav, #moonglade-footer').removeClass('bg-dark');
        $('.post-publish-info-mobile').removeClass('bg-dark');
        $('.post-publish-info-mobile').addClass('bg-light');

        document.querySelector('#blog-footer').classList.add('bg-accent2');

        $('body').removeClass('bg-moca-dark text-light darkmode');
        $('body.body-post-slug').addClass('bg-gray-1');
        $('.article-post-slug').addClass('box border');
        $('.card').removeClass('text-white bg-dark');
        $('.list-group-item, .card-body').removeClass('bg-moca-dark text-light');

        $('.post-content img.img-thumbnail').removeClass('bg-dark border-secondary');
        $('.post-content table.table').removeClass('table-dark');

        $('.comment-form-containter .form-control, aside .form-control').removeClass('bg-transparent');
        $('aside .btn-light').removeClass('btn-dark').addClass('btn-light');
        $('#aside-tags .btn-dark').removeClass('btn-dark').addClass('btn-accent');
        $('.post-summary-tags .btn-dark').removeClass('btn-dark').addClass('btn-accent');

        isDarkMode = false;
        $('.lightswitch').removeClass('bg-dark text-light border-secondary');
        $('hr').removeClass('hr-dark');
        document.querySelector('#lighticon').classList.add('bi-brightness-high');
        document.querySelector('#lighticon').classList.remove('bi-moon');
    }
}