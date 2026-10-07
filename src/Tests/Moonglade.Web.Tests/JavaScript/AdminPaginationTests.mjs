import assert from 'node:assert/strict';
import test from 'node:test';
import { createPaginationMixin } from '../../../Moonglade.Web/wwwroot/js/app/admin.pagination.mjs';

test('changing pages scrolls the admin content after loading the new page', async (t) => {
    const calls = [];
    globalThis.document = {
        querySelector(selector) {
            assert.equal(selector, '.admin-main');
            return { scrollTo: (...position) => calls.push(position) };
        }
    };
    t.after(() => delete globalThis.document);

    const manager = Object.assign(createPaginationMixin(5), {
        totalRows: 20,
        async loadData() { calls.push(this.currentPage); },
        updateUrl() { calls.push('url'); }
    });
    await manager.goToPage(2);
    assert.deepEqual(calls, [2, 'url', [0, 0]]);
});
