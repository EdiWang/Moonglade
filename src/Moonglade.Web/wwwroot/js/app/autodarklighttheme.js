'use strict';
if (window.matchMedia && window.themeModeSwitcher && supportLightSwitch) {
    if (window.matchMedia('(prefers-color-scheme: dark)').matches) {
        themeModeSwitcher.useDarkMode();
    }
    else {
        themeModeSwitcher.useLightMode();
    }
}