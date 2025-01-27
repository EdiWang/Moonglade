const getStoredTheme = () => localStorage.getItem('theme');
const setStoredTheme = (theme) => localStorage.setItem('theme', theme);
const getSystemTheme = () => window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';

export const getPreferredTheme = () => getStoredTheme() || 'auto';

const applyTheme = (theme) => {
    const rootElement = document.documentElement;
    const finalTheme = theme === 'auto' ? getSystemTheme() : theme;
    rootElement.setAttribute('data-bs-theme', finalTheme);
};

export function setTheme(theme) {
    applyTheme(theme);
    setStoredTheme(theme);
}

const handleThemeSelection = (event) => {
    const theme = event.target.getAttribute('data-theme');
    if (theme) {
        setTheme(theme);
    }
};

const initializeTheme = () => {
    applyTheme(getPreferredTheme());

    const themeLinks = document.querySelectorAll('.dropdown-item[data-theme]');
    themeLinks.forEach((link) => link.addEventListener('click', handleThemeSelection));

    window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', () => {
        if (getPreferredTheme() === 'auto') {
            applyTheme('auto');
        }
    });
};

document.addEventListener('DOMContentLoaded', initializeTheme);
