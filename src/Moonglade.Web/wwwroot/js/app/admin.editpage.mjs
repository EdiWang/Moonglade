import { Alpine } from './alpine-init.mjs';
import { fetch2 } from './httpService.mjs?v=1500';
import { success, error } from './toastService.mjs';
import { getLocalizedString } from './utils.module.mjs';
import { createMoongladeCodeEditor } from '/_content/Moonglade.Editor.StaticAssets/moonglade-editor/moonglade-editor.code.js';

let htmlContentEditor = null;
let cssContentEditor = null;
let hasCssEditorInitialized = false;

Alpine.data('pageEditor', () => ({
    pageId: null,
    isLoading: true,
    isSaving: false,
    isPreview: false,
    formData: {
        title: '',
        slug: '',
        metaDescription: '',
        rawHtmlContent: '',
        cssContent: '',
        hideSidebar: false,
        isPublished: false
    },

    async init() {
        // Get pageId from URL
        const urlParams = new URLSearchParams(window.location.search);
        const pathSegments = window.location.pathname.split('/');
        const idFromPath = pathSegments[pathSegments.length - 1];

        if (idFromPath && idFromPath !== 'edit' && idFromPath !== window.emptyGuid) {
            this.pageId = idFromPath;
            await this.loadPageData();
        } else {
            this.isLoading = false;
        }

        await this.$nextTick();
        this.initCodeEditors();
        this.setupTabHandlers();
        this.setupKeyboardShortcuts();
    },

    async loadPageData() {
        if (!this.pageId) return;

        this.isLoading = true;
        try {
            const data = await fetch2(`/api/page/${this.pageId}`, 'GET');

            this.formData = {
                title: data.title || '',
                slug: data.slug || '',
                metaDescription: data.metaDescription || '',
                rawHtmlContent: data.htmlContent || '',
                cssContent: data.cssContent || '',
                hideSidebar: data.hideSidebar || false,
                isPublished: data.isPublished || false
            };

            // Update editors with loaded content
            if (htmlContentEditor) {
                htmlContentEditor.setValue(this.formData.rawHtmlContent);
            }

            if (cssContentEditor) {
                cssContentEditor.setValue(this.formData.cssContent);
            }
        } catch (err) {
            error(err);
        } finally {
            this.isLoading = false;
        }
    },

    initCodeEditors() {
        htmlContentEditor = this.createCodeEditor(
            '#RawHtmlContentEditor',
            'html',
            this.formData.rawHtmlContent
        );
    },

    setupTabHandlers() {
        document.querySelectorAll('a[data-bs-toggle="tab"]').forEach(element => {
            element.addEventListener('shown.bs.tab', (e) => {
                const isCssTab = e.target.id === "csscontent-tab";
                if (isCssTab && !hasCssEditorInitialized) {
                    cssContentEditor = this.createCodeEditor(
                        '#CssContentEditor',
                        'css',
                        this.formData.cssContent
                    );
                    hasCssEditorInitialized = true;
                }

                if (e.target.id === "preview-tab") {
                    this.refreshPreview();
                }
            });
        });
    },

    createCodeEditor(elementSelector, language, content) {
        return createMoongladeCodeEditor({
            language,
            element: document.querySelector(elementSelector),
            content: content || '',
            height: '100%',
            lineWrapping: true,
            tabSize: 2
        });
    },

    refreshPreview() {
        this.syncEditorValues();
        const frame = document.getElementById('preview-frame');
        const doc = frame.contentDocument || frame.contentWindow.document;
        doc.open();
        doc.write(`<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <link rel="stylesheet" href="/lib/twitter-bootstrap/css/bootstrap.min.css" />
    <link rel="stylesheet" href="/css/base.css" />
    <style>${this.formData.cssContent || ''}</style>
</head>
<body>
    <main>
        ${this.formData.rawHtmlContent || ''}
    </main>
</body>
</html>`);
        doc.close();
    },

    setupKeyboardShortcuts() {
        window.addEventListener('keydown', (event) => {
            if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 's') {
                event.preventDefault();
                this.handleSubmit();
            }
        });
    },

    syncEditorValues() {
        if (htmlContentEditor) {
            this.formData.rawHtmlContent = htmlContentEditor.getValue();
        }

        if (hasCssEditorInitialized && cssContentEditor) {
            this.formData.cssContent = cssContentEditor.getValue();
        }
    },

    async handleSubmit() {
        this.syncEditorValues();
        this.isSaving = true;

        try {
            const isCreateMode = !this.pageId || this.pageId === window.emptyGuid;
            const apiAddress = isCreateMode ? '/api/page' : `/api/page/${this.pageId}`;
            const verb = isCreateMode ? 'POST' : 'PUT';

            const data = await fetch2(apiAddress, verb, this.formData);

            if (data.pageId) {
                this.pageId = data.pageId;
                success(getLocalizedString('pageSaved'));

                if (this.isPreview) {
                    window.open(`/admin/page/preview/${data.pageId}`);
                    this.isPreview = false;
                }
            }
        } catch (err) {
            error(err);
        } finally {
            this.isSaving = false;
        }
    }
}));
