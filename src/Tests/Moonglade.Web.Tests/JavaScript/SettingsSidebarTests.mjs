import assert from 'node:assert/strict';
import { registerHooks } from 'node:module';
import test from 'node:test';

const moduleUrl = new URL('../../../Moonglade.Web/wwwroot/js/app/admin.settings.general.mjs', import.meta.url).href;
const settingsUrl = new URL('../../../Moonglade.Web/wwwroot/js/app/admin.settings.mjs', import.meta.url).href;

test('sidebar settings coordinate the existing enum field', async t => {
    const globals = ['settingsRegression', 'document', 'bootstrap'];
    const previous = new Map(globals.map(name => [name, Object.getOwnPropertyDescriptor(globalThis, name)]));
    const bridge = globalThis.settingsRegression = {};
    const hooks = registerHooks({
        resolve(specifier, context, nextResolve) {
            let source;
            if (context.parentURL?.startsWith(settingsUrl)) {
                if (specifier === './httpService.mjs?v=1500')
                    source = 'export const fetch2 = (...args) => globalThis.settingsRegression.fetch(...args);';
                if (specifier === './toastService.mjs')
                    source = 'export const success = message => globalThis.settingsRegression.successes.push(message);';
            }
            if (context.parentURL?.startsWith(moduleUrl) && specifier === './imageuploader.mjs')
                source = 'export class ImageUploader { bindEvents() {} }';
            if (source) return { url: `data:text/javascript,${encodeURIComponent(source)}`, shortCircuit: true };
            return nextResolve(specifier, context);
        }
    });
    t.after(() => {
        hooks.deregister();
        for (const [name, descriptor] of previous) {
            if (descriptor) Object.defineProperty(globalThis, name, descriptor);
            else delete globalThis[name];
        }
    });

    let fixtureId = 0;
    const createFixture = async (option = 'Right') => {
        bridge.successes = [];
        const calls = [];
        bridge.fetch = async (...args) => { calls.push(args); };
        const element = () => ({
            hidden: true,
            disabled: false,
            dataset: {},
            listeners: {},
            addEventListener(name, listener) { this.listeners[name] = listener; },
            setAttribute() {},
            hasAttribute() { return false; },
            focus() { document.activeElement = this; }
        });
        const messages = [];
        const row = { append: message => messages.push(message) };
        const toggle = Object.assign(element(), {
            name: 'SideBarOption', type: 'checkbox', checked: option !== 'Disabled'
        });
        const position = Object.assign(element(), {
            name: 'ViewModel.SideBarOption', type: 'select-one', value: option === 'Disabled' ? 'Right' : option
        });
        const profile = Object.assign(element(), {
            name: 'WidgetsProfile', type: 'checkbox', checked: true
        });
        const controls = [toggle, position, profile];
        for (const control of controls) control.closest = () => row;
        const form = Object.assign(element(), {
            action: '/api/settings/general',
            querySelectorAll: selector => selector === '[data-setting-edit]' ? [] : controls
        });
        const dialogForm = Object.assign(element(), { querySelector: () => element() });
        const dialog = Object.assign(element(), {
            dataset: { updated: 'updated' },
            querySelector: selector => selector === 'form' ? dialogForm : element()
        });
        const nodes = new Map([
            ['form-settings', form],
            ['setting-edit-modal', dialog],
            ['settings-show-sidebar', toggle],
            ['ViewModel_SideBarOption', position]
        ]);
        globalThis.document = {
            body: {},
            getElementById: id => nodes.get(id) ?? element(),
            createElement: element
        };
        document.activeElement = document.body;
        globalThis.bootstrap = { Modal: class {} };
        await import(`${moduleUrl}?fixture=${++fixtureId}`);
        return {
            calls, toggle, position, profile, messages,
            change: control => form.listeners.change({ target: control })
        };
    };

    await t.test('position saves Left without changing sidebar visibility', async () => {
        const fixture = await createFixture();
        await fixture.change(fixture.position);
        assert.deepEqual(fixture.calls, []);

        fixture.position.value = 'Left';
        await fixture.change(fixture.position);

        assert.deepEqual(fixture.calls, [['/api/settings/general', 'PATCH', { SideBarOption: 'Left' }]]);
        assert.equal(fixture.toggle.checked, true);
        assert.equal(fixture.position.value, 'Left');
    });

    await t.test('disabled sidebar allows choosing a position without saving until enabled', async () => {
        const fixture = await createFixture('Disabled');
        await fixture.change(fixture.toggle);
        fixture.position.value = 'Left';
        await fixture.change(fixture.position);
        assert.deepEqual(fixture.calls, []);
        assert.equal(fixture.toggle.checked, false);

        fixture.toggle.checked = true;
        await fixture.change(fixture.toggle);

        assert.deepEqual(fixture.calls, [['/api/settings/general', 'PATCH', { SideBarOption: 'Left' }]]);
        assert.equal(fixture.position.value, 'Left');
        assert.equal(fixture.toggle.checked, true);
    });

    await t.test('disable and enable retain the selected position on the same page', async () => {
        const fixture = await createFixture('Left');
        fixture.toggle.checked = false;
        await fixture.change(fixture.toggle);
        assert.equal(fixture.toggle.checked, false);
        assert.equal(fixture.position.value, 'Left');

        fixture.toggle.checked = true;
        await fixture.change(fixture.toggle);

        assert.deepEqual(fixture.calls, [
            ['/api/settings/general', 'PATCH', { SideBarOption: 'Disabled' }],
            ['/api/settings/general', 'PATCH', { SideBarOption: 'Left' }]
        ]);
        assert.equal(fixture.toggle.checked, true);
        assert.equal(fixture.position.value, 'Left');
    });

    await t.test('position saves restore focus to the dropdown instead of the checked switch', async () => {
        const fixture = await createFixture();
        fixture.position.focus();
        fixture.position.value = 'Left';
        bridge.fetch = async (...args) => {
            fixture.calls.push(args);
            document.activeElement = document.body;
        };

        await fixture.change(fixture.position);

        assert.equal(document.activeElement, fixture.position);
        assert.equal(fixture.position.disabled, false);
        assert.deepEqual(fixture.calls, [['/api/settings/general', 'PATCH', { SideBarOption: 'Left' }]]);
    });

    await t.test('failed saves restore both controls and lock them while saving', async () => {
        const fixture = await createFixture('Left');
        const pending = Promise.withResolvers();
        bridge.fetch = (...args) => {
            fixture.calls.push(args);
            return pending.promise;
        };
        fixture.toggle.checked = false;
        const saving = fixture.change(fixture.toggle);
        assert.equal(fixture.toggle.disabled, true);
        assert.equal(fixture.position.disabled, true);
        pending.reject(new Error('Save rejected'));
        await saving;

        assert.equal(fixture.toggle.checked, true);
        assert.equal(fixture.position.value, 'Left');
        assert.equal(fixture.toggle.disabled, false);
        assert.equal(fixture.position.disabled, false);
        assert.equal(fixture.messages.at(-1).hidden, false);
        assert.match(String(fixture.messages.at(-1).textContent), /Save rejected/);
        assert.deepEqual(bridge.successes, []);

        fixture.position.value = 'Right';
        bridge.fetch = async (...args) => {
            fixture.calls.push(args);
            throw new Error('Position rejected');
        };
        await fixture.change(fixture.position);
        assert.equal(fixture.toggle.checked, true);
        assert.equal(fixture.position.value, 'Left');
        assert.deepEqual(fixture.calls, [
            ['/api/settings/general', 'PATCH', { SideBarOption: 'Disabled' }],
            ['/api/settings/general', 'PATCH', { SideBarOption: 'Right' }]
        ]);
    });

    await t.test('unbound widget switches retain their existing boolean save and rollback', async () => {
        const fixture = await createFixture();
        fixture.profile.checked = false;
        await fixture.change(fixture.profile);
        assert.deepEqual(fixture.calls, [['/api/settings/general', 'PATCH', { WidgetsProfile: false }]]);
        assert.equal(fixture.profile.checked, false);

        bridge.fetch = async () => { throw new Error('Widget rejected'); };
        fixture.profile.checked = true;
        await fixture.change(fixture.profile);
        assert.equal(fixture.profile.checked, false);
        assert.equal(fixture.profile.disabled, false);
        assert.equal(fixture.toggle.checked, true);
        assert.equal(fixture.position.value, 'Right');
    });
});
