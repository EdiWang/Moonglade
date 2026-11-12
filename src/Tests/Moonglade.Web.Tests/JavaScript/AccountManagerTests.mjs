import assert from 'node:assert/strict';
import { registerHooks } from 'node:module';
import test from 'node:test';

const moduleUrl = new URL('../../../Moonglade.Web/wwwroot/js/app/admin.localaccount.mjs', import.meta.url).href;
const stubs = new Map([
    ['./alpine-init.mjs', `export const Alpine = { data: (name, factory) => {
        globalThis.accountRegression.name = name;
        globalThis.accountRegression.factory = factory;
    } };`],
    ['./httpService.mjs?v=1500', `export const fetch2 = (...args) => {
        globalThis.accountRegression.calls.push(args);
        return globalThis.accountRegression.fetch(...args);
    };`],
    ['./toastService.mjs', `export const success = message => globalThis.accountRegression.events.push(['success', message]);
        export const error = failure => globalThis.accountRegression.events.push(['error', failure]);`],
    ['./imageuploader.mjs', 'export class ImageUploader { bindEvents() {} }'],
    ['./utils.module.mjs', `export const getLocalizedString = key => key;`]
]);
const originalRevision = '11111111-1111-1111-1111-111111111111';
const freshRevision = '22222222-2222-2222-2222-222222222222';
const oldPassword = 'A'.repeat(30) + 'a1Setup12!';
const newPassword = 'A'.repeat(30) + 'a1Change2!';

