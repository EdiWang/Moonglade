import { Alpine } from './alpine-init.mjs';
import { fetch2 } from './httpService.mjs?v=1500';
import { success, error } from './toastService.mjs';
import { keepAlive } from './admin.editor.module.mjs';
import { showConfirmModal, hideConfirmModal, escapeHtml } from './adminModal.mjs';
import { createSlugMixin } from './admin.editpost.slug.mjs';
import { createEditorMixin } from './admin.editpost.editor.mjs';
import { createTagifyMixin } from './admin.editpost.tagify.mjs';
import { createScheduleMixin } from './admin.editpost.schedule.mjs';
import { createFormMixin } from './admin.editpost.form.mjs';

Alpine.data('postEditor', () => ({
    postId: null,
    isLoading: true,
    isSaving: false,
    submitAction: 'save',
    categories: [],
    languages: [],

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
        lastModifiedUtc: '',
        selectedCatIds: []
    },

    ...createSlugMixin(),
    ...createEditorMixin(),
    ...createTagifyMixin(),
    ...createScheduleMixin(),
    ...createFormMixin(),

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
            this.initScheduleState();
            this.setupKeyboardShortcuts();
            this.setupDirtyFormWarning();
            keepAlive();
        });
    },

    async loadMeta() {
        try {
            const meta = await fetch2('/api/post/meta', 'GET');
            if (meta) {
                this.editorChoice = meta.editorChoice;
                this.categories = meta.categories || [];
                this.languages = meta.languages || [];

                if (!this.postId) {
                    this.formData.author = meta.defaultAuthor || '';
                }

                if (this.languages.length > 0 && !this.formData.languageCode) {
                    this.formData.languageCode = this.languages[0].value;
                }
            }
        } catch (err) {
            error(err);
        }
    },

    async loadPostData() {
        if (!this.postId) return;

        try {
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
        } catch (err) {
            error(err);
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

    async handleSubmit() {
        this.syncEditorContent();

        if (!this.formData.editorContent) {
            error('Please enter content.');
            return;
        }

        // Sync tags from tagify
        this.syncTags();

        if (this.submitAction === 'publish') {
            if (this.enableSchedule && this.formData.scheduledPublishTime) {
                this.formData.postStatus = 'Scheduled';
            } else {
                this.formData.postStatus = 'Published';
                this.formData.scheduledPublishTime = null;
            }
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
        } catch (err) {
            error(err);
        } finally {
            this.isSaving = false;
            this.submitAction = 'save';
        }
    },

    openUnpublishModal() {
        showConfirmModal({
            title: 'Unpublish Post',
            body: `<div class="alert alert-warning">Unpublishing this post will remove it from the public site and turn it into a draft. This will have impact on SEO. Please confirm.</div><p>${escapeHtml(this.formData.title)}</p>`,
            confirmText: 'Confirm',
            confirmClass: 'btn-danger',
            onConfirm: async () => {
                try {
                    if (!this.postId) return;
                    await fetch2(`/api/post/${this.postId}/unpublish`, 'PUT', {});
                    success('Post unpublished');
                    location.reload();
                } catch (err) {
                    error(err);
                } finally {
                    hideConfirmModal();
                }
            }
        });
    }
}));