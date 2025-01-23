import { callApi } from './httpService.mjs'
import { handleSettingsSubmit } from './admin.settings.mjs';
import { success } from './toastService.mjs';

function handleSubmit(event) {
    assignEditorValues(cssContentEditor, "#ViewModel_CssCode");
    handleSettingsSubmit(event);
}

var themeModal = new bootstrap.Modal(document.getElementById('thememodal'));

window.createTheme = function (oFormElement) {
    callApi(oFormElement.action,
        'POST',
        {
            name: document.querySelector('#Name').value,
            accentColor: document.querySelector('#AccentColor').value
        },
        async (resp) => {
            themeModal.hide();
            window.location.reload();
        });
}

window.deleteTheme = function (id) {
    callApi(`/api/theme/${id}`,
        'DELETE',
        {},
        (resp) => {
            var col = document.getElementById(`user-theme-col-${id}`);
            col.remove();

            success('Theme deleted.');
        });
}

let cssContentEditor = null;

require(['vs/editor/editor.main'], function () {
    cssContentEditor = initEditor('CssContentEditor', "#ViewModel_CssCode", 'css');
});

const form = document.querySelector('#form-settings');
form.addEventListener('submit', handleSubmit);