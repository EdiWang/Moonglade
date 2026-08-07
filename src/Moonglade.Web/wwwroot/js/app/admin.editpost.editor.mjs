import { codeSampleLanguages } from './admin.editor.module.mjs';

const richHtmlEditorModulePath = '/_content/Moonglade.Editor.StaticAssets/moonglade-editor/moonglade-editor.rich-html.js';
const codeEditorModulePath = '/_content/Moonglade.Editor.StaticAssets/moonglade-editor/moonglade-editor.code.js';
const htmlEditorImageExtensions = ['.png', '.jpg', '.jpeg', '.gif', '.webp', '.svg'];
const csrfFieldName = 'CSRF-TOKEN-MOONGLADE-FORM';

let richHtmlEditorModulePromise = null;
let codeEditorModulePromise = null;

async function ensureRichHtmlEditor() {
    if (!richHtmlEditorModulePromise) {
        richHtmlEditorModulePromise = import(richHtmlEditorModulePath);
    }

    return await richHtmlEditorModulePromise;
}

async function ensureCodeEditor() {
    if (!codeEditorModulePromise) {
        codeEditorModulePromise = import(codeEditorModulePath);
    }

    return await codeEditorModulePromise;
}

function getCsrfToken() {
    return document.querySelector(`input[name="${csrfFieldName}"]`)?.value ?? '';
}

async function uploadPostImage(file) {
    const formData = new FormData();
    formData.append('file', file);

    const response = await fetch('/image', {
        method: 'POST',
        credentials: 'include',
        headers: {
            'Accept': 'application/json',
            'XSRF-TOKEN': getCsrfToken()
        },
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

    return {
        src: url,
        alt: result?.filename || file.name,
        title: result?.title
    };
}

async function uploadMarkdownImage(file) {
    const result = await uploadPostImage(file);
    return { url: result.src };
}

export function createEditorMixin() {
    return {
        editorChoice: '',
        _editorInitialized: false,

        async initEditor() {
            if (this.formData.contentType === 'html') {
                const { createMoongladeEditor } = await ensureRichHtmlEditor();
                const editorElement = document.getElementById('html-content-editor');
                const textarea = document.querySelector('.post-content-textarea');

                if (editorElement && textarea) {
                    if (window.htmlContentEditor) {
                        window.htmlContentEditor.destroy();
                    }

                    window.htmlContentEditor = createMoongladeEditor({
                        mode: 'rich-html',
                        element: editorElement,
                        textarea,
                        height: '100%',
                        spellcheck: true,
                        uploadImage: uploadPostImage,
                        allowedImageExtensions: htmlEditorImageExtensions,
                        codesample_languages: codeSampleLanguages,
                        onChange: (html) => {
                            this.formData.editorContent = html;
                        }
                    });
                }
            }

            if (this.formData.contentType === 'markdown') {
                const { createMoongladeCodeEditor } = await ensureCodeEditor();
                const editorElement = document.getElementById('markdown-content-editor');
                const textarea = document.querySelector('.post-content-textarea');

                if (editorElement && textarea) {
                    if (window.mdContentEditor) {
                        window.mdContentEditor.destroy();
                    }

                    window.mdContentEditor = createMoongladeCodeEditor({
                        language: 'markdown',
                        element: editorElement,
                        textarea,
                        content: this.formData.editorContent || '',
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
