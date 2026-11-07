import assert from 'node:assert/strict';
import test from 'node:test';
import { createTagifyMixin } from '../../../Moonglade.Web/wwwroot/js/app/admin.editpost.tagify.mjs';

test('category selection restores numeric IDs and saves JSON numbers after Tagify string normalization', (t) => {
    globalThis.document = { querySelector: () => ({}) };
    globalThis.Tagify = class {
        constructor(input, settings) {
            this.settings = settings;
        }
        addTags(values) {
            this.value = values.map(tag => ({ ...tag, value: String(tag.value) }));
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
            { id: 100, displayName: 'Windows, Development' },
            { id: 101, displayName: 'Windows, Development' }
        ],
        formData: { selectedCatIds: [101, 100], tags: 'existing' },
        isFormDirty: false
    };
    editor.initCategoryTagify();
    const categories = editor.categoryTagifyInstance;
    assert.deepEqual(categories.value.map(category => category.value), ['101', '100']);
    assert.deepEqual(categories.settings.whitelist.map(category => category.value), ['100', '101']);
    assert.equal(categories.settings.enforceWhitelist, true);
    assert.equal(categories.settings.delimiters, null);
    assert.equal(categories.settings.tagTextProp, 'name');
    const typedCategory = { value: 'Windows, Development' };
    categories.settings.transformTag(typedCategory);
    assert.equal(typedCategory.value, '100');
    const selectedCategory = { value: 101 };
    categories.settings.transformTag(selectedCategory);
    assert.equal(selectedCategory.value, '101');
    categories.notify();
    assert.equal(editor.isFormDirty, false);

    categories.value = [categories.settings.whitelist[0]];
    categories.notify();
    assert.deepEqual(editor.formData.selectedCatIds, [100]);
    assert.equal(JSON.stringify(editor.formData.selectedCatIds), '[100]');
    assert.equal(editor.isFormDirty, true);

    categories.value.push(categories.settings.whitelist[1]);
    categories.notify();
    assert.deepEqual(editor.formData.selectedCatIds, [100, 101]);

    categories.value = [{ value: '101' }, { value: '100' }];
    editor.syncTags();
    assert.deepEqual(editor.formData.selectedCatIds, [101, 100]);
    assert.equal(JSON.stringify(editor.formData.selectedCatIds), '[101,100]');

    categories.value = [];
    editor.syncTags();
    assert.deepEqual(editor.formData.selectedCatIds, []);
    assert.equal(editor.formData.tags, 'existing');
});
