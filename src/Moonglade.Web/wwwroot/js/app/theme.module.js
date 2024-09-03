let isDarkMode = false;

const getStoredTheme = () => localStorage.getItem('theme')
const setStoredTheme = theme => localStorage.setItem('theme', theme)

export const getPreferredTheme = () => {
    const storedTheme = getStoredTheme()
    if (storedTheme) {
        return storedTheme
    }

    return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light'
}

export const setTheme = theme => {
    if (theme === 'auto') {
        document.documentElement.setAttribute('data-bs-theme', (window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light'))
    } else {
        document.documentElement.setAttribute('data-bs-theme', theme)
    }
}

export function useDarkMode() {
    setTheme('dark');
    setStoredTheme('dark');
    $('.article-post-slug').removeClass('border');

    isDarkMode = true;
}

export function useLightMode() {
    setTheme('light');
    setStoredTheme('light');
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