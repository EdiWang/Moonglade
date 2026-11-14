import assert from 'node:assert/strict';
import test from 'node:test';

test('selected navigation stays visible after fonts load and navigation resizes', async (t) => {
    const scrolls = [];
    const observed = [];
    const focus = new Map();
    let resize;
    let fontsLoaded;
    const activeLink = {
        scrollIntoView(options) { scrolls.push({ link: 'selected', options }); },
        addEventListener(event, callback) {
            assert.equal(event, 'focus');
            focus.set('selected', callback);
        }
    };
    const otherLink = {
        scrollIntoView(options) { scrolls.push({ link: 'other', options }); },
        addEventListener(event, callback) {
            assert.equal(event, 'focus');
            focus.set('other', callback);
        }
    };
    const navigation = {
        querySelector(selector) {
            assert.equal(selector, '[aria-current="page"]');
            return activeLink;
        },
        querySelectorAll(selector) {
            assert.equal(selector, 'a');
            return [activeLink, otherLink];
        }
    };
    globalThis.document = {
        querySelector(selector) {
            assert.equal(selector, '.admin-secondary-nav');
            return navigation;
        },
        fonts: { ready: new Promise(resolve => { fontsLoaded = resolve; }) }
    };
    globalThis.ResizeObserver = class {
        constructor(callback) { resize = callback; }
        observe(element) { observed.push(element); }
    };
    t.after(() => {
        delete globalThis.document;
        delete globalThis.ResizeObserver;
    });

    await import('../../../Moonglade.Web/wwwroot/js/app/admin.navigation.mjs');
    assert.deepEqual(observed, [navigation]);
    assert.equal(scrolls.length, 0);

    fontsLoaded();
    await document.fonts.ready;
    assert.deepEqual(scrolls, [
        { link: 'selected', options: { block: 'nearest', inline: 'nearest' } }
    ]);

    resize();
    assert.deepEqual(scrolls, [
        { link: 'selected', options: { block: 'nearest', inline: 'nearest' } },
        { link: 'selected', options: { block: 'nearest', inline: 'nearest' } }
    ]);

    await t.test('keyboard focus reveals the focused link rather than the selected link', () => {
        scrolls.length = 0;
        focus.get('other')();
        assert.deepEqual(scrolls, [
            { link: 'other', options: { block: 'nearest', inline: 'nearest' } }
        ]);

        focus.get('selected')();
        assert.deepEqual(scrolls, [
            { link: 'other', options: { block: 'nearest', inline: 'nearest' } },
            { link: 'selected', options: { block: 'nearest', inline: 'nearest' } }
        ]);
    });
});
