let isDarkMode = false;

export function useDarkMode() {
    $('body').attr("data-bs-theme", "dark");
    $('.article-post-slug').removeClass('border');

    isDarkMode = true;
    document.querySelector('#lighticon').classList.remove('bi-brightness-high');
    document.querySelector('#lighticon').classList.add('bi-moon');
}

export function useLightMode() {
    $('body').removeAttr("data-bs-theme");
    $('.article-post-slug').addClass('border');

    isDarkMode = false;
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