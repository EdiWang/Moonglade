import { Alpine } from '/js/app/alpine-init.mjs';
import { fetch2 } from './httpService.mjs?v=1500';
import { success, error } from './toastService.mjs';
import { loadTinyMCE, keepAlive } from './admin.editor.module.mjs';

function slugify(text) {
    if (!/^[A-Za-z][A-Za-z0-9 \(\)#,\.\?]*$/.test(text)) {
        return '';
    }
    return text
        .toLowerCase()
        .replace(/[()#,.?]/g, '')
        .replace(/[^\w ]+/g, '')
        .replace(/ +/g, '-');
}

Alpine.data('postEditor', () => ({
    postId: null,
    isLoading: true,
    isSaving: false,
    submitAction: 'save',
    editorChoice: '',
    categories: [],
    languages: [],
    abstractWords: 400,
    warnSlugModification: false,
    slugUnlocked: false,
    scheduleInfoHtml: '',
    minScheduleDate: '',
    tagifyInstance: null,
    isFormDirty: false,

    formData: {
        postId: '',
        title: '',
        slug: '',
        author: '',
        editorContent: '',
        postStatus: 'Draft',
        enableComment: true,
        feedIncluded: true,
        featured: false,
        isOutdated: false,
        languageCode: '',
        abstract: '',
        keywords: '',
        tags: '',
        publishDate: null,
        scheduledPublishTime: null,
        scheduledPublishTimeUtc: null,
        clientTimeZoneId: '',
        changePublishDate: false,
        lastModifiedUtc: '',
        selectedCatIds: []
    },

    get abstractTooltip() {
        return `The first ${this.abstractWords} character(s) will be used as abstract if you leave this field blank.`;
    },

    async init() {
        const pathSegments = window.location.pathname.split('/');
        const idFromPath = pathSegments[pathSegments.length - 1];

        if (idFromPath && idFromPath !== 'edit' && idFromPath !== window.emptyGuid) {
            this.postId = idFromPath;
        }

        this.formData.clientTimeZoneId = Intl.DateTimeFormat().resolvedOptions().timeZone;

        await this.loadMeta();

        if (this.postId) {
            await this.loadPostData();
        }

        this.isLoading = false;

        this.$nextTick(() => {
            this.initEditor();
            this.initTagify();
            this.updateMinScheduleDate();
            this.updateScheduleInfo();
            this.setupKeyboardShortcuts();
            this.setupDirtyFormWarning();
            keepAlive();
        });
    },

    async loadMeta() {
        const meta = await fetch2('/api/post/meta', 'GET');
        if (meta) {
            this.editorChoice = meta.editorChoice;
            this.categories = meta.categories || [];
            this.languages = meta.languages || [];
            this.abstractWords = meta.abstractWords || 400;

            if (!this.postId) {
                this.formData.author = meta.defaultAuthor || '';
            }

            if (this.languages.length > 0 && !this.formData.languageCode) {
                this.formData.languageCode = this.languages[0].value;
            }
        }
    },

    async loadPostData() {
        if (!this.postId) return;

        const data = await fetch2(`/api/post/${this.postId}`, 'GET');
        if (!data) return;

        this.formData = {
            ...this.formData,
            postId: data.postId || '',
            title: data.title || '',
            slug: data.slug || '',
            author: data.author || '',
            editorContent: data.editorContent || '',
            postStatus: data.postStatus || 'Draft',
            enableComment: data.enableComment ?? true,
            feedIncluded: data.feedIncluded ?? true,
            featured: data.featured ?? false,
            isOutdated: data.isOutdated ?? false,
            languageCode: data.languageCode || '',
            abstract: data.contentAbstract || '',
            keywords: data.keywords || '',
            tags: data.tags || '',
            publishDate: data.publishDate ? data.publishDate.substring(0, 10) : null,
            scheduledPublishTimeUtc: data.scheduledPublishTimeUtc || null,
            lastModifiedUtc: data.lastModifiedUtc || '',
            selectedCatIds: data.selectedCatIds || []
        };

        // Determine warnSlugModification: post published > 3 days ago
        if (data.publishDate) {
            const pubDate = new Date(data.publishDate);
            const daysSincePublish = (Date.now() - pubDate.getTime()) / (1000 * 60 * 60 * 24);
            this.warnSlugModification = daysSincePublish > 3;
        }
    },

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

    async initTagify() {
        const data = await fetch2('/api/tags/names', 'GET', {});
        const input = document.querySelector('#post-tags-input');
        if (!input) return;

        this.tagifyInstance = new Tagify(input, {
            pattern: /^[a-zA-Z 0-9\.\-\+\#\s]*$/i,
            whitelist: data,
            originalInputValueFormat: valuesArr => valuesArr.map(item => item.value).join(','),
            maxTags: 10,
            dropdown: {
                maxItems: 30,
                classname: 'tags-dropdown',
                enabled: 0,
                closeOnSelect: false
            }
        });

        // Load existing tags
        if (this.formData.tags) {
            const existingTags = this.formData.tags.split(',').filter(t => t.trim());
            this.tagifyInstance.addTags(existingTags);
        }
    },

    setupKeyboardShortcuts() {
        window.addEventListener('keydown', (event) => {
            if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 's') {
                event.preventDefault();
                this.submitAction = 'save';
                this.handleSubmit();
            }
        });
    },

    setupDirtyFormWarning() {
        const form = document.getElementById('post-edit-form');
        if (!form) return;

        form.addEventListener('input', () => {
            this.isFormDirty = true;
        });

        window.addEventListener('beforeunload', (event) => {
            if (this.isFormDirty) {
                const message = 'You have unsaved changes, are you sure to leave this page?';
                event.returnValue = message;
                return message;
            }
        });
    },

    onTitleChange() {
        if (!this.warnSlugModification || this.slugUnlocked) {
            const newSlug = slugify(this.formData.title);
            if (newSlug) {
                this.formData.slug = newSlug;
            }
        }
    },

    unlockSlug() {
        const message = 'This post was published for a period of time, changing slug will result in breaking SEO, would you like to continue?';
        if (confirm(message)) {
            this.slugUnlocked = true;
        }
    },

    toggleCategory(catId, checked) {
        if (checked) {
            if (!this.formData.selectedCatIds.includes(catId)) {
                this.formData.selectedCatIds.push(catId);
            }
        } else {
            this.formData.selectedCatIds = this.formData.selectedCatIds.filter(id => id !== catId);
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
    },

    async handleSubmit() {
        this.syncEditorContent();

        if (!this.formData.editorContent) {
            error('Please enter content.');
            return;
        }

        // Sync tags from tagify
        if (this.tagifyInstance) {
            this.formData.tags = this.tagifyInstance.value.map(t => t.value).join(',');
        }

        if (this.submitAction === 'publish') {
            this.formData.postStatus = 'Published';
        }

        this.isSaving = true;
        this.isFormDirty = false;

        try {
            const requestData = {
                postId: this.formData.postId || window.emptyGuid,
                title: this.formData.title,
                slug: this.formData.slug,
                author: this.formData.author,
                editorContent: this.formData.editorContent,
                postStatus: this.formData.postStatus,
                enableComment: this.formData.enableComment,
                feedIncluded: this.formData.feedIncluded,
                featured: this.formData.featured,
                isOutdated: this.formData.isOutdated,
                languageCode: this.formData.languageCode,
                abstract: this.formData.abstract,
                keywords: this.formData.keywords,
                tags: this.formData.tags,
                selectedCatIds: this.formData.selectedCatIds,
                changePublishDate: this.formData.changePublishDate,
                publishDate: this.formData.publishDate,
                scheduledPublishTime: this.formData.scheduledPublishTime || null,
                clientTimeZoneId: this.formData.clientTimeZoneId,
                lastModifiedUtc: this.formData.lastModifiedUtc
            };

            const resp = await fetch2('/api/post/createoredit', 'POST', requestData);

            if (resp && resp.postId) {
                this.postId = resp.postId;
                this.formData.postId = resp.postId;
                success('Post saved successfully.');

                if (this.submitAction === 'preview') {
                    window.open(`/admin/post/preview/${resp.postId}`);
                }
            }
        } finally {
            this.isSaving = false;
            this.submitAction = 'save';
        }
    },

    async unpublishPost() {
        if (!this.postId) return;
        await fetch2(`/api/post/${this.postId}/unpublish`, 'PUT', {});
        success('Post unpublished');
        location.reload();
    },

    cancelSchedule() {
        this.formData.scheduledPublishTime = null;
        this.formData.postStatus = 'Draft';
        this.updateScheduleInfo();
    },

    confirmSchedule() {
        this.formData.postStatus = 'Scheduled';
        this.updateScheduleInfo();
    },

    updateMinScheduleDate() {
        this.minScheduleDate = new Date().toISOString().slice(0, 16);
    },

    updateScheduleInfo() {
        const status = this.formData.postStatus;

        if (status === 'Scheduled') {
            let displayTime;

            if (this.formData.scheduledPublishTime) {
                displayTime = new Date(this.formData.scheduledPublishTime).toLocaleString();
            } else if (this.formData.scheduledPublishTimeUtc) {
                const utcDate = new Date(this.formData.scheduledPublishTimeUtc);
                const localDate = new Date(utcDate.getTime() - utcDate.getTimezoneOffset() * 60000);
                displayTime = localDate.toLocaleString();

                // Sync the local time back to the input
                const pad = n => n < 10 ? '0' + n : n;
                const year = localDate.getFullYear();
                const month = pad(localDate.getMonth() + 1);
                const day = pad(localDate.getDate());
                const hours = pad(localDate.getHours());
                const minutes = pad(localDate.getMinutes());
                this.formData.scheduledPublishTime = `${year}-${month}-${day}T${hours}:${minutes}`;
            }

            this.scheduleInfoHtml = `<i class="bi-clock"></i> <span>Scheduled for: ${displayTime}</span>`;
        } else {
            this.scheduleInfoHtml = '';
        }
    }
}));

Alpine.start();