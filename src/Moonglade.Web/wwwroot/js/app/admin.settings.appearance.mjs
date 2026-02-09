import { fetch2 } from './httpService.mjs?v=1500'
import { handleSettingsSubmit } from './admin.settings.mjs';
import { success, error } from './toastService.mjs';

async function handleSubmit(event) {
    assignEditorValues(cssContentEditor, "#ViewModel_CssCode");
    await handleSettingsSubmit(event);
}

var themeModal = new bootstrap.Modal(document.getElementById('thememodal'));

window.createTheme = async function (oFormElement) {
    try {
        await fetch2(oFormElement.action, 'POST',
            {
                name: document.querySelector('#Name').value,
                accentColor: document.querySelector('#AccentColor').value
            });

        themeModal.hide();
        window.location.reload();
    } catch (err) {
        error(err);
    }
}

window.deleteTheme = async function (id) {
    try {
        await fetch2(`/api/theme/${id}`, 'DELETE', {});

        var col = document.getElementById(`user-theme-col-${id}`);
        col.remove();

        success('Theme deleted.');
    } catch (err) {
        error(err);
    }
}

let cssContentEditor = null;

require(['vs/editor/editor.main'], function () {
    cssContentEditor = initEditor('CssContentEditor', "#ViewModel_CssCode", 'css');
});

const form = document.querySelector('#form-settings');
form.addEventListener('submit', handleSubmit);