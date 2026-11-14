import { fetch2 } from './httpService.mjs?v=1500'
import { initializeSettings } from './admin.settings.mjs';
import { success, error } from './toastService.mjs';
import { getLocalizedString } from './utils.module.mjs';
import { createMoongladeCodeEditor } from '/_content/Moonglade.Editor.StaticAssets/moonglade-editor/moonglade-editor.code.js';

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

initializeSettings({
    createEditor(name, textarea) {
        if (name !== 'CssCode') return;

        const element = document.createElement('div');
        textarea.classList.add('d-none');
        textarea.before(element);
        const editor = createMoongladeCodeEditor({
            language: 'css',
            element,
            textarea,
            height: 'min(50vh, 24rem)',
            lineWrapping: true,
            tabSize: 2
        });
        const editable = element.querySelector('.cm-content');
        editable.setAttribute('aria-labelledby', textarea.getAttribute('aria-labelledby'));
        editable.setAttribute('aria-describedby', textarea.getAttribute('aria-describedby'));
        return editor;
    },
    onSaved(fields) {
        if (!('ThemeId' in fields)) return;
        for (const theme of document.querySelectorAll('.theme-selector-container')) {
            const active = Number(theme.dataset.themeId) === fields.ThemeId;
            theme.classList.toggle('active', active);
            theme.querySelector('.theme-selector-footer').classList.toggle('active', active);
        }
    }
});
