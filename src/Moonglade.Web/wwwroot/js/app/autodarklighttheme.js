'use strict';
if (window.matchMedia) {
    if (window.matchMedia('(prefers-color-scheme: dark)').matches) {
        console.debug('Using Dark Mode');
        themeModeSwitcher.useDarkMode();
    }
    else {
        console.debug('Using Light Mode');
        themeModeSwitcher.useLightMode();
    }
}