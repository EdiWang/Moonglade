import assert from 'node:assert/strict';
import test from 'node:test';
import { loadWidgetCollection, saveWidgetCollection, WidgetRequestError } from '../../../Moonglade.Web/wwwroot/js/app/admin.widgets.api.mjs';

test('widget transport retains structured outcome and field details', async t => {
    const previousFetch = globalThis.fetch;
    const previousDocument = Object.getOwnPropertyDescriptor(globalThis, 'document');
    globalThis.document = { querySelector: () => ({ value: 'synthetic-csrf' }) };
    t.after(() => { globalThis.fetch = previousFetch; if (previousDocument) Object.defineProperty(globalThis, 'document', previousDocument); else delete globalThis.document; });

    await t.test('uses the collection URL, credentials and actual antiforgery field', async () => {
        const calls = [];
        globalThis.fetch = async (...args) => { calls.push(args); return { ok: true, status: 200, json: async () => ({ widgets: [] }) }; };
        await loadWidgetCollection();
        await saveWidgetCollection({ widgets: [] });
        assert.equal(calls[0][0], '/api/widgets');
        assert.equal(calls[0][1].method, 'GET');
        assert.equal(calls[0][1].body, null);
        assert.equal(calls[1][1].method, 'PUT');
        assert.equal(calls[1][1].credentials, 'include');
        assert.equal(calls[1][1].headers['XSRF-TOKEN'], 'synthetic-csrf');
        assert.deepEqual(JSON.parse(calls[1][1].body), { widgets: [] });
    });
    await t.test('HTTP field errors and committed state remain available to recovery UI', async () => {
        for (const [status, problem] of [[400, { errors: { 'Widgets[0].Title': ['Required'] } }], [500, { committed: true, savedCollection: { widgets: [] } }]]) {
            globalThis.fetch = async () => ({ ok: false, status, json: async () => problem });
            await assert.rejects(saveWidgetCollection({ widgets: [] }), error => error instanceof WidgetRequestError && error.status === status && error.problem === problem);
        }
    });
    await t.test('malformed success responses and network errors do not become successful empty saves', async () => {
        globalThis.fetch = async () => ({ ok: true, status: 200, json: async () => { throw new Error('Not JSON'); } });
        await assert.rejects(saveWidgetCollection({ widgets: [] }), error => error instanceof WidgetRequestError && error.status === 200);
        globalThis.fetch = async () => { throw new TypeError('Connection lost'); };
        await assert.rejects(saveWidgetCollection({ widgets: [] }), TypeError);
    });
});
