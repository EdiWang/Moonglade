import assert from 'node:assert/strict';
import test from 'node:test';

test('authenticator digits support numeric entry, paste, autofill and keyboard navigation', async (t) => {
    let focused;
    const digits = Array.from({ length: 6 }, () => Object.assign(new EventTarget(), {
        value: '',
        focus() { focused = this; this.dispatchEvent(new Event('focus')); },
        select() { this.selected = true; }
    }));
    const code = { value: '', form: new EventTarget() };
    globalThis.document = {
        getElementById: () => code,
        querySelectorAll: () => digits
    };
    t.after(() => delete globalThis.document);
    await import('../../../Moonglade.Web/wwwroot/js/app/authenticator-code.mjs');

    const input = (index, value) => {
        digits[index].value = value;
        digits[index].dispatchEvent(new Event('input'));
    };
    const key = (index, key) => digits[index].dispatchEvent(
        Object.assign(new Event('keydown', { cancelable: true }), { key }));

    assert.equal(focused, digits[0]);
    input(0, '0');
    assert.equal(code.value, '0');
    assert.equal(focused, digits[1]);
    assert.equal(focused.selected, true);

    const letter = Object.assign(new Event('beforeinput', { cancelable: true }), {
        data: 'a', inputType: 'insertText'
    });
    assert.equal(digits[1].dispatchEvent(letter), false);
    input(1, 'a');
    assert.equal(digits[1].value, '');
    assert.equal(code.value, '0');

    input(0, '012345');
    assert.equal(code.value, '012345');
    assert.deepEqual(digits.map(digit => digit.value), [...'012345']);
    assert.equal(focused, digits[5]);
    key(5, 'ArrowRight');
    assert.equal(focused, digits[5]);
    key(5, 'ArrowLeft');
    assert.equal(focused, digits[4]);
    key(0, 'ArrowLeft');
    assert.equal(focused, digits[0]);

    digits[3].dispatchEvent(Object.assign(new Event('paste', { cancelable: true }), {
        clipboardData: { getData: () => '987 654' }
    }));
    assert.equal(code.value, '987654');
    input(5, '');
    key(5, 'Backspace');
    assert.equal(code.value, '9876');
    assert.equal(digits[4].value, '');
    assert.equal(focused, digits[4]);
    input(4, '12');
    assert.equal(code.value, '987612');

    input(5, '9');
    assert.equal(code.value, '987619');
    digits[0].value = '0';
    code.form.dispatchEvent(new Event('submit'));
    assert.equal(code.value, '087619');
});
