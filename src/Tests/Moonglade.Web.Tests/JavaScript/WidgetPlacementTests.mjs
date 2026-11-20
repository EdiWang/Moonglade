import assert from 'node:assert/strict';
import { registerHooks } from 'node:module';
import test from 'node:test';

test('widget manager sends and validates explicit placement', async t => {
    const names = ['widgetPlacementTest', 'window', 'alert'];
    const previous = names.map(name => Object.getOwnPropertyDescriptor(globalThis, name));
    globalThis.widgetPlacementTest = {};
    globalThis.window = { emptyGuid: '00000000-0000-0000-0000-000000000000' };
    globalThis.alert = message => globalThis.widgetPlacementTest.alerts.push(message);
    const moduleUrl = new URL('../../../Moonglade.Web/wwwroot/js/app/admin.widgets.mjs', import.meta.url).href;
    const stubs = new Map([
        ['./alpine-init.mjs', 'export const Alpine = { data: (name, factory) => { globalThis.widgetPlacementTest.factory = factory; } };'],
        ['./httpService.mjs?v=1500', 'export const fetch2 = (...args) => globalThis.widgetPlacementTest.fetch(...args);'],
        ['./utils.module.mjs', 'export const getLocalizedString = key => key;'],
        ['./toastService.mjs', 'export const success = () => {}; export const error = error => { throw error; };'],
        ['./adminModal.mjs', 'export const showDeleteConfirmModal = () => {}; export const hideConfirmModal = () => {};'],
        ['./admin.widgets.render.mjs', 'export const renderWidgetContent = () => "";'],
        ['./admin.widgets.links.mjs', 'export const createLinkMixin = () => ({});'],
        ['./admin.widgets.buttons.mjs', 'export const createButtonMixin = () => ({});'],
        ['./admin.offcanvas.mjs', 'export const createOffcanvas = () => ({});']
    ]);
    const hooks = registerHooks({
        resolve(specifier, context, nextResolve) {
            if (context.parentURL === moduleUrl && stubs.has(specifier))
                return { url: `data:text/javascript,${encodeURIComponent(stubs.get(specifier))}`, shortCircuit: true };
            return nextResolve(specifier, context);
        }
    });
    t.after(() => {
        hooks.deregister();
        names.forEach((name, index) => {
            if (previous[index]) Object.defineProperty(globalThis, name, previous[index]);
            else delete globalThis[name];
        });
    });
    await import(moduleUrl);
    const createManager = () => {
        const state = globalThis.widgetPlacementTest;
        state.alerts = [];
        state.calls = [];
        state.fetch = async (...args) => { state.calls.push(args); return []; };
        const manager = state.factory();
        manager.editCanvas = { show() {}, hide() {} };
        manager.formData.title = 'Fixture';
        return manager;
    };

    await t.test('orders above the former band capacity are sent with a region', async () => {
        const manager = createManager();
        manager.formData.region = 'BeforeProfile';
        manager.formData.sortOrder = 1000;

        await manager.handleSubmit();

        assert.deepEqual(globalThis.widgetPlacementTest.calls[0], ['/api/widgets', 'POST', {
            title: 'Fixture', widgetType: 'LinkList', region: 'BeforeProfile', sortOrder: 1000,
            isEnabled: true, contentCode: '[]'
        }]);
        assert.deepEqual(globalThis.widgetPlacementTest.alerts, []);
    });

    await t.test('invalid region and non-integer or out-of-range order never write', async () => {
        for (const [region, sortOrder] of [['Unknown', 0], ['BeforeTags', -1], ['BeforeTags', 1.5], ['BeforeTags', NaN], ['BeforeTags', 2147483648]]) {
            const manager = createManager();
            Object.assign(manager.formData, { region, sortOrder });
            await manager.handleSubmit();
            assert.equal(globalThis.widgetPlacementTest.calls.length, 0);
            assert.equal(globalThis.widgetPlacementTest.alerts.length, 1);
        }
    });

    await t.test('editing loads the persisted region and region-local order', async () => {
        const manager = createManager();
        globalThis.widgetPlacementTest.fetch = async () => ({
            id: '10000000-0000-0000-0000-000000000001', title: 'Stored', widgetType: 'LinkList',
            region: 'BeforeCategories', sortOrder: 12, isEnabled: false, contentCode: '[]'
        });

        await manager.editWidget('10000000-0000-0000-0000-000000000001', null);

        assert.equal(manager.formData.region, 'BeforeCategories');
        assert.equal(manager.formData.sortOrder, 12);
        assert.equal(manager.formData.isEnabled, false);
    });
});
