import { loadTinyMCE } from './admin.editor.module.mjs';

const scriptPromises = new Map();

function loadScript(src) {
    if (scriptPromises.has(src)) {
        return scriptPromises.get(src);
    }

    const existing = document.querySelector(`script[src="${src}"]`);
    if (existing) {
        // If the script element is already loaded, resolve immediately and cache.
        if (
            existing.dataset.loaded === 'true' ||
            existing.readyState === 'complete' ||
            existing.readyState === 'loaded'
        ) {
            const resolved = Promise.resolve();
            scriptPromises.set(src, resolved);
            return resolved;
        }
        // Script tag exists but may still be loading; create a promise that
        // resolves when it finishes, and attach listeners if not already present.
        const promise = new Promise((resolve, reject) => {
            const onLoad = () => {
                existing.dataset.loaded = 'true';
                existing.removeEventListener('load', onLoad);
                existing.removeEventListener('error', onError);
                resolve();
            };
            const onError = (e) => {
                existing.removeEventListener('load', onLoad);
                existing.removeEventListener('error', onError);
                scriptPromises.delete(src);
                reject(e);
            };

            existing.addEventListener('load', onLoad);
            existing.addEventListener('error', onError);
        });
        scriptPromises.set(src, promise);
        return promise;
    }

    const promise = new Promise((resolve, reject) => {
        const script = document.createElement('script');
        script.src = src;
        script.onload = () => {
            script.dataset.loaded = 'true';
            resolve();
        };
        script.onerror = (e) => {
            scriptPromises.delete(src);
            reject(e);
        };
        document.head.appendChild(script);
    });

    scriptPromises.set(src, promise);
    return promise;
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
