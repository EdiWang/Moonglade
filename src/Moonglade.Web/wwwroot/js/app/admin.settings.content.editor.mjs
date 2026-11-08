import { initializeSettings } from './admin.settings.mjs';
import { createMoongladeCodeEditor } from '/_content/Moonglade.Editor.StaticAssets/moonglade-editor/moonglade-editor.code.js';

initializeSettings({
    createEditor(name, textarea) {
        if (name !== 'PostFooterHtmlPitch' && name !== 'CalloutSectionHtmlPitch') return;

        const element = document.createElement('div');
        textarea.classList.add('d-none');
        textarea.before(element);
        const editor = createMoongladeCodeEditor({
            language: 'html',
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
    }
});
