import assert from 'node:assert/strict';
import test from 'node:test';
import { slugify } from '../../../Moonglade.Web/wwwroot/js/app/utils.module.mjs';

test('slugify supports English colons in titles', () => {
    assert.equal(
        slugify('Not Only Azure: Migrating this Blog from Entra ID to Standard OIDC'),
        'not-only-azure-migrating-this-blog-from-entra-id-to-standard-oidc'
    );
});
