import { codeSampleLanguages } from './admin.editor.module.mjs';

const htmlEditorModulePath = '/lib/moonglade-editor/moonglade-editor.js';
const htmlEditorImageExtensions = ['.png', '.jpg', '.jpeg', '.gif', '.webp', '.svg'];

let htmlEditorModulePromise = null;

async function ensureMoongladeHtmlEditor() {
    if (!htmlEditorModulePromise) {
        htmlEditorModulePromise = import(htmlEditorModulePath);
    }

    return await htmlEditorModulePromise;
}

function ensureCodeEditor() {
    if (!window.MoongladeCodeEditor?.createMoongladeCodeEditor) {
        throw new Error('Moonglade.CodeEditor static assets were not loaded.');
    }

    return window.MoongladeCodeEditor;
}

async function uploadMarkdownImage(file) {
    const formData = new FormData();
    formData.append('file', file);

    const response = await fetch('/image', {
        method: 'POST',
        credentials: 'include',
        body: formData
    });

    if (!response.ok) {
        const message = await response.text();
        throw new Error(message || `Image upload failed with status ${response.status}.`);
    }

    const result = await response.json();
    const url = result?.location || result?.filename;

    if (!url) {
        throw new Error('Image upload response did not include an image URL.');
    }

    return { url };
}

export function createEditorMixin() {
    return {
        editorChoice: '',
        _editorInitialized: false,

        async initEditor() {
            if (this.formData.contentType === 'html') {
                const { createMoongladeEditor } = await ensureMoongladeHtmlEditor();
                const editorElement = document.getElementById('html-content-editor');
                const textarea = document.querySelector('.post-content-textarea');

                if (editorElement && textarea) {
                    if (window.htmlContentEditor) {
                        window.htmlContentEditor.destroy();
                    }

                    window.htmlContentEditor = createMoongladeEditor({
                        element: editorElement,
                        textarea,
                        height: '100%',
                        spellcheck: true,
                        uploadUrl: '/image',
                        allowedImageExtensions: htmlEditorImageExtensions,
                        codesample_languages: codeSampleLanguages,
                        onChange: (html) => {
                            this.formData.editorContent = html;
                        }
                    });
                }
            }

            if (this.formData.contentType === 'markdown') {
                const codeEditor = ensureCodeEditor();
                const editorElement = document.getElementById('markdown-content-editor');
                const textarea = document.querySelector('.post-content-textarea');

                if (editorElement && textarea) {
                    if (window.mdContentEditor) {
                        window.mdContentEditor.destroy();
                    }

                    window.mdContentEditor = codeEditor.createMoongladeCodeEditor({
                        element: editorElement,
                        textarea,
                        content: this.formData.editorContent || '',
                        language: 'markdown',
                        height: '100%',
                        lineWrapping: true,
                        tabSize: 2,
                        markdownImageUpload: {
                            upload: uploadMarkdownImage
                        },
                        onChange: (markdown) => {
                            this.formData.editorContent = markdown;
                        }
                    });
                }
            }

            this._editorInitialized = true;
        },

        async switchEditor() {
            if (!this._editorInitialized) return;

            this.syncEditorContent();

            // Destroy current editors
            if (window.htmlContentEditor) {
                window.htmlContentEditor.destroy();
                window.htmlContentEditor = null;
            }
            if (window.mdContentEditor) {
                window.mdContentEditor.destroy();
                window.mdContentEditor = null;
            }

            // Wait for Alpine to re-render the template
            await this.$nextTick();
            await this.initEditor();
        },

        syncEditorContent() {
            if (window.htmlContentEditor) {
                window.htmlContentEditor.syncToTextarea();
                this.formData.editorContent = window.htmlContentEditor.getHTML();
            }

            if (window.mdContentEditor) {
                this.formData.editorContent = window.mdContentEditor.getValue();
            }
        }
    };
}
