import assert from 'node:assert/strict';
import test from 'node:test';
import {
    localDateBoundaryToUtcIso,
    parseUtcDate,
    toLocalDateTimeInputValue,
    toUtcDatePath
} from '../../../Moonglade.Web/wwwroot/js/app/utils.module.mjs';

test('parseUtcDate treats a timestamp without an offset as UTC', () => {
    const parsed = parseUtcDate('2026-08-15T01:02:03.1234567');

    assert.equal(parsed?.toISOString(), '2026-08-15T01:02:03.123Z');
});

test('toUtcDatePath keeps published routes on the UTC calendar date', () => {
    assert.equal(toUtcDatePath('2026-01-01T00:30:00Z'), '2026/1/1');
});

test('toLocalDateTimeInputValue performs exactly one UTC-to-local conversion', () => {
    const sourceUtc = '2026-01-15T12:34:00Z';
    const localInput = toLocalDateTimeInputValue(sourceUtc);

    assert.equal(new Date(localInput).toISOString(), '2026-01-15T12:34:00.000Z');
});

test('localDateBoundaryToUtcIso converts local day boundaries to explicit UTC', () => {
    const start = new Date(localDateBoundaryToUtcIso('2026-08-15'));
    const end = new Date(localDateBoundaryToUtcIso('2026-08-15', true));

    assert.equal(start.getFullYear(), 2026);
    assert.equal(start.getMonth(), 7);
    assert.equal(start.getDate(), 15);
    assert.equal(start.getHours(), 0);
    assert.equal(end.getFullYear(), 2026);
    assert.equal(end.getMonth(), 7);
    assert.equal(end.getDate(), 15);
    assert.equal(end.getHours(), 23);
    assert.equal(end.getMinutes(), 59);
    assert.equal(end.getSeconds(), 59);
});

test('UTC parsing helpers reject invalid input', () => {
    assert.equal(parseUtcDate('not-a-date'), null);
    assert.equal(toUtcDatePath('not-a-date'), null);
    assert.equal(localDateBoundaryToUtcIso('not-a-date'), null);
});
