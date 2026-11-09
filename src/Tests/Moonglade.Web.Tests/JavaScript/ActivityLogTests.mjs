import assert from 'node:assert/strict';
import { registerHooks } from 'node:module';
import test from 'node:test';

const moduleUrl = new URL('../../../Moonglade.Web/wwwroot/js/app/admin.activitylogs.mjs', import.meta.url).href;
const stubs = new Map([
    ['./alpine-init.mjs', `export const Alpine = { data: (name, factory) => { globalThis.b1Activity.factory = factory; globalThis.b1Activity.name = name; } };`],
    ['./httpService.mjs?v=1500', `export const fetch2 = (...args) => globalThis.b1Activity.fetch(...args);`],
    ['./utils.module.mjs', `export const formatUtcTime = () => globalThis.b1Activity.events.push('format');
        export const getLocalizedString = key => globalThis.b1Activity.localized?.[key] ?? key;
        export const localDateBoundaryToUtcIso = (...args) => globalThis.b1Activity.date(...args);`],
    ['./toastService.mjs', `export const success = message => globalThis.b1Activity.events.push(['success', message]);
        export const error = error => globalThis.b1Activity.events.push(['error', error]);`],
    ['./adminModal.mjs', `export const showConfirmModal = options => {
            globalThis.b1Activity.events.push(['confirm', options.body]);
            globalThis.b1Activity.confirmOptions = options;
            globalThis.b1Activity.confirm = options.onConfirm;
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
        Object.assign(globalThis.b1Activity, { events, confirm: null, confirmOptions: null, localized: null });
        const manager = globalThis.b1Activity.factory();
        manager.logs = [{ id: 11, summary: 'Historical post' }, { id: 12, summary: 'Historical comment' }];
        manager.totalRows = 35;
        manager.currentPage = 4;
        manager.selectedEventKeys = ['activity-log.cleared'];
        manager.isLoading = false;
        manager.$nextTick = callback => callback();
        return { manager, events, originalLogs: manager.logs };
    };

    await t.test('localized clear confirmation uses page title action and body with unchanged danger presentation', () => {
        for (const localized of [
            { clearAll: '清除全部', confirmClearAllLogs: '确定要清除全部活动日志吗？此操作将被记录。' },
            { clearAll: 'Alle löschen', confirmClearAllLogs: 'Alle Aktivitätsprotokolle löschen? Diese Aktion wird protokolliert.' }
        ]) {
            const { manager, events, originalLogs } = createManager();
            globalThis.b1Activity.localized = localized;
            globalThis.b1Activity.fetch = () => assert.fail('Opening localized confirmation must not call any API.');

            manager.clearAllLogs();

            const options = globalThis.b1Activity.confirmOptions;
            assert.deepEqual(Object.keys(options).sort(), ['body', 'confirmClass', 'confirmIcon', 'confirmText', 'onConfirm', 'title']);
            assert.equal(options.title, localized.clearAll);
            assert.equal(options.confirmText, localized.clearAll);
            assert.equal(options.body, localized.confirmClearAllLogs);
            assert.equal(options.confirmClass, 'btn-outline-danger');
            assert.equal(options.confirmIcon, 'bi-trash');
            assert.equal(typeof options.onConfirm, 'function');
            assert.strictEqual(globalThis.b1Activity.confirm, options.onConfirm);
            assert.strictEqual(manager.logs, originalLogs);
            assert.equal(manager.currentPage, 4);
            assert.equal(manager.totalRows, 35);
            assert.deepEqual(events, [['confirm', localized.confirmClearAllLogs]]);
        }
    });

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
        assert.deepEqual(manager.selectedEventKeys, ['activity-log.cleared']);
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
            assert.deepEqual(parsed.searchParams.getAll('eventKeys'), ['activity-log.cleared']);
            return { items: [{ id: 50, summary: 'Activity log cleared' }], totalItemCount: 1 };
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
        assert.deepEqual(manager.logs, [{ id: 50, summary: 'Activity log cleared' }]);
        assert.equal(manager.isLoading, false);
        assert.deepEqual(events, [
            ['confirm', 'confirmClearAllLogs'],
            ['DELETE', '/api/activitylog/clear'],
            ['GET', '/api/activitylog/list?pageIndex=1&pageSize=10&eventKeys=activity-log.cleared'],
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
        assert.deepEqual(manager.selectedEventKeys, ['activity-log.cleared']);
        assert.deepEqual(events, [
            ['confirm', 'confirmClearAllLogs'],
            ['DELETE', '/api/activitylog/clear'],
            ['error', failure],
            'hide'
        ]);
    });

    await t.test('string event keys serialize repeatedly and server summary is used without reconstruction', async () => {
        const { manager, events } = createManager();
        manager.selectedEventKeys = ['post.created', 'account.authenticator.reset'];
        manager.startDate = '2026-03-01';
        manager.endDate = '2026-03-02';
        const dateCalls = [];
        globalThis.b1Activity.date = (date, end = false) => {
            dateCalls.push([date, end]);
            return end ? '2026-03-02T23:59:59.9999999Z' : '2026-03-01T00:00:00.000Z';
        };
        const items = [{ id: 81, eventKey: 'post.created', actor: 'System', actorKind: 'Administrator',
            summary: 'Localized server sentence: <captured title>', eventTimeUtc: '2026-03-01T00:00:00Z' }];
        globalThis.b1Activity.fetch = async (url, method) => {
            assert.equal(method, 'GET');
            const parsed = new URL(url, 'https://blog.example.invalid');
            assert.deepEqual(parsed.searchParams.getAll('eventKeys'), ['post.created', 'account.authenticator.reset']);
            assert.equal(parsed.searchParams.get('pageIndex'), '4');
            assert.equal(parsed.searchParams.get('pageSize'), '10');
            assert.equal(parsed.searchParams.get('startTimeUtc'), '2026-03-01T00:00:00.000Z');
            assert.equal(parsed.searchParams.get('endTimeUtc'), '2026-03-02T23:59:59.9999999Z');
            assert.equal(parsed.searchParams.has('eventTypes'), false);
            return { items, totalItemCount: 81 };
        };
        await manager.loadData();
        assert.strictEqual(manager.logs, items);
        assert.equal(manager.logs[0].summary, 'Localized server sentence: <captured title>');
        assert.equal(manager.logs[0].actorKind, 'Administrator');
        assert.equal(manager.totalRows, 81);
        assert.equal(manager.isLoading, false);
        assert.deepEqual(dateCalls, [['2026-03-01', false], ['2026-03-02', true]]);
        assert.deepEqual(events, ['format']);
    });

    await t.test('key toggling filter reset and page changes preserve exact active filters', async t => {
        const { manager, events } = createManager();
        const previousDocument = Object.getOwnPropertyDescriptor(globalThis, 'document');
        globalThis.document = {
            querySelector: selector => {
                assert.equal(selector, '.admin-main-body');
                return { scrollTo: (x, y) => events.push(['scroll', x, y]) };
            }
        };
        t.after(() => {
            if (previousDocument) Object.defineProperty(globalThis, 'document', previousDocument);
            else delete globalThis.document;
        });
        manager.filterCanvas = { hide: () => events.push('filter-hide') };
        manager.updateUrl = () => events.push(['page-url', manager.currentPage]);
        manager.toggleEventKey('post.created');
        assert.deepEqual(manager.selectedEventKeys, ['activity-log.cleared', 'post.created']);
        manager.toggleEventKey('activity-log.cleared');
        assert.deepEqual(manager.selectedEventKeys, ['post.created']);
        assert.equal(manager.activeFilterCount, 1);
        const calls = [];
        globalThis.b1Activity.fetch = async url => {
            const query = new URL(url, 'https://blog.example.invalid').searchParams;
            calls.push({ page: query.get('pageIndex'), keys: query.getAll('eventKeys') });
            return { items: [{ id: 1, summary: 'Captured' }], totalItemCount: 25 };
        };
        await manager.handleFilter();
        assert.equal(manager.currentPage, 1);
        await manager.goToPage(2);
        assert.equal(manager.currentPage, 2);
        manager.startDate = '2026-01-01';
        manager.endDate = '2026-01-02';
        assert.equal(manager.activeFilterCount, 3);
        await manager.clearFilter();
        assert.deepEqual(calls, [{ page: '1', keys: ['post.created'] }, { page: '2', keys: ['post.created'] }, { page: '1', keys: [] }]);
        assert.equal(manager.currentPage, 1);
        assert.equal(manager.startDate, '');
        assert.equal(manager.endDate, '');
        assert.deepEqual(manager.selectedEventKeys, []);
        assert.equal(manager.activeFilterCount, 0);
        assert.equal(events.filter(item => item === 'filter-hide').length, 2);
    });

    await t.test('detail opens the offcanvas and restores focus when closed', async () => {
        const { manager, events } = createManager();
        const detail = { id: 42, eventKey: 'post.created', heading: 'Localized heading',
            targetId: 'snapshot-id', targetDisplayName: '<title>', ipAddress: '192.0.2.50', userAgent: '<agent>',
            fields: [{ key: 'PostStatus', label: 'Status', value: 'Published' }] };
        manager.detailCanvas = { show: () => events.push('detail-show') };
        manager.$refs = { detailCanvas: new EventTarget() };
        globalThis.b1Activity.fetch = async (url, method) => {
            assert.equal(url, '/api/activitylog/42/metadata');
            assert.equal(method, 'GET');
            assert.equal(manager.currentDetail, null);
            return detail;
        };
        await manager.showDetail(42, { focus: () => events.push('detail-focus') });
        manager.$refs.detailCanvas.dispatchEvent(new Event('hidden.bs.offcanvas'));
        assert.strictEqual(manager.currentDetail, detail);
        assert.equal(manager.currentDetail.heading, 'Localized heading');
        assert.equal(manager.hasDetailContext, true);
        assert.equal(manager.formatDetailValue('Published', 'PostStatus'), 'published');
        assert.equal(manager.formatDetailValue('Draft', 'PostStatus'), 'draft');
        assert.equal(manager.formatDetailValue('Scheduled', 'PostStatus'), 'scheduled');
        assert.equal(manager.formatDetailValue('Published', 'FileName'), 'Published');
        assert.equal(manager.formatDetailValue('Unknown', 'PostStatus'), 'Unknown');
        assert.equal(manager.formatDetailValue(true, 'IsEnabled'), 'yes');
        assert.equal(manager.formatDetailValue(false, 'IsEnabled'), 'no');
        assert.equal(manager.formatDetailValue(12.75, 'FileSize'), '12.75');
        assert.equal(manager.formatDetailValue(null, 'Slug'), '\u2014');
        assert.equal(manager.formatDetailValue(undefined, 'Slug'), '\u2014');
        assert.equal(manager.formatDetailValue(['<script>', 7, false, null], 'CommentIds'), '<script>, 7, no, \u2014');
        assert.equal(manager.formatDetailValue('<script>alert(1)</script>', 'Slug'), '<script>alert(1)</script>');
        assert.deepEqual(events, ['detail-show', 'format', 'detail-focus']);
    });

    await t.test('empty details remain a successful detail and context detection distinguishes each optional field', async () => {
        const { manager } = createManager();
        manager.detailCanvas = { show() {} };
        globalThis.b1Activity.fetch = async () => ({ id: 44, heading: 'Post created', fields: [] });
        await manager.showDetail(44);
        assert.equal(manager.currentDetail.id, 44);
        assert.deepEqual(manager.currentDetail.fields, []);
        assert.equal(manager.hasDetailContext, false);
        for (const key of ['targetId', 'targetDisplayName', 'ipAddress', 'userAgent']) {
            manager.currentDetail = { fields: [], [key]: 'context' };
            assert.equal(manager.hasDetailContext, true, key);
        }
        manager.currentDetail = { targetId: null, targetDisplayName: '', ipAddress: null, userAgent: '', fields: [] };
        assert.equal(manager.hasDetailContext, false);
        manager.currentDetail = null;
        assert.equal(manager.hasDetailContext, false);
    });

    await t.test('failed detail drops stale values and reports error without opening modal', async () => {
        const { manager, events } = createManager();
        const failure = new Error('Synthetic detail failed');
        manager.currentDetail = { id: 1, heading: 'Stale' };
        manager.detailCanvas = { show: () => assert.fail('Failed detail must not open the offcanvas.') };
        globalThis.b1Activity.fetch = async () => { throw failure; };
        await manager.showDetail(55);
        assert.equal(manager.currentDetail, null);
        assert.deepEqual(events, [['error', failure]]);
    });

    await t.test('B3 settings changes retain typed old and new values as plain text', async () => {
        const { manager, events } = createManager();
        const changes = [
            { key: 'SiteTitle', label: 'Title', oldValue: '<old>', newValue: '<script>alert(1)</script>' },
            { key: 'EnableComments', label: 'Comments', oldValue: true, newValue: false },
            { key: 'FeedItemCount', label: 'Feed items', oldValue: 0, newValue: 2147483647 },
            { key: 'SiteTitle', label: 'Nullable title', oldValue: null, newValue: '' }
        ];
        manager.detailCanvas = { show: () => events.push('detail-show') };
        globalThis.b1Activity.fetch = async () => ({ id: 101, fields: [], changes });

        await manager.showDetail(101);

        assert.equal(manager.hasSettingsChanges, true);
        assert.strictEqual(manager.currentDetail.changes, changes);
        assert.deepEqual(changes.map(change => [
            manager.formatDetailValue(change.oldValue, change.key),
            manager.formatDetailValue(change.newValue, change.key)
        ]), [
            ['<old>', '<script>alert(1)</script>'],
            ['yes', 'no'],
            ['0', '2147483647'],
            ['\u2014', '']
        ]);
        assert.deepEqual(events, ['detail-show', 'format']);
    });

    await t.test('empty changes are intentional while non-settings details have no change table', () => {
        const { manager } = createManager();
        manager.currentDetail = { fields: [], changes: [] };
        assert.equal(manager.hasSettingsChanges, true);
        assert.equal(manager.currentDetail.changes.length, 0);
        assert.equal(manager.hasDetailContext, false);
        for (const detail of [
            { fields: [{ key: 'SiteTitle', value: 'Legacy' }], changes: null },
            { fields: [] },
            { changes: {} },
            { changes: '' },
            null
        ]) {
            manager.currentDetail = detail;
            assert.equal(manager.hasSettingsChanges, false);
        }
    });

    await t.test('all six settings enum discriminators localize every approved choice without rewriting ordinary strings', () => {
        const { manager } = createManager();
        const cases = [
            ['SideBarOption', 'Right', 'right'], ['SideBarOption', 'Left', 'left'], ['SideBarOption', 'Disabled', 'disabled'],
            ['DefaultEditor', 'Html', 'html'], ['DefaultEditor', 'Markdown', 'markdown'],
            ['PostTitleAlignment', 'Left', 'left'], ['PostTitleAlignment', 'Center', 'center'],
            ['CommentProvider', 'BuiltIn', 'builtIn'], ['CommentProvider', 'ThirdParty', 'thirdParty'],
            ['CommentOrder', 'OldToNew', 'oldToNew'], ['CommentOrder', 'NewToOld', 'newToOld'],
            ['WordFilterMode', 'Mask', 'maskWord'], ['WordFilterMode', 'Block', 'blockComment']
        ];
        for (const [key, value, label] of cases) {
            assert.equal(manager.formatDetailValue(value, key), label, `${key} approved choice`);
            assert.equal(manager.formatDetailValue(value, 'SiteTitle'), value, `${key} ordinary string`);
            assert.equal(manager.formatDetailValue('Unknown', key), 'Unknown', `${key} unknown choice`);
        }
        assert.equal(manager.formatDetailValue('Right', 'DefaultEditor'), 'Right');
        assert.equal(manager.formatDetailValue('Markdown', 'CommentProvider'), 'Markdown');
    });

    await t.test('failed list retains existing history and page and empty successful list clears it', async () => {
        const { manager, events, originalLogs } = createManager();
        const failure = new Error('Synthetic list failed');
        globalThis.b1Activity.fetch = async () => { throw failure; };
        await manager.loadData();
        assert.strictEqual(manager.logs, originalLogs);
        assert.equal(manager.currentPage, 4);
        assert.equal(manager.totalRows, 35);
        assert.equal(manager.isLoading, false);
        assert.deepEqual(events, [['error', failure]]);
        globalThis.b1Activity.fetch = async () => ({ items: [], totalItemCount: 0 });
        await manager.loadData();
        assert.deepEqual(manager.logs, []);
        assert.equal(manager.totalRows, 0);
        assert.equal(manager.hasLogs, false);
        assert.equal(manager.isLoading, false);
    });
});
