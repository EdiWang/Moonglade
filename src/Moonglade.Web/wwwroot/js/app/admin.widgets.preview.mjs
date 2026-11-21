import { previewWidgetCollection } from './admin.widgets.api.mjs';

export function createWidgetPreview() {
    return {
        previewHtml: '', previewState: 'idle', previewVersion: 0, previewTimer: null, previewController: null, previewHeight: 1,
        get previewFingerprint() { return this.loaded ? JSON.stringify(this.payload()) : ''; },
        get previewStatusText() {
            return this.t(this.previewState === 'ready' ? 'draftPreview' : this.previewState === 'loading' ? 'updatingPreview'
                : this.previewState === 'invalid' ? 'previewInvalid' : this.previewState === 'failed' ? 'previewFailed' : 'loadingWidgets');
        },
        initializePreview() {
            this.$watch('previewFingerprint', () => this.schedulePreview());
            this.previewThemeObserver = new MutationObserver(() => this.schedulePreview());
            this.previewThemeObserver.observe(document.documentElement, { attributes: true, attributeFilter: ['data-bs-theme'] });
            this.schedulePreview();
        },
        schedulePreview() {
            clearTimeout(this.previewTimer);
            this.previewController?.abort();
            const version = ++this.previewVersion;
            if (!this.loaded) return;
            if (this.issues.length) { this.previewState = 'invalid'; return; }
            this.previewState = 'loading';
            this.previewTimer = setTimeout(() => this.refreshPreview(version), 300);
        },
        async refreshPreview(version = this.previewVersion) {
            if (version !== this.previewVersion || !this.loaded || this.issues.length) return;
            clearTimeout(this.previewTimer);
            const controller = new AbortController();
            this.previewController = controller;
            const fingerprint = this.previewFingerprint;
            try {
                const html = await previewWidgetCollection(this.payload(), controller.signal);
                if (version === this.previewVersion && fingerprint === this.previewFingerprint) {
                    this.previewHtml = html;
                    this.previewState = 'ready';
                }
            } catch {
                if (version === this.previewVersion) this.previewState = 'failed';
            } finally {
                if (version === this.previewVersion) this.previewController = null;
            }
        },
        measurePreview(frame) {
            this.previewLayoutObserver?.disconnect();
            const body = frame.contentDocument?.body;
            if (!body) return;
            const measure = () => { this.previewHeight = Math.max(1, Math.ceil(body.scrollHeight)); };
            this.previewLayoutObserver = new ResizeObserver(measure);
            this.previewLayoutObserver.observe(body);
            measure();
        },
        stopPreview() {
            ++this.previewVersion;
            clearTimeout(this.previewTimer);
            this.previewController?.abort();
            this.previewThemeObserver?.disconnect();
            this.previewLayoutObserver?.disconnect();
        }
    };
}
