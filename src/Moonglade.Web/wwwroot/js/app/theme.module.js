let isDarkMode = false;

export function useDarkMode() {
    $('body').attr("data-bs-theme", "dark");
    $('.article-post-slug').removeClass('border');

    isDarkMode = true;
}

export function useLightMode() {
    $('body').removeAttr("data-bs-theme");
    $('.article-post-slug').addClass('border');

    isDarkMode = false;
}

export function toggleTheme() {
    if (isDarkMode) {
        useLightMode();
    } else {
        useDarkMode();
    }
}