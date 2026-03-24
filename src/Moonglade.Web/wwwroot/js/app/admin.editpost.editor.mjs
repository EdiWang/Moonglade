import { loadTinyMCE } from './admin.editor.module.mjs';

function loadScript(src) {
    return new Promise((resolve, reject) => {
        if (document.querySelector(`script[src="${src}"]`)) {
            resolve();
            return;
        }
        const script = document.createElement('script');
        script.src = src;
        script.onload = resolve;
        script.onerror = reject;
        document.head.appendChild(script);
    });
}

let monacoReady = false;

async function ensureTinyMCE() {
    if (window.tinyMCE) return;
    await loadScript('/lib/tinymce/tinymce.min.js');
}

async function ensureMonaco() {
    if (monacoReady) return;

    await loadScript('/lib/moonglade-monaco/min/vs/loader.js');

    require.config({ paths: { 'vs': '/lib/moonglade-monaco/min/vs' } });

    if (!window.initEditor) {
        window.initEditor = function (containerId, textAreaQuerySelector, lang) {
            var editorDiv = document.getElementById(containerId);

            if (window.getPreferredTheme && window.getPreferredTheme() === 'dark') {
                monaco.editor.setTheme('vs-dark');
            }

            var editorInstance = monaco.editor.create(editorDiv, { language: lang });
            editorInstance.layout();

            var editorValue = document.querySelector(textAreaQuerySelector).value;
            editorInstance.setValue(editorValue);

            return editorInstance;
        };
    }

    await loadScript('/js/3rd/inline-attachment.js');
    await loadScript('/js/3rd/monaco.inline-attachment.js');
    monacoReady = true;
}

export function createEditorMixin() {
    return {
        editorChoice: '',
        _editorInitialized: false,

        async initEditor() {
            if (this.formData.contentType === 'html') {
                await ensureTinyMCE();
                loadTinyMCE('.post-content-textarea');
            }

            if (this.formData.contentType === 'markdown') {
                await ensureMonaco();

                await new Promise((resolve) => {
                    require(['vs/editor/editor.main'], () => {
                        window.mdContentEditor = window.initEditor('markdown-content-editor', '.post-content-textarea', 'markdown');

                        if (this.formData.editorContent) {
                            window.mdContentEditor.setValue(this.formData.editorContent);
                        }

                        inlineAttachment.editors.monaco.attach(
                            window.mdContentEditor,
                            document.getElementsByClassName('md-editor-image-upload-area')[0],
                            {
                                uploadUrl: '/image',
                                urlText: '![file]({filename})',
                                onFileUploadResponse: function (xhr) {
                                    var result = JSON.parse(xhr.responseText),
                                        filename = result[this.settings.jsonFieldName];

                                    if (result && filename) {
                                        var newValue;
                                        if (typeof this.settings.urlText === 'function') {
                                            newValue = this.settings.urlText.call(this, filename, result);
                                        } else {
                                            newValue = this.settings.urlText.replace(this.filenameTag, filename);
                                        }
                                        var text = this.editor.getValue().replace(this.lastValue, newValue);
                                        this.editor.setValue(text);
                                        this.settings.onFileUploaded.call(this, filename);
                                    }
                                    return false;
                                }
                            }
                        );

                        resolve();
                    });
                });
            }

            this._editorInitialized = true;
        },

        async switchEditor() {
            if (!this._editorInitialized) return;

            this.syncEditorContent();

            // Destroy current editors
            if (window.tinyMCE) {
                window.tinyMCE.remove();
            }
            if (window.mdContentEditor) {
                window.mdContentEditor.dispose();
                window.mdContentEditor = null;
            }

            // Wait for Alpine to re-render the template
            await this.$nextTick();
            await this.initEditor();
        },

        syncEditorContent() {
            if (window.tinyMCE) {
                window.tinyMCE.triggerSave();
                const ta = document.querySelector('.post-content-textarea');
                if (ta) {
                    this.formData.editorContent = ta.value;
                }
            }

            if (window.mdContentEditor) {
                this.formData.editorContent = window.mdContentEditor.getValue();
            }
        }
    };
}
