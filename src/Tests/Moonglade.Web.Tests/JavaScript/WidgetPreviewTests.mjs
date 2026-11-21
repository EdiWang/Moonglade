import assert from 'node:assert/strict';
import test from 'node:test';
import { previewWidgetCollection, WidgetRequestError } from '../../../Moonglade.Web/wwwroot/js/app/admin.widgets.api.mjs';
import { createWidgetPreview } from '../../../Moonglade.Web/wwwroot/js/app/admin.widgets.preview.mjs';

const response = html => ({ ok: true, status: 200, headers: new Headers({ 'Content-Type': 'text/html; charset=utf-8' }), text: async () => html });

test('actual preview lifecycle debounces, ignores stale responses and never changes draft state', async t => {
    const previous = ['fetch', 'document'].map(name => Object.getOwnPropertyDescriptor(globalThis, name));
    t.after(() => ['fetch', 'document'].forEach((name, index) => previous[index] ? Object.defineProperty(globalThis, name, previous[index]) : delete globalThis[name]));
    globalThis.document = { documentElement: { dataset: { bsTheme: 'dark' } }, querySelector: () => ({ value: 'csrf' }) };
    const pending = [];
    globalThis.fetch = (url, options) => new Promise(resolve => pending.push({ url, options, resolve }));
    const draft = { widgets: [{ title: 'First' }] };
    const preview = Object.defineProperties({ loaded: true, issues: [], payload: () => draft, t: key => key }, Object.getOwnPropertyDescriptors(createWidgetPreview()));
    t.after(() => preview.stopPreview());
    t.mock.timers.enable({ apis: ['setTimeout'] });
    preview.schedulePreview();
    t.mock.timers.tick(299);
    assert.equal(pending.length, 0);
    t.mock.timers.tick(1);
    assert.equal(pending.length, 1);
    assert.equal(pending[0].options.method, 'POST');
    assert.equal(pending[0].options.headers['XSRF-TOKEN'], 'csrf');
    assert.equal(pending[0].url, '/api/widgets/preview?dark=true&culture=en-US&ui-culture=en-US');
    draft.widgets[0].title = 'Latest';
    preview.schedulePreview();
    assert.equal(pending[0].options.signal.aborted, true);
    const latest = preview.refreshPreview();
    pending[1].resolve(response('<html>Latest</html>'));
    await latest;
    pending[0].resolve(response('<html>Obsolete</html>'));
    await new Promise(setImmediate);
    assert.equal(preview.previewHtml, '<html>Latest</html>');
    assert.equal(preview.previewState, 'ready');
    assert.equal(draft.widgets[0].title, 'Latest');
    assert.equal(pending.every(item => item.options.method === 'POST'), true);
    const fingerprint = preview.previewFingerprint;
    preview.selectedId = 'Other'; preview.expanded = false;
    assert.equal(preview.previewFingerprint, fingerprint);
});

test('invalid drafts and failed preview retain the last document; retry and teardown are safe', async t => {
    const previous = ['fetch', 'document'].map(name => Object.getOwnPropertyDescriptor(globalThis, name));
    t.after(() => ['fetch', 'document'].forEach((name, index) => previous[index] ? Object.defineProperty(globalThis, name, previous[index]) : delete globalThis[name]));
    globalThis.document = { documentElement: { dataset: { bsTheme: 'light' } }, querySelector: () => ({ value: 'csrf' }) };
    let calls = 0;
    globalThis.fetch = async () => { calls++; throw new Error('Offline'); };
    const preview = Object.defineProperties({ loaded: true, issues: [], payload: () => ({ widgets: [] }), t: key => key }, Object.getOwnPropertyDescriptors(createWidgetPreview()));
    t.after(() => preview.stopPreview());
    preview.previewHtml = '<html>Last good preview</html>';
    preview.issues = [{}];
    preview.schedulePreview();
    assert.equal(preview.previewState, 'invalid');
    assert.equal(calls, 0);
    preview.issues = [];
    preview.schedulePreview();
    await preview.refreshPreview();
    assert.equal(preview.previewState, 'failed');
    assert.equal(preview.previewHtml, '<html>Last good preview</html>');
    globalThis.fetch = async () => response('<html>Recovered</html>');
    preview.schedulePreview();
    await preview.refreshPreview();
    assert.equal(preview.previewState, 'ready');
    assert.equal(preview.previewHtml, '<html>Recovered</html>');
    let finish;
    globalThis.fetch = () => new Promise(resolve => finish = resolve);
    preview.schedulePreview();
    const loading = preview.refreshPreview();
    preview.stopPreview();
    finish(response('<html>After teardown</html>'));
    await loading;
    assert.equal(preview.previewHtml, '<html>Recovered</html>');
});

test('preview transport rejects empty or non-HTML success and retains structured validation errors', async t => {
    const previous = ['fetch', 'document'].map(name => Object.getOwnPropertyDescriptor(globalThis, name));
    t.after(() => ['fetch', 'document'].forEach((name, index) => previous[index] ? Object.defineProperty(globalThis, name, previous[index]) : delete globalThis[name]));
    globalThis.document = { documentElement: { dataset: {} }, querySelector: () => ({ value: 'csrf' }) };
    globalThis.fetch = async () => response('');
    await assert.rejects(previewWidgetCollection({ widgets: [] }), WidgetRequestError);
    globalThis.fetch = async () => ({ ok: true, status: 200, headers: new Headers({ 'Content-Type': 'application/json' }), json: async () => ({ widgets: [] }) });
    await assert.rejects(previewWidgetCollection({ widgets: [] }), WidgetRequestError);
    const problem = { errors: { 'Widgets[0].Title': ['Required'] } };
    globalThis.fetch = async () => ({ ok: false, status: 400, json: async () => problem });
    await assert.rejects(previewWidgetCollection({ widgets: [] }), error => error.status === 400 && error.problem === problem);
});

test('preview height follows document and image layout and disconnects previous document observers', t => {
    const previous = Object.getOwnPropertyDescriptor(globalThis, 'ResizeObserver');
    let callback;
    let disconnected = 0;
    globalThis.ResizeObserver = class {
        constructor(action) { callback = action; }
        observe(body) { assert.equal(typeof body.scrollHeight, 'number'); }
        disconnect() { disconnected++; }
    };
    t.after(() => previous ? Object.defineProperty(globalThis, 'ResizeObserver', previous) : delete globalThis.ResizeObserver);
    const preview = createWidgetPreview();
    const body = { scrollHeight: 400 };
    preview.measurePreview({ contentDocument: { body } });
    assert.equal(preview.previewHeight, 400);
    body.scrollHeight = 900;
    callback();
    assert.equal(preview.previewHeight, 900);
    preview.measurePreview({ contentDocument: { body: { scrollHeight: 80 } } });
    assert.equal(preview.previewHeight, 80);
    assert.equal(disconnected, 1);
    preview.stopPreview();
    assert.equal(disconnected, 2);
});