test('accountManager password and email revision coordination', async t => {
    const globals = ['accountRegression', 'document', 'bootstrap', 'window'];
    const previous = new Map(globals.map(name => [name, Object.getOwnPropertyDescriptor(globalThis, name)]));
    const bridge = globalThis.accountRegression = {};
    const gravatar = {
        checked: false,
        disabled: false,
        dataset: { saveUrl: '/api/account/avatar/gravatar' },
        addEventListener: (_, listener) => { bridge.changeGravatar = listener; }
    };
    globalThis.document = {
        getElementById: id => id === 'account-gravatar' ? gravatar : ({
            addEventListener: () => {},
            dataset: { emailUrl: '/api/account/email', signinUrl: '/auth/signin' }
        })
    };
    globalThis.bootstrap = {
        Modal: class {
            constructor(selector) { this.selector = selector; }
            show() { bridge.events.push(['show', this.selector]); }
            hide() { bridge.events.push(['hide', this.selector]); }
        }
    };
    globalThis.window = { location: { href: '/admin/account' } };
    const hooks = registerHooks({
        resolve(specifier, context, nextResolve) {
            if (context.parentURL === moduleUrl && stubs.has(specifier))
                return { url: `data:text/javascript,${encodeURIComponent(stubs.get(specifier))}`, shortCircuit: true };
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
    await import(moduleUrl);
    assert.equal(bridge.name, 'accountManager');
    const createManager = async () => {
        bridge.events = [];
        bridge.calls = [];
        bridge.fetch = async (url, method) => {
            assert.equal(url, '/api/account/email');
            assert.equal(method, 'GET');
            return { email: 'owner@example.test', revision: originalRevision };
        };
        window.location.href = '/admin/account';
        const manager = bridge.factory();
        await manager.init();
        assert.equal(manager.emailLoaded, true);
        assert.equal(manager.email, 'owner@example.test');
        assert.equal(manager.revision, originalRevision);
        manager.formData.oldPassword = oldPassword;
        manager.formData.newPassword = newPassword;
        manager.formData.email = 'changed@example.test';
        manager.formData.emailPassword = newPassword;
        bridge.calls.length = 0;
        return manager;
    };

    await t.test('Gravatar save is independent and rolls back a rejected change', async () => {
        bridge.events = [];
        bridge.calls = [];
        bridge.fetch = async () => {};
        gravatar.checked = true;

        await bridge.changeGravatar();

        assert.deepEqual(bridge.calls, [['/api/account/avatar/gravatar', 'PUT', true]]);
        assert.equal(gravatar.checked, true);
        assert.equal(gravatar.disabled, false);

        bridge.fetch = async () => { throw new Error('Save rejected'); };
        gravatar.checked = false;
        await bridge.changeGravatar();

        assert.equal(gravatar.checked, true);
        assert.equal(gravatar.disabled, false);
        assert.equal(bridge.events.at(-1)[0], 'error');
    });

    await t.test('password success refreshes revision before same-page email save', async () => {
        const manager = await createManager();
        const passwordPut = Promise.withResolvers();
        const profileGet = Promise.withResolvers();
        const reloadStarted = Promise.withResolvers();
        bridge.fetch = async (url, method) => {
            if (url === '/api/account/password/local' && method === 'PUT') return passwordPut.promise;
            if (url === '/api/account/email' && method === 'GET') {
                reloadStarted.resolve();
                return profileGet.promise;
            }
            assert.equal(url, '/api/account/email');
            assert.equal(method, 'PUT');
        };

        const resetting = manager.resetPassword();
        assert.equal(manager.emailLoaded, false);
        assert.equal(manager.savingPassword, true);
        assert.deepEqual(bridge.calls, [
            ['/api/account/password/local', 'PUT', { OldPassword: oldPassword, NewPassword: newPassword }]
        ]);
        const duplicateWhileUpdating = manager.resetPassword();
        await manager.updateEmail();
        assert.equal(bridge.calls.length, 1);
        assert.deepEqual(bridge.events, []);
        passwordPut.resolve();
        const checkpoint = await Promise.race([
            reloadStarted.promise.then(() => 'reload'),
            resetting.then(() => 'finished')
        ]);
        assert.equal(checkpoint, 'reload');
        assert.equal(manager.emailLoaded, false);
        assert.equal(manager.savingPassword, true);
        assert.equal(manager.revision, originalRevision);
        const duplicateWhileReloading = manager.resetPassword();
        await manager.updateEmail();
        assert.deepEqual(bridge.calls, [
            ['/api/account/password/local', 'PUT', { OldPassword: oldPassword, NewPassword: newPassword }],
            ['/api/account/email', 'GET']
        ]);
        assert.deepEqual(bridge.events, [['hide', '#resetPasswordModal']]);

        profileGet.resolve({ email: 'owner@example.test', revision: freshRevision });
        await Promise.all([resetting, duplicateWhileUpdating, duplicateWhileReloading]);

        assert.equal(manager.emailLoaded, true);
        assert.equal(manager.savingPassword, false);
        assert.equal(manager.revision, freshRevision);
        assert.equal(manager.formData.oldPassword, '');
        assert.equal(manager.formData.newPassword, '');
        assert.deepEqual(bridge.events, [
            ['hide', '#resetPasswordModal'], ['success', 'passwordUpdated']
        ]);
        await manager.updateEmail();
        assert.deepEqual(bridge.calls, [
            ['/api/account/password/local', 'PUT', { OldPassword: oldPassword, NewPassword: newPassword }],
            ['/api/account/email', 'GET'],
            ['/api/account/email', 'PUT', {
                Email: 'changed@example.test', Revision: freshRevision, CurrentPassword: newPassword
            }]
        ]);
        assert.equal(manager.formData.emailPassword, '');
        assert.equal(manager.savingEmail, false);
        assert.equal(window.location.href, '/auth/signin');
        assert.deepEqual(bridge.events, [
            ['hide', '#resetPasswordModal'], ['success', 'passwordUpdated'], ['hide', '#ownerEmailModal']
        ]);
    });

    for (const loaded of [true, false]) {
        await t.test(`failed password PUT restores ${loaded ? 'loaded' : 'unloaded'} state without refresh or success`, async () => {
            const manager = await createManager();
            manager.emailLoaded = loaded;
            const passwordPut = Promise.withResolvers();
            const failure = new Error('Synthetic password update rejected');
            bridge.fetch = async () => passwordPut.promise;

            const resetting = manager.resetPassword();
            assert.equal(manager.emailLoaded, false);
            await manager.updateEmail();
            assert.equal(bridge.calls.length, 1);
            passwordPut.reject(failure);
            await resetting;

            assert.equal(manager.emailLoaded, loaded);
            assert.equal(manager.savingPassword, false);
            assert.equal(manager.revision, originalRevision);
            assert.equal(manager.email, 'owner@example.test');
            assert.equal(manager.formData.oldPassword, oldPassword);
            assert.equal(manager.formData.newPassword, newPassword);
            assert.equal(window.location.href, '/admin/account');
            assert.deepEqual(bridge.calls, [
                ['/api/account/password/local', 'PUT', { OldPassword: oldPassword, NewPassword: newPassword }]
            ]);
            assert.deepEqual(bridge.events, [['error', failure]]);
        });
    }

    await t.test('failed profile reload surfaces error and leaves email submission disabled', async () => {
        const manager = await createManager();
        const failure = new Error('Synthetic profile reload failed');
        bridge.fetch = async (url, method) => {
            if (url === '/api/account/password/local' && method === 'PUT') return;
            assert.equal(url, '/api/account/email');
            assert.equal(method, 'GET');
            throw failure;
        };

        await manager.resetPassword();

        assert.equal(manager.emailLoaded, false);
        assert.equal(manager.savingPassword, false);
        assert.equal(manager.revision, originalRevision);
        assert.equal(manager.email, 'owner@example.test');
        assert.equal(manager.formData.oldPassword, '');
        assert.equal(manager.formData.newPassword, '');
        assert.deepEqual(bridge.events, [
            ['hide', '#resetPasswordModal'], ['error', failure], ['success', 'passwordUpdated']
        ]);
        await manager.updateEmail();
        assert.equal(manager.savingEmail, false);
        assert.equal(window.location.href, '/admin/account');
        assert.deepEqual(bridge.calls, [
            ['/api/account/password/local', 'PUT', { OldPassword: oldPassword, NewPassword: newPassword }],
            ['/api/account/email', 'GET']
        ]);
    });
});
