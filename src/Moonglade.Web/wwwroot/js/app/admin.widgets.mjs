import { Alpine } from './alpine-init.mjs';
import { createWidgetDraft, widgetPayload, validateWidget } from './admin.widgets.draft.mjs';
import { loadWidgetCollection, saveWidgetCollection } from './admin.widgets.api.mjs';
import { getLocalizedString } from './utils.module.mjs';
import { showConfirmModal, hideConfirmModal, escapeHtml } from './adminModal.mjs';

Alpine.data('widgetManager', () => Object.defineProperties(createWidgetDraft(), Object.getOwnPropertyDescriptors({
    isLoading: false, isSaving: false, loadError: '', saveMessage: '', saveState: '',
    validationShown: false, serverIssues: [], uncertain: false, committed: false,
    t: getLocalizedString,
    get locked() { return !this.loaded || this.isLoading || this.isSaving || this.committed; },
    get canSave() { return !this.locked && this.dirty; },
    get allIssues() { return [...(this.validationShown ? this.issues : []), ...this.serverIssues]; },
    get currentIssues() { return this.allIssues.filter(issue => issue.widgetId === this.selectedId); },
    get statusText() {
        if (this.isLoading) return this.t('loadingWidgets');
        if (this.isSaving) return this.t('saving');
        if (this.committed) return this.t('saved');
        if (this.uncertain) return this.t('saveNotConfirmed');
        return this.dirty ? this.t('unsavedChanges') : this.loaded ? this.t('saved') : '';
    },
    async init() {
        this.leaveHandler = event => {
            if ((this.dirty && !this.committed) || this.isSaving) { event.preventDefault(); event.returnValue = ''; }
        };
        window.addEventListener('beforeunload', this.leaveHandler);
        await this.loadWidgets();
    },
    destroy() { window.removeEventListener('beforeunload', this.leaveHandler); },
    confirmAction(messageKey, action) {
        showConfirmModal({
            title: this.t('confirm'), body: escapeHtml(this.t(messageKey)),
            confirmText: this.t('confirm'), confirmClass: 'btn-accent',
            onConfirm: async () => { hideConfirmModal(); await action(); }
        });
    },
    async loadWidgets(confirmed = false) {
        if (this.isLoading || this.isSaving) return;
        if (this.dirty && !confirmed) return this.confirmAction('confirmReload', () => this.loadWidgets(true));
        this.isLoading = true;
        this.loadError = '';
        try {
            this.acceptSnapshot(await loadWidgetCollection());
            this.clearSaveState();
        } catch (error) {
            this.loadError = this.t('loadFailed');
            const invalidId = /^Invalid widget content: ([0-9a-f-]{36})$/i.exec(error.message);
            if (invalidId) this.loadError += ` (${invalidId[1]})`;
        } finally {
            this.isLoading = false;
        }
    },
    clearSaveState() {
        this.serverIssues = [];
        this.validationShown = false;
        this.uncertain = false;
        this.committed = false;
        this.saveState = '';
        this.saveMessage = '';
    },
    edited() {
        this.serverIssues = [];
        if (!this.uncertain && !this.committed) { this.saveState = ''; this.saveMessage = ''; }
    },
    chooseWidget(id) { this.selectWidget(id); this.focusTitle(); },
    addWidget(type) {
        if (this.locked) return;
        this.createWidget(type);
        this.edited();
        this.focusTitle();
    },
    changeType(type, select, confirmed = false) {
        select.value = this.selectedWidget.widgetType;
        if (this.locked || type === this.selectedWidget.widgetType) return;
        if (!confirmed) return this.confirmAction('confirmTypeChange', () => this.changeType(type, select, true));
        this.switchType(type);
        this.edited();
        select.value = this.selectedWidget.widgetType;
    },
    changeRegion(region) {
        if (this.locked) return;
        this.placeWidget(region);
        this.edited();
        this.revealSelection();
    },
    moveSelected(direction) {
        if (this.locked) return;
        this.moveWidget(direction);
        this.edited();
        this.revealSelection();
    },
    stageDeletion() {
        if (this.locked) return;
        this.removeWidget();
        this.edited();
        this.$nextTick(() => {
            const button = this.selectedId ? document.getElementById(`widget-select-${this.selectedId}`) : this.$refs.addWidget;
            button?.focus();
        });
    },
    undoDeletion(id) {
        if (this.locked) return;
        this.restoreWidget(id);
        this.edited();
        this.revealSelection();
    },
    discardChanges(confirmed = false) {
        if (this.locked || !this.dirty) return;
        if (!confirmed) return this.confirmAction('confirmDiscard', () => this.discardChanges(true));
        const uncertain = this.uncertain;
        this.discard();
        this.clearSaveState();
        if (uncertain) {
            this.uncertain = true;
            this.saveState = 'uncertain';
            this.saveMessage = this.t('saveUncertain');
        }
        this.focusTitle();
    },
    addInlineItem() {
        if (this.locked) return;
        const item = this.addItem();
        this.edited();
        if (item) this.$nextTick(() => document.getElementById(`item-name-${item.key}`)?.focus());
    },
    deleteInlineItem(itemKey) {
        if (this.locked) return;
        const index = this.selectedWidget.items.findIndex(item => item.key === itemKey);
        this.removeItem(itemKey);
        this.edited();
        this.$nextTick(() => {
            const next = this.selectedWidget.items[Math.min(index, this.selectedWidget.items.length - 1)];
            if (next) document.getElementById(`item-expand-${next.key}`)?.focus();
            else this.$refs.addItem?.focus();
        });
    },
    moveInlineItem(itemKey, direction) {
        if (this.locked) return;
        this.moveItem(itemKey, direction);
        this.edited();
        this.$nextTick(() => document.getElementById(`item-expand-${itemKey}`)?.focus());
    },
    focusTitle() { this.$nextTick(() => this.$refs.widgetTitle?.focus()); },
    focusInvalidField() { this.$nextTick(() => (document.querySelector('#widget-composer-form [aria-invalid="true"]') ?? this.$refs.widgetTitle)?.focus()); },
    revealSelection() { this.$nextTick(() => document.getElementById(`widget-select-${this.selectedId}`)?.scrollIntoView({ block: 'nearest' })); },
    itemLabel(item) { return item.data.name || item.data.text || this.t(this.selectedWidget?.widgetType === 'LinkList' ? 'unnamedLink' : 'unnamedButton'); },
    fieldError(field, itemKey = null) {
        return this.currentIssues.filter(issue => issue.field === field && issue.itemKey === itemKey)
            .map(issue => issue.message ?? this.t(issue.messageKey)).join(' ');
    },
    widgetHasErrors(id) { return this.allIssues.some(issue => issue.widgetId === id); },
    contentLength() { return this.selectedWidget ? widgetPayload(this.selectedWidget).contentCode.length : 0; },
    async saveChanges(confirmed = false) {
        if (!this.canSave) return;
        this.validationShown = true;
        this.serverIssues = [];
        const invalid = this.issues[0];
        if (invalid) {
            this.selectedId = invalid.widgetId;
            if (invalid.itemKey) this.selectedWidget.items.find(item => item.key === invalid.itemKey).expanded = true;
            this.saveState = 'validation';
            this.saveMessage = this.t('reviewErrors');
            this.focusInvalidField();
            return;
        }
        if (this.uncertain && !confirmed) return this.confirmAction('confirmOverwriteAfterFailure', () => this.saveChanges(true));
        const payload = this.payload();
        this.isSaving = true;
        this.saveState = '';
        this.saveMessage = '';
        try {
            this.acceptSnapshot(await saveWidgetCollection(payload));
            this.clearSaveState();
            this.saveState = 'saved';
        } catch (error) {
            if (error.problem?.committed === true) {
                this.committed = true;
                this.saveState = 'committed';
                this.saveMessage = this.t('savedCacheFailed');
            } else if (error.status === 400) {
                this.saveState = 'validation';
                this.saveMessage = this.t('reviewErrors');
                this.applyServerErrors(error.problem?.errors, payload);
            } else {
                this.uncertain = true;
                this.saveState = 'uncertain';
                this.saveMessage = this.t('saveUncertain');
            }
        } finally {
            this.isSaving = false;
        }
    },
    applyServerErrors(errors, payload) {
        for (const [path, messages] of Object.entries(errors ?? {})) {
            const match = /^Widgets\[(\d+)\]\.(.*)$/i.exec(path);
            const id = match ? payload.widgets[Number(match[1])]?.id : null;
            if (!id) continue;
            const fieldPath = match[2];
            const itemMatch = /^ContentCode\[(\d+)\](?:\.(\w+))?$/i.exec(fieldPath);
            const imageMatch = /^ContentCode\.(\w+)$/i.exec(fieldPath);
            const widget = this.widgets.find(item => item.id === id);
            const originalContent = widget && widgetPayload(widget).contentCode === widget.contentCode;
            const item = itemMatch ? (originalContent ? widget?.items.find(entry => entry.sourceIndex === Number(itemMatch[1])) : widget?.items[Number(itemMatch[1])]) : null;
            const field = itemMatch ? (itemMatch[2] ? itemMatch[2][0].toLowerCase() + itemMatch[2].slice(1) : 'contentCode')
                : imageMatch ? imageMatch[1][0].toLowerCase() + imageMatch[1].slice(1) : fieldPath[0].toLowerCase() + fieldPath.slice(1);
            this.serverIssues.push({ widgetId: id, field, itemKey: item?.key ?? null, message: [].concat(messages).join(' ') });
            if (item) item.expanded = true;
        }
        if (this.serverIssues[0]) { this.selectedId = this.serverIssues[0].widgetId; this.focusInvalidField(); }
    },
    validateWidget
})));
