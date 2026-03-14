import { loadTinyMCE } from './admin.editor.module.mjs';

export function createEditorMixin() {
    return {
        editorChoice: '',

        initEditor() {
            if (this.editorChoice === 'html') {
                loadTinyMCE('.post-content-textarea');
            }

            if (this.editorChoice === 'markdown') {
                require(['vs/editor/editor.main'], () => {
                    window.mdContentEditor = initEditor('markdown-content-editor', '.post-content-textarea', 'markdown');

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
                });
            }
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
