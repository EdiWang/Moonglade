import assert from 'node:assert/strict';
import { registerHooks } from 'node:module';
import test from 'node:test';

const row = (id = 1) => ({ id: `10000000-0000-0000-0000-${String(id).padStart(12, '0')}`, title: `Widget ${id}`, widgetType: 'LinkList', region: 'AfterCategories', sortOrder: id - 1, isEnabled: true, contentCode: '[{"name":"GitHub","url":"/github"}]' });
const clone = value => JSON.parse(JSON.stringify(value));

test('actual composer controls draft persistence and recovery', async t => {
    const names = ['widgetComposerTest', 'window', 'document'];
    const previous = names.map(name => Object.getOwnPropertyDescriptor(globalThis, name));
    const state = { confirmation: null, writes: [], reads: 0, loaded: { widgets: [row(), row(2)] }, listeners: new Map() };
    globalThis.widgetComposerTest = state;
    globalThis.window = {
        addEventListener: (name, handler) => state.listeners.set(name, handler),
        removeEventListener: name => state.listeners.delete(name)
    };
    globalThis.document = { getElementById: () => ({ focus() {}, scrollIntoView() {} }), querySelector: () => null };
    const moduleUrl = new URL('../../../Moonglade.Web/wwwroot/js/app/admin.widgets.mjs', import.meta.url).href;
    const stubs = new Map([
        ['./alpine-init.mjs', 'export const Alpine = { data: (name, factory) => { globalThis.widgetComposerTest.factory = factory; } };'],
        ['./admin.widgets.api.mjs', 'export const loadWidgetCollection = () => globalThis.widgetComposerTest.load(); export const saveWidgetCollection = payload => globalThis.widgetComposerTest.save(payload);'],
        ['./utils.module.mjs', 'export const getLocalizedString = key => key;'],
        ['./adminModal.mjs', 'export const showConfirmModal = options => { globalThis.widgetComposerTest.confirmation = options; }; export const hideConfirmModal = () => {}; export const escapeHtml = value => value;']
    ]);
    const hooks = registerHooks({
        resolve(specifier, context, nextResolve) {
            if (context.parentURL === moduleUrl && stubs.has(specifier)) return { url: `data:text/javascript,${encodeURIComponent(stubs.get(specifier))}`, shortCircuit: true };
            return nextResolve(specifier, context);
        }
    });
    t.after(() => {
        hooks.deregister();
        names.forEach((name, index) => previous[index] ? Object.defineProperty(globalThis, name, previous[index]) : delete globalThis[name]);
    });
    await import(moduleUrl);
    const create = async (initialize = true) => {
        Object.assign(state, { confirmation: null, writes: [], reads: 0, loaded: { widgets: [row(), row(2)] } });
        state.load = async () => { state.reads++; return clone(state.loaded); };
        state.save = async payload => { state.writes.push(clone(payload)); state.loaded = clone(payload); return clone(payload); };
        const manager = state.factory();
        manager.$nextTick = action => action();
        manager.$refs = { widgetTitle: { focus() {} }, addWidget: { focus() {} }, addItem: { focus() {} } };
        if (initialize) await manager.init();
        return manager;
    };

    await t.test('loading and failed or incomplete reads keep mutation disabled', async () => {
        const manager = await create(false);
        assert.equal(manager.locked, true);
        assert.equal(manager.canSave, false);
        state.load = async () => { throw new Error('Network'); };
        await manager.init();
        manager.addWidget('LinkList');
        assert.equal(manager.loaded, false);
        assert.equal(manager.widgets.length, 0);
        assert.equal(manager.loadError, 'loadFailed');
        await manager.saveChanges();
        assert.equal(state.writes.length, 0);
        state.load = async () => ({});
        await manager.loadWidgets();
        assert.equal(manager.loaded, false);
        state.load = async () => ({ widgets: [] });
        await manager.loadWidgets();
        assert.equal(manager.loaded, true);
        assert.equal(manager.locked, false);
    });

    await t.test('selection, edits and disclosure never write; one Save submits the full collection', async () => {
        const manager = await create();
        const key = manager.selectedWidget.items[0].key;
        manager.selectedWidget.items[0].data.name = 'Edited';
        manager.chooseWidget(row(2).id);
        manager.selectedWidget.isEnabled = false;
        manager.chooseWidget(row().id);
        assert.equal(manager.selectedWidget.items[0].key, key);
        assert.equal(manager.selectedWidget.items[0].data.name, 'Edited');
        assert.equal(state.writes.length, 0);
        await manager.saveChanges();
        assert.equal(state.writes.length, 1);
        assert.equal(state.writes[0].widgets.length, 2);
        assert.equal(state.writes[0].widgets[1].isEnabled, false);
        assert.equal(manager.dirty, false);
        assert.equal(manager.saveState, 'saved');
    });

    await t.test('concurrent reload attempts cannot race and overwrite a later editable baseline', async () => {
        const manager = await create();
        let finish;
        let calls = 0;
        state.load = () => { calls++; return new Promise(resolve => finish = resolve); };
        const loading = manager.loadWidgets();
        await manager.loadWidgets();
        assert.equal(calls, 1);
        assert.equal(manager.locked, true);
        finish({ widgets: [row()] });
        await loading;
        manager.selectedWidget.title = 'Edited after the completed load';
        assert.equal(manager.dirty, true);
        assert.equal(manager.widgets.length, 1);
    });

    await t.test('type switch cancellation keeps content; confirmation changes only the draft', async () => {
        const manager = await create();
        const content = manager.payload().widgets[0].contentCode;
        const select = { value: 'ImageLink' };
        manager.changeType('ImageLink', select);
        assert.equal(select.value, 'LinkList');
        assert.equal(manager.payload().widgets[0].contentCode, content);
        await state.confirmation.onConfirm();
        assert.equal(manager.selectedWidget.widgetType, 'ImageLink');
        assert.equal(manager.selectedWidget.image.imageUrl, '');
        assert.equal(state.loaded.widgets[0].contentCode, content);
        assert.equal(state.writes.length, 0);
    });

    await t.test('discard confirmation restores staged deletion and all edits without saving', async () => {
        const manager = await create();
        manager.selectedWidget.title = 'Unsaved';
        manager.stageDeletion();
        assert.equal(manager.removedWidgets.length, 1);
        manager.discardChanges();
        assert.equal(manager.removedWidgets.length, 1);
        await state.confirmation.onConfirm();
        assert.equal(manager.removedWidgets.length, 0);
        assert.equal(manager.widgets[0].title, row().title);
        assert.equal(manager.dirty, false);
        assert.equal(state.writes.length, 0);
    });

    await t.test('client validation selects the affected widget and keeps the entire draft', async () => {
        const manager = await create();
        const created = manager.createWidget('ButtonLink');
        created.title = 'Button';
        manager.chooseWidget(row().id);
        await manager.saveChanges();
        assert.equal(manager.selectedId, created.id);
        assert.equal(manager.saveState, 'validation');
        assert.ok(manager.currentIssues.some(issue => issue.itemKey === created.items[0].key));
        assert.equal(state.writes.length, 0);
    });

    await t.test('server validation retains all draft edits and points to stable inline items', async () => {
        const manager = await create();
        manager.widgets[0].title = 'Edited first';
        manager.widgets[1].title = 'Edited second';
        state.save = async () => { throw { status: 400, problem: { errors: { 'Widgets[1].ContentCode[0].Url': ['Rejected URL'] } } }; };
        await manager.saveChanges();
        assert.equal(manager.selectedId, row(2).id);
        assert.equal(manager.fieldError('url', manager.selectedWidget.items[0].key), 'Rejected URL');
        assert.equal(manager.widgets[0].title, 'Edited first');
        assert.equal(manager.dirty, true);
        assert.equal(manager.uncertain, false);
    });

    await t.test('uncertain save requires confirmation before retry and reuses stable IDs', async () => {
        const manager = await create();
        manager.selectedWidget.title = 'Keep this draft';
        state.save = async payload => { state.writes.push(clone(payload)); throw new Error('Connection lost'); };
        await manager.saveChanges();
        assert.equal(manager.uncertain, true);
        assert.equal(manager.selectedWidget.title, 'Keep this draft');
        await manager.saveChanges();
        assert.equal(state.writes.length, 1);
        state.save = async payload => { state.writes.push(clone(payload)); return clone(payload); };
        await state.confirmation.onConfirm();
        assert.equal(state.writes.length, 2);
        assert.deepEqual(state.writes[0], state.writes[1]);
        assert.equal(manager.uncertain, false);
        assert.equal(manager.dirty, false);
    });

    await t.test('discard after an uncertain commit restores local data without claiming the server baseline is current', async () => {
        const manager = await create();
        manager.selectedWidget.title = 'Saved without acknowledgment';
        state.save = async payload => { state.writes.push(clone(payload)); state.loaded = clone(payload); throw new Error('Connection lost'); };
        await manager.saveChanges();
        manager.discardChanges();
        await state.confirmation.onConfirm();
        assert.equal(manager.selectedWidget.title, row().title);
        assert.equal(manager.dirty, false);
        assert.equal(manager.uncertain, true);
        assert.equal(manager.statusText, 'saveNotConfirmed');
        assert.equal(manager.saveMessage, 'saveUncertain');
        assert.equal(manager.canSave, false);
        assert.equal(state.writes.length, 1);
        await manager.loadWidgets();
        assert.equal(manager.selectedWidget.title, 'Saved without acknowledgment');
        assert.equal(manager.uncertain, false);
        assert.equal(manager.statusText, 'saved');
    });

    await t.test('server item paths follow retained raw order and edited order; image paths reach their field', async () => {
        const manager = await create();
        manager.acceptSnapshot({ widgets: [{ ...row(), contentCode: '[{"name":"Later","url":"/later","order":9},{"name":"First","url":"/first","order":0}]' }] });
        manager.applyServerErrors({ 'Widgets[0].ContentCode[0].Url': ['Original-order error'] }, manager.payload());
        assert.equal(manager.fieldError('url', manager.selectedWidget.items[1].key), 'Original-order error');
        manager.serverIssues = [];
        manager.selectedWidget.items[0].data.name = 'Edited';
        manager.applyServerErrors({ 'Widgets[0].ContentCode[0].Url': ['Edited-order error'] }, manager.payload());
        assert.equal(manager.fieldError('url', manager.selectedWidget.items[0].key), 'Edited-order error');
        manager.serverIssues = [];
        manager.acceptSnapshot({ widgets: [{ ...row(), widgetType: 'ImageLink', contentCode: '{"imageUrl":"/image.png"}' }] });
        manager.applyServerErrors({ 'Widgets[0].ContentCode.ImageUrl': ['Image error'] }, manager.payload());
        assert.equal(manager.fieldError('imageUrl'), 'Image error');
    });

    await t.test('known commit failure preserves draft, blocks another write and offers confirmed reload', async () => {
        const manager = await create();
        manager.selectedWidget.title = 'Saved remotely';
        state.save = async payload => { state.loaded = clone(payload); throw { status: 500, problem: { committed: true, savedCollection: clone(payload) } }; };
        await manager.saveChanges();
        assert.equal(manager.committed, true);
        assert.equal(manager.dirty, true);
        assert.equal(manager.canSave, false);
        assert.equal(manager.saveState, 'committed');
        let prevented = false;
        state.listeners.get('beforeunload')({ preventDefault() { prevented = true; } });
        assert.equal(prevented, false);
        await manager.loadWidgets();
        assert.equal(manager.committed, true);
        await state.confirmation.onConfirm();
        assert.equal(manager.committed, false);
        assert.equal(manager.selectedWidget.title, 'Saved remotely');
        assert.equal(manager.dirty, false);
    });

    await t.test('saving blocks duplicate submission and draft mutation while selection remains available', async () => {
        const manager = await create();
        manager.selectedWidget.title = 'Pending';
        let complete;
        state.save = payload => { state.writes.push(clone(payload)); return new Promise(resolve => complete = () => resolve(clone(payload))); };
        const save = manager.saveChanges();
        assert.equal(manager.isSaving, true);
        manager.addWidget('LinkList'); manager.stageDeletion();
        manager.chooseWidget(row(2).id);
        await manager.saveChanges();
        assert.equal(manager.widgets.length, 2);
        assert.equal(manager.selectedId, row(2).id);
        assert.equal(state.writes.length, 1);
        complete(); await save;
        assert.equal(manager.isSaving, false);
        assert.equal(manager.selectedId, row(2).id);
    });

    await t.test('drag controls move the authenticated local draft only and reject locked or foreign drops', async () => {
        const manager = await create();
        manager.widgets[0].items[0].data.name = 'Retained edit';
        const data = new Map();
        let prevented = false;
        const event = { preventDefault() { prevented = true; }, dataTransfer: {
            setData: (type, value) => data.set(type, value), getData: type => data.get(type)
        } };
        manager.startWidgetDrag(row(2).id, event);
        manager.hoverWidgetDrop('BeforeTags', null, event);
        manager.dropWidget('BeforeTags', null, event);
        assert.equal(manager.widgets[1].region, 'BeforeTags');
        assert.equal(manager.selectedId, row(2).id);
        assert.equal(manager.widgets[0].items[0].data.name, 'Retained edit');
        assert.equal(manager.draggingId, null);
        assert.equal(state.writes.length, 0);
        const before = manager.payload();
        manager.startWidgetDrag(row(2).id, event);
        data.clear();
        manager.dropWidget('AfterCategories', row().id, event);
        assert.deepEqual(manager.payload(), before);
        manager.isSaving = true;
        manager.startWidgetDrag(row().id, event);
        assert.equal(prevented, true);
        assert.equal(manager.draggingId, null);
    });

    await t.test('explicit removal of the last widget saves an empty collection', async () => {
        const manager = await create();
        manager.stageDeletion(); manager.stageDeletion();
        await manager.saveChanges();
        assert.deepEqual(state.writes[0], { widgets: [] });
        assert.equal(manager.loaded, true);
        assert.equal(manager.dirty, false);
    });

    await t.test('leave protection covers unsaved work and saving and is removed on teardown', async () => {
        const manager = await create();
        let prevented = false;
        const event = { preventDefault() { prevented = true; } };
        state.listeners.get('beforeunload')(event);
        assert.equal(prevented, false);
        manager.selectedWidget.title = 'Unsaved';
        state.listeners.get('beforeunload')(event);
        assert.equal(prevented, true);
        manager.destroy();
        assert.equal(state.listeners.has('beforeunload'), false);
    });
});
