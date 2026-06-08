import { Alpine } from './alpine-init.mjs';
import { fetch2 } from './httpService.mjs?v=1500';
import { success, error } from './toastService.mjs';
import { keepAlive } from './admin.editor.module.mjs';
import { showConfirmModal, hideConfirmModal, escapeHtml } from './adminModal.mjs';
import { getLocalizedString } from './utils.module.mjs';
import { createSlugMixin } from './admin.editpost.slug.mjs';
import { createEditorMixin } from './admin.editpost.editor.mjs';
import { createTagifyMixin } from './admin.editpost.tagify.mjs';
import { createScheduleMixin } from './admin.editpost.schedule.mjs';
import { createFormMixin } from './admin.editpost.form.mjs';

const AUTO_SAVE_INTERVAL_MS = 60 * 1000;
const AUTO_SAVE_STORAGE_KEY = 'moonglade.postEditor.autoSaveEnabled';

Alpine.data('postEditor', () => ({
    postId: null,
    isLoading: true,
    isSaving: false,
    isAutoSaving: false,
    submitAction: 'save',
    categories: [],
    languages: [],
    autoSaveTimerId: null,
    lastSavedSnapshot: '',

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
        containsAiAssistedContent: false,
        languageCode: '',
        abstract: '',
        keywords: '',
        tags: '',
        publishDate: null,
        scheduledPublishTime: null,
        scheduledPublishTimeUtc: null,
        clientTimeZoneId: '',
        lastModifiedUtc: '',
        selectedCatIds: [],
        contentType: ''
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

        this.$nextTick(async () => {
            await this.initEditor();
            await this.initTagify();
            this.updateMinScheduleDate();
            this.initScheduleState();
            this.setupKeyboardShortcuts();
            this.setupDirtyFormWarning();
            this.updateSavedSnapshot();
            this.setupAutoSave();
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
                    this.formData.contentType = meta.editorChoice || 'html';
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
                containsAiAssistedContent: data.containsAiAssistedContent ?? false,
                languageCode: data.languageCode || '',
                abstract: data.contentAbstract || '',
                keywords: data.keywords || '',
                tags: data.tags || '',
                publishDate: data.publishDate ? data.publishDate.substring(0, 10) : null,
                scheduledPublishTimeUtc: data.scheduledPublishTimeUtc || null,
                lastModifiedUtc: data.lastModifiedUtc || '',
                selectedCatIds: data.selectedCatIds || [],
                contentType: data.contentType || this.editorChoice || 'html'
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

    getString(key) {
        return getLocalizedString(key);
    },

    createPostRequestData() {
        return {
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
            containsAiAssistedContent: this.formData.containsAiAssistedContent,
            languageCode: this.formData.languageCode,
            abstract: this.formData.abstract,
            keywords: this.formData.keywords,
            tags: this.formData.tags,
            selectedCatIds: this.formData.selectedCatIds,
            publishDate: this.formData.publishDate,
            scheduledPublishTime: this.formData.scheduledPublishTime || null,
            clientTimeZoneId: this.formData.clientTimeZoneId,
            lastModifiedUtc: this.formData.lastModifiedUtc,
            contentType: this.formData.contentType
        };
    },

    createPostSnapshot() {
        const { lastModifiedUtc, ...snapshot } = this.createPostRequestData();
        return JSON.stringify(snapshot);
    },

    updateSavedSnapshot() {
        this.syncEditorContent();
        this.syncTags();
        this.lastSavedSnapshot = this.createPostSnapshot();
    },

    validatePostForm(reportInvalid) {
        const form = document.getElementById('post-edit-form');

        if (form) {
            const isValid = reportInvalid ? form.reportValidity() : form.checkValidity();
            if (!isValid) return false;
        }

        if (!this.formData.editorContent) {
            if (reportInvalid) {
                error(this.getString('pleaseEnterContent'));
            }
            return false;
        }

        return true;
    },

    async handleSubmit() {
        this.syncEditorContent();

        if (!this.validatePostForm(true)) {
            return;
        }

        this.syncTags();

        if (this.submitAction === 'publish') {
            if (this.enableSchedule && this.formData.scheduledPublishTime) {
                this.formData.postStatus = 'Scheduled';
            } else {
                this.formData.postStatus = 'Published';
                this.formData.scheduledPublishTime = null;
            }
        }

        try {
            const resp = await this.savePost();

            if (resp && resp.postId) {
                success(this.getString('postSaved'));
                this.syncAutoSaveAvailability();

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

    async savePost() {
        this.isSaving = true;

        const resp = await fetch2('/api/post/createoredit', 'POST', this.createPostRequestData());

        if (resp && resp.postId) {
            this.postId = resp.postId;
            this.formData.postId = resp.postId;
            this.formData.lastModifiedUtc = resp.lastModifiedUtc || this.formData.lastModifiedUtc;
            this.isFormDirty = false;
            this.lastSavedSnapshot = this.createPostSnapshot();
        }

        return resp;
    },

    setupAutoSave() {
        const toggle = document.getElementById('auto-save-toggle');
        if (!toggle) return;

        toggle.addEventListener('change', () => {
            this.setAutoSaveEnabled(toggle.checked);
        });

        this.syncAutoSaveAvailability();
    },

    isDraftPost() {
        return this.formData.postStatus === 'Draft';
    },

    syncAutoSaveAvailability() {
        const item = document.getElementById('auto-save-nav-item');
        const toggle = document.getElementById('auto-save-toggle');
        const isAvailable = this.isDraftPost();

        if (item) {
            item.classList.toggle('d-none', !isAvailable);
            item.classList.toggle('d-flex', isAvailable);
        }

        if (!isAvailable) {
            if (toggle) {
                toggle.checked = false;
            }
            this.stopAutoSave();
            this.updateAutoSaveStatus('');
            return;
        }

        const autoSaveEnabled = window.localStorage.getItem(AUTO_SAVE_STORAGE_KEY) === 'true';
        if (toggle) {
            toggle.checked = autoSaveEnabled;
        }
        this.setAutoSaveEnabled(autoSaveEnabled, false);
    },

    stopAutoSave() {
        window.clearInterval(this.autoSaveTimerId);
        this.autoSaveTimerId = null;
    },

    setAutoSaveEnabled(enabled, persist = true) {
        if (enabled && !this.isDraftPost()) {
            const toggle = document.getElementById('auto-save-toggle');
            if (toggle) {
                toggle.checked = false;
            }
            this.stopAutoSave();
            this.updateAutoSaveStatus('');
            return;
        }

        if (persist) {
            window.localStorage.setItem(AUTO_SAVE_STORAGE_KEY, enabled ? 'true' : 'false');
        }

        this.stopAutoSave();

        this.updateAutoSaveStatus(enabled ? this.getString('autoSaveOn') : this.getString('autoSaveOff'));

        if (!enabled) return;

        this.autoSaveTimerId = window.setInterval(() => {
            this.autoSave();
        }, AUTO_SAVE_INTERVAL_MS);
    },

    async autoSave() {
        if (!this.isDraftPost() || this.isLoading || this.isSaving || this.isAutoSaving) return;

        this.syncEditorContent();
        this.syncTags();

        if (!this.validatePostForm(false)) {
            this.updateAutoSaveStatus(this.getString('autoSaveWaiting'));
            return;
        }

        const currentSnapshot = this.createPostSnapshot();
        if (currentSnapshot === this.lastSavedSnapshot) return;

        this.isAutoSaving = true;
        const originalSubmitAction = this.submitAction;
        this.submitAction = 'save';

        try {
            const resp = await this.savePost();
            if (resp && resp.postId) {
                const savedAt = new Date().toLocaleTimeString();
                this.updateAutoSaveStatus(this.getString('autoSavedAt').replace('{0}', savedAt));
            }
        } catch (err) {
            error(err);
        } finally {
            this.isAutoSaving = false;
            this.isSaving = false;
            this.submitAction = originalSubmitAction;
        }
    },

    updateAutoSaveStatus(message) {
        const status = document.getElementById('auto-save-status');
        if (status) {
            status.textContent = message || '';
        }
    },

    openUnpublishModal() {
        showConfirmModal({
            title: this.getString('unpublishTitle'),
            body: `<div class="alert alert-warning">${escapeHtml(this.getString('unpublishWarning'))}</div><p>${escapeHtml(this.formData.title)}</p>`,
            confirmText: this.getString('confirm'),
            confirmClass: 'btn-danger',
            onConfirm: async () => {
                try {
                    if (!this.postId) return;
                    await fetch2(`/api/post/${this.postId}/unpublish`, 'PUT', {});
                    success(this.getString('postUnpublished'));
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
