import assert from 'node:assert/strict';
import test from 'node:test';
import { createOffcanvas } from '../../../Moonglade.Web/wwwroot/js/app/admin.offcanvas.mjs';

test('offcanvas restores its opening control after closing', async t => {
    const previous = Object.getOwnPropertyDescriptor(globalThis, 'bootstrap');
    globalThis.bootstrap = {
        Offcanvas: class {
            constructor(element) {
                this.element = element;
            }

            show(relatedTarget) {
                const event = new Event('show.bs.offcanvas');
                event.relatedTarget = relatedTarget;
                this.element.dispatchEvent(event);
            }

            hide() {
                this.element.dispatchEvent(new Event('hidden.bs.offcanvas'));
            }
        }
    };
    t.after(() => {
        if (previous) Object.defineProperty(globalThis, 'bootstrap', previous);
        else delete globalThis.bootstrap;
    });

    const createTrigger = () => ({
        isConnected: true,
        disabled: false,
        focusCount: 0,
        closest: () => null,
        getClientRects: () => [{}],
        focus() { this.focusCount++; }
    });
    const createCanvas = trigger => {
        const element = new EventTarget();
        element.ownerDocument = { activeElement: trigger };
        return createOffcanvas(element);
    };

    await t.test('captures the active opening button and waits for the hidden event', () => {
        const trigger = createTrigger();
        const canvas = createCanvas(trigger);

        canvas.show();
        canvas.element.ownerDocument.activeElement = createTrigger();
        assert.equal(trigger.focusCount, 0);
        canvas.hide();

        assert.equal(trigger.focusCount, 1);
        assert.equal(canvas.element.ownerDocument.activeElement.focusCount, 0);
    });

    await t.test('explicit opening control survives an asynchronous focus change', () => {
        const trigger = createTrigger();
        const other = createTrigger();
        const canvas = createCanvas(other);

        canvas.show(trigger);
        canvas.hide();

        assert.equal(trigger.focusCount, 1);
        assert.equal(other.focusCount, 0);
    });

    await t.test('returns to the visible dropdown toggle instead of its hidden menu item', () => {
        const toggle = createTrigger();
        const item = createTrigger();
        item.getClientRects = () => [];
        item.closest = selector => {
            assert.equal(selector, '.dropdown');
            return {
                querySelector(selector) {
                    assert.equal(selector, '[data-bs-toggle="dropdown"]');
                    return toggle;
                }
            };
        };
        const canvas = createCanvas(item);

        canvas.show(item);
        canvas.hide();

        assert.equal(toggle.focusCount, 1);
        assert.equal(item.focusCount, 0);
    });

    await t.test('reopening replaces the target and closing does not reuse a stale target', () => {
        const first = createTrigger();
        const second = createTrigger();
        const canvas = createCanvas(first);
        canvas.show();
        canvas.hide();
        canvas.element.ownerDocument.activeElement = second;
        canvas.show();
        canvas.hide();
        canvas.hide();

        assert.equal(first.focusCount, 1);
        assert.equal(second.focusCount, 1);
    });

    for (const state of ['removed', 'hidden', 'disabled']) {
        await t.test(`does not focus a ${state} opening control`, () => {
            const trigger = createTrigger();
            const canvas = createCanvas(trigger);
            canvas.show();
            if (state === 'removed') trigger.isConnected = false;
            if (state === 'hidden') trigger.getClientRects = () => [];
            if (state === 'disabled') trigger.disabled = true;

            canvas.hide();

            assert.equal(trigger.focusCount, 0);
        });
    }
});
