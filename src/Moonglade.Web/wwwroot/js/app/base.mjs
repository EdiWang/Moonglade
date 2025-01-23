import { getPreferredTheme, setTheme } from './themeService.mjs'

window.emptyGuid = '00000000-0000-0000-0000-000000000000';
window.getPreferredTheme = getPreferredTheme;

setTheme(getPreferredTheme());