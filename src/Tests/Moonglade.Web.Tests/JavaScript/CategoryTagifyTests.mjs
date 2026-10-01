import assert from 'node:assert/strict';
import test from 'node:test';
import { createTagifyMixin } from '../../../Moonglade.Web/wwwroot/js/app/admin.editpost.tagify.mjs';

test('category selection restores and saves IDs and tracks additions and removals', (t) => {
    globalThis.document = { querySelector: () => ({}) };
    globalThis.Tagify = class {
        constructor(input, settings) {
            this.settings = settings;
        }
        addTags(values) {
            this.value = [...values];
        }
        on(events, handler) {
            this.notify = handler;
        }
    };
    t.after(() => {
        delete globalThis.document;
        delete globalThis.Tagify;
    });

    const editor = {
        ...createTagifyMixin(),
        categories: [
            { id: 'first', displayName: 'Windows, Development' },
            { id: 'second', displayName: 'Windows, Development' }
        ],
        formData: { selectedCatIds: ['second', 'first'], tags: 'existing' },
        isFormDirty: false
    };
    editor.initCategoryTagify();
    const categories = editor.categoryTagifyInstance;
    assert.deepEqual(categories.value.map(category => category.value), ['second', 'first']);
    assert.equal(categories.settings.enforceWhitelist, true);
    assert.equal(categories.settings.delimiters, null);
    assert.equal(categories.settings.tagTextProp, 'name');
    const typedCategory = { value: 'Windows, Development' };
    categories.settings.transformTag(typedCategory);
    assert.equal(typedCategory.value, 'first');
    const selectedCategory = { value: 'second' };
    categories.settings.transformTag(selectedCategory);
    assert.equal(selectedCategory.value, 'second');
    categories.notify();
    assert.equal(editor.isFormDirty, false);

    categories.value = [categories.settings.whitelist[0]];
    categories.notify();
    assert.deepEqual(editor.formData.selectedCatIds, ['first']);
    assert.equal(editor.isFormDirty, true);

    categories.value.push(categories.settings.whitelist[1]);
    categories.notify();
    assert.deepEqual(editor.formData.selectedCatIds, ['first', 'second']);

    categories.value = [];
    editor.syncTags();
    assert.deepEqual(editor.formData.selectedCatIds, []);
    assert.equal(editor.formData.tags, 'existing');
});
