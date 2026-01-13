import { default as Alpine } from '/lib/alpinejs/alpinejs.3.15.0.module.esm.min.js';
import { fetch2 } from './httpService.mjs?v=1500';
import { success } from './toastService.mjs';

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

        this.initMonacoEditor();
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
        } finally {
            this.isLoading = false;
        }
    },

    initMonacoEditor() {
        require(['vs/editor/editor.main'], () => {
            htmlContentEditor = window.initEditor(
                'RawHtmlContentEditor', 
                '.page-rawhtmlcontent-textarea', 
                'html'
            );
        });
    },

    setupTabHandlers() {
        document.querySelectorAll('a[data-bs-toggle="tab"]').forEach(element => {
            element.addEventListener('shown.bs.tab', (e) => {
                const isCssTab = e.target.id === "csscontent-tab";
                if (isCssTab && !hasCssEditorInitialized) {
                    cssContentEditor = window.initEditor(
                        'CssContentEditor', 
                        '.page-csscontent-textarea', 
                        'css'
                    );
                    hasCssEditorInitialized = true;
                }
            });
        });
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
                success('Page saved successfully.');

                if (this.isPreview) {
                    window.open(`/admin/page/preview/${data.pageId}`);
                    this.isPreview = false;
                }
            }
        } finally {
            this.isSaving = false;
        }
    }
}));

Alpine.start();