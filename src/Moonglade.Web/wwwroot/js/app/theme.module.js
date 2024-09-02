let isDarkMode = false;

export function useDarkMode() {
    $('body').attr("data-bs-theme", "dark");
    $('body.body-post-slug').removeClass('bg-gray-1');
    $('.article-post-slug').removeClass('border');

    $('#aside-tags .btn-accent').removeClass('btn-accent').addClass('btn-dark');
    $('.post-summary-tags .btn-accent').removeClass('btn-accent').addClass('btn-dark');

    $('.aside-widget').removeClass('bg-white').addClass('border-dark');

    $('.comment-item').removeClass('bg-white').addClass('bg-dark');
    $('.comment-item .card-subtitle').removeClass('text-body-secondary').addClass('text-body-dark');

    isDarkMode = true;
    $('.lightswitch').addClass('bg-dark text-light border-secondary');
    document.querySelector('#lighticon').classList.remove('bi-brightness-high');
    document.querySelector('#lighticon').classList.add('bi-moon');
}

export function useLightMode() {
    $('body').removeAttr("data-bs-theme");
    $('body.body-post-slug').addClass('bg-gray-1');
    $('.article-post-slug').addClass('border');

    $('#aside-tags .btn-dark').removeClass('btn-dark').addClass('btn-accent');
    $('.post-summary-tags .btn-dark').removeClass('btn-dark').addClass('btn-accent');

    $('.aside-widget').addClass('bg-white').removeClass('border-dark');

    $('.comment-item').removeClass('bg-dark').addClass('bg-white');
    $('.comment-item .card-subtitle').removeClass('text-body-dark').addClass('text-body-secondary');

    isDarkMode = false;
    $('.lightswitch').removeClass('bg-dark text-light border-secondary');
    document.querySelector('#lighticon').classList.add('bi-brightness-high');
    document.querySelector('#lighticon').classList.remove('bi-moon');
}

export function toggleTheme() {
    if (isDarkMode) {
        useLightMode();
    } else {
        useDarkMode();
    }
}