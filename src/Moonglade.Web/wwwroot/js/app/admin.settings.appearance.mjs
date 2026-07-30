import { fetch2 } from './httpService.mjs?v=1500'
import { handleSettingsSubmit } from './admin.settings.mjs';
import { success, error } from './toastService.mjs';
import { getLocalizedString } from './utils.module.mjs';
import { createMoongladeEditor } from '/lib/moonglade-editor/moonglade-editor.js';

async function handleSubmit(event) {
    cssContentEditor.syncToTextarea();
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

        success(getLocalizedString('themeDeleted'));
    } catch (err) {
        error(err);
    }
}

let cssContentEditor = null;

cssContentEditor = createMoongladeEditor({
    mode: 'css',
    element: document.querySelector('#CssContentEditor'),
    textarea: document.querySelector('#ViewModel_CssCode'),
    height: 'calc(100vh - 450px)',
    lineWrapping: true,
    tabSize: 2
});

const form = document.querySelector('#form-settings');
form.addEventListener('submit', handleSubmit);
