import * as blogToast from '/js/app/blogtoast.module.mjs'
import { getPreferredTheme, setTheme } from '/js/app/themeService.mjs'

window.emptyGuid = '00000000-0000-0000-0000-000000000000';

window.blogToast = blogToast;
window.getPreferredTheme = getPreferredTheme;

setTheme(getPreferredTheme());