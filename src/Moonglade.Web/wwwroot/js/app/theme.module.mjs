const getStoredTheme = () => localStorage.getItem('theme');
const setStoredTheme = theme => localStorage.setItem('theme', theme);

const getSystemTheme = () => window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';

export const getPreferredTheme = () => {
    const storedTheme = getStoredTheme();
    return storedTheme || getSystemTheme();
}

export const setTheme = theme => {
    const rootElement = document.documentElement;
    if (theme === 'auto') {
        const systemTheme = getSystemTheme();
        rootElement.setAttribute('data-bs-theme', systemTheme);
        localStorage.removeItem('theme');
    } else {
        rootElement.setAttribute('data-bs-theme', theme);
        setStoredTheme(theme);
    }
}

window.getPreferredTheme = getPreferredTheme;
window.setTheme = setTheme;
setTheme(getPreferredTheme());