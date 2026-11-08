import assert from 'node:assert/strict';
import { registerHooks } from 'node:module';
import test from 'node:test';

const moduleUrl = new URL('../../../Moonglade.Web/wwwroot/js/app/admin.activitylogs.mjs', import.meta.url).href;
const stubs = new Map([
    ['./alpine-init.mjs', `export const Alpine = { data: (name, factory) => { globalThis.b1Activity.factory = factory; globalThis.b1Activity.name = name; } };`],
    ['./httpService.mjs?v=1500', `export const fetch2 = (...args) => globalThis.b1Activity.fetch(...args);`],
    ['./utils.module.mjs', `export const formatUtcTime = () => globalThis.b1Activity.events.push('format');
        export const getLocalizedString = key => key;
        export const localDateBoundaryToUtcIso = () => { throw new Error('No date filter expected.'); };`],
    ['./toastService.mjs', `export const success = message => globalThis.b1Activity.events.push(['success', message]);
        export const error = error => globalThis.b1Activity.events.push(['error', error]);`],
    ['./adminModal.mjs', `export const showDeleteConfirmModal = (message, callback) => {
            globalThis.b1Activity.events.push(['confirm', message]);
            globalThis.b1Activity.confirm = callback;
        };
        export const hideConfirmModal = () => globalThis.b1Activity.events.push('hide');`]
]);

test('activityLogManager clear confirmation flow', async t => {
    const previous = Object.getOwnPropertyDescriptor(globalThis, 'b1Activity');
    globalThis.b1Activity = { events: [] };
    const hooks = registerHooks({
        resolve(specifier, context, nextResolve) {
            if (context.parentURL === moduleUrl && stubs.has(specifier))
                return { url: `data:text/javascript,${encodeURIComponent(stubs.get(specifier))}`, shortCircuit: true };
            return nextResolve(specifier, context);
        }
    });
    t.after(() => {
        hooks.deregister();
        if (previous) Object.defineProperty(globalThis, 'b1Activity', previous);
        else delete globalThis.b1Activity;
    });
    await import(moduleUrl);
    assert.equal(globalThis.b1Activity.name, 'activityLogManager');
    const createManager = () => {
        const events = [];
        Object.assign(globalThis.b1Activity, { events, confirm: null });
        const manager = globalThis.b1Activity.factory();
        manager.logs = [{ id: 11, operation: 'Historical post' }, { id: 12, operation: 'Historical comment' }];
        manager.totalRows = 35;
        manager.currentPage = 4;
        manager.selectedEventTypes = ['901'];
        manager.isLoading = false;
        manager.$nextTick = callback => callback();
        return { manager, events, originalLogs: manager.logs };
    };

    await t.test('cancelled clear keeps history and pagination without clear or refresh', () => {
        const { manager, events, originalLogs } = createManager();
        globalThis.b1Activity.fetch = () => assert.fail('Cancellation must not call any API.');

        manager.clearAllLogs();
        assert.deepEqual(events, [['confirm', 'confirmClearAllLogs']]);
        globalThis.b1Activity.confirm = null; // Dismiss the modal without invoking its confirmation callback.

        assert.strictEqual(manager.logs, originalLogs);
        assert.deepEqual(manager.logs.map(row => row.id), [11, 12]);
        assert.equal(manager.totalRows, 35);
        assert.equal(manager.currentPage, 4);
        assert.equal(manager.isLoading, false);
        assert.deepEqual(manager.selectedEventTypes, ['901']);
        assert.deepEqual(events, [['confirm', 'confirmClearAllLogs']]);
    });

    await t.test('confirmed clear waits for delete then reloads page one and reports success', async () => {
        const { manager, events, originalLogs } = createManager();
        let finishClear;
        const cleared = new Promise(resolve => { finishClear = resolve; });
        globalThis.b1Activity.fetch = async (url, method) => {
            events.push([method, url]);
            if (method === 'DELETE') {
                assert.equal(url, '/api/activitylog/clear');
                return cleared;
            }
            assert.equal(method, 'GET');
            const parsed = new URL(url, 'https://blog.example.invalid');
            assert.equal(parsed.pathname, '/api/activitylog/list');
            assert.equal(parsed.searchParams.get('pageIndex'), '1');
            assert.equal(parsed.searchParams.get('pageSize'), '10');
            assert.deepEqual(parsed.searchParams.getAll('eventTypes'), ['901']);
            return { items: [{ id: 50, operation: 'Clear Activity Logs' }], totalItemCount: 1 };
        };

        manager.clearAllLogs();
        const pending = globalThis.b1Activity.confirm();
        assert.strictEqual(manager.logs, originalLogs);
        assert.equal(manager.currentPage, 4);
        assert.deepEqual(events, [['confirm', 'confirmClearAllLogs'], ['DELETE', '/api/activitylog/clear']]);
        finishClear();
        await pending;

        assert.equal(manager.currentPage, 1);
        assert.equal(manager.totalRows, 1);
        assert.deepEqual(manager.logs, [{ id: 50, operation: 'Clear Activity Logs' }]);
        assert.equal(manager.isLoading, false);
        assert.deepEqual(events, [
            ['confirm', 'confirmClearAllLogs'],
            ['DELETE', '/api/activitylog/clear'],
            ['GET', '/api/activitylog/list?pageIndex=1&pageSize=10&eventTypes=901'],
            'format',
            ['success', 'logsCleared'],
            'hide'
        ]);
    });

    await t.test('failed clear preserves history and page and surfaces error without success', async () => {
        const { manager, events, originalLogs } = createManager();
        const failure = new Error('Synthetic clear failed');
        globalThis.b1Activity.fetch = async (url, method) => {
            events.push([method, url]);
            assert.equal(method, 'DELETE');
            assert.equal(url, '/api/activitylog/clear');
            throw failure;
        };

        manager.clearAllLogs();
        await globalThis.b1Activity.confirm();

        assert.strictEqual(manager.logs, originalLogs);
        assert.deepEqual(manager.logs.map(row => row.id), [11, 12]);
        assert.equal(manager.totalRows, 35);
        assert.equal(manager.currentPage, 4);
        assert.equal(manager.isLoading, false);
        assert.deepEqual(manager.selectedEventTypes, ['901']);
        assert.deepEqual(events, [
            ['confirm', 'confirmClearAllLogs'],
            ['DELETE', '/api/activitylog/clear'],
            ['error', failure],
            'hide'
        ]);
    });
});
