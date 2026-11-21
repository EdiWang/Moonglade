import assert from 'node:assert/strict';
import test from 'node:test';
import { createWidgetDraft, widgetPayload } from '../../../Moonglade.Web/wwwroot/js/app/admin.widgets.draft.mjs';

const row = (suffix = 1, overrides = {}) => ({
    id: `10000000-0000-0000-0000-${String(suffix).padStart(12, '0')}`,
    title: `Widget ${suffix}`, widgetType: 'LinkList', region: 'AfterCategories', sortOrder: suffix,
    isEnabled: true, contentCode: '[{"name":"GitHub","url":"https://example.com","icon":"bi-github","order":7}]',
    contentType: 'JSON', createdTimeUtc: '2026-11-01T00:00:00Z', ...overrides
});
const loaded = rows => { const draft = createWidgetDraft(); draft.acceptSnapshot({ widgets: rows }); return draft; };

test('a complete explicit baseline is required and invalid loading never replaces a good draft', () => {
    const draft = loaded([row()]);
    draft.selectedWidget.title = 'Unsaved';
    for (const payload of [null, {}, { widgets: null }, { widgets: {} }, { widgets: [null] }, { widgets: [row(), row()] }, { widgets: [row(1, { widgetType: 'Unknown' })] }, { widgets: [row(1, { contentCode: 'broken' })] }, { widgets: [row(1, { contentCode: '[null]' })] }]) {
        assert.throws(() => draft.acceptSnapshot(payload));
        assert.equal(draft.selectedWidget.title, 'Unsaved');
        assert.equal(draft.dirty, true);
    }
    const empty = loaded([]);
    assert.equal(empty.loaded, true);
    assert.deepEqual(empty.payload(), { widgets: [] });
    assert.equal(empty.dirty, false);
});

test('selection, disclosures and ordinary fields preserve raw JSON, flags and supported metadata', () => {
    const raw = ' [ { "Name":"<script>example</script>", "URL":"/relative", "Icon":null, "custom":{"tag":1} } ] ';
    const draft = loaded([row(1, { contentCode: raw }), row(2)]);
    const first = draft.selectedWidget;
    const itemKey = first.items[0].key;
    first.items[0].expanded = false;
    first.items[0].advanced = true;
    draft.selectWidget(row(2).id);
    draft.selectWidget(first.id);
    assert.equal(draft.selectedWidget.items[0].key, itemKey);
    assert.equal(first.items[0].expanded, false);
    assert.equal(draft.dirty, false);
    first.title = 'Title changed';
    assert.equal(widgetPayload(first).contentCode, raw);
    first.items[0].data.name = 'Edited';
    const saved = JSON.parse(widgetPayload(first).contentCode)[0];
    assert.equal(saved.name, 'Edited');
    assert.deepEqual(saved.custom, { tag: 1 });
    assert.equal(Object.hasOwn(saved, 'openInNewTab'), false);
    assert.equal(Object.hasOwn(saved, 'key'), false);
    assert.equal(Object.hasOwn(widgetPayload(first), 'createdTimeUtc'), false);
    assert.equal(Object.hasOwn(widgetPayload(first), 'contentType'), false);
});

test('all three types are created with stable IDs and no baseline mutation', () => {
    const draft = loaded([]);
    for (const type of ['LinkList', 'ImageLink', 'ButtonLink']) {
        const widget = draft.createWidget(type);
        assert.equal(widget.widgetType, type);
        assert.match(widget.id, /^[0-9a-f-]{36}$/);
        assert.equal(widget.region, 'AfterCategories');
        const id = widget.id;
        widget.title = type;
        if (type === 'ImageLink') widget.image.imageUrl = '/image.png';
        if (type === 'ButtonLink') Object.assign(widget.items[0].data, { text: 'About', url: '/about' });
        assert.equal(widget.id, id);
    }
    assert.equal(draft.issues.length, 0);
    assert.equal(draft.payload().widgets.length, 3);
    assert.deepEqual(draft.baseline, { widgets: [] });
    assert.equal(draft.dirty, true);
});

test('widget placement and movement keep selections and stable IDs with unlimited band capacity', () => {
    const draft = loaded(Array.from({ length: 12 }, (_, index) => row(index + 1, { region: 'BeforeProfile', sortOrder: index })));
    const last = draft.widgets.at(-1);
    draft.selectWidget(last.id);
    draft.moveWidget(-1);
    assert.equal(draft.regionWidgets('BeforeProfile')[10].id, last.id);
    assert.equal(draft.selectedId, last.id);
    draft.placeWidget('BeforeTags');
    assert.equal(draft.regionWidgets('BeforeTags')[0].id, last.id);
    assert.equal(draft.regionWidgets('BeforeProfile').length, 11);
    assert.deepEqual(draft.regionWidgets('BeforeProfile').map(widget => widget.sortOrder), Array.from({ length: 11 }, (_, i) => i));
});

test('new widgets append after sparse and maximum stored order values', () => {
    const draft = loaded([row(1, { sortOrder: 2147483647 })]);
    const added = draft.createWidget('LinkList');
    assert.equal(draft.orderedWidgets.at(-1).id, added.id);
    assert.equal(added.sortOrder, 2147483647);
    draft.selectWidget(row(1).id);
    draft.placeWidget('BeforeTags');
    assert.equal(draft.selectedWidget.sortOrder, 0);
});

test('staged deletion, undo and full discard preserve data without losing an intentional empty draft', () => {
    const draft = loaded([row(), row(2)]);
    const first = draft.selectedWidget;
    const itemKey = first.items[0].key;
    draft.removeWidget();
    assert.equal(draft.removedWidgets[0].id, first.id);
    assert.equal(draft.payload().widgets.length, 1);
    assert.equal(draft.selectedId, row(2).id);
    draft.restoreWidget(first.id);
    assert.equal(draft.selectedWidget.items[0].key, itemKey);
    assert.equal(draft.dirty, false);
    first.title = 'Changed';
    draft.removeWidget();
    draft.removeWidget();
    assert.deepEqual(draft.payload(), { widgets: [] });
    assert.equal(draft.dirty, true);
    draft.discard();
    assert.equal(draft.widgets[0].title, row().title);
    assert.equal(draft.dirty, false);
});

test('inline link reorder, removal and expansion retain field values and stable item keys', () => {
    const draft = loaded([row(1, { contentCode: '[{"name":"A","url":"/a","order":0},{"name":"B","url":"/b","order":1}]' })]);
    const [first, second] = draft.selectedWidget.items;
    first.data.icon = 'bi-cloud'; first.data.openInNewTab = false;
    draft.moveItem(first.key, 1);
    assert.equal(draft.selectedWidget.items[1].key, first.key);
    assert.equal(draft.selectedWidget.items[1].data.icon, 'bi-cloud');
    assert.deepEqual(draft.selectedWidget.items.map(item => item.data.order), [0, 1]);
    draft.removeItem(second.key);
    assert.equal(draft.selectedWidget.items[0].data.openInNewTab, false);
    const added = draft.addItem();
    Object.assign(added.data, { name: 'New', url: '/new' });
    assert.equal(draft.issues.length, 0);
    assert.equal(JSON.parse(draft.payload().widgets[0].contentCode).length, 2);
});

test('type changes clear only draft content and buttons enforce the one-to-three rule', () => {
    const draft = loaded([row()]);
    const baseline = JSON.stringify(draft.baseline);
    draft.switchType('ButtonLink');
    assert.equal(draft.selectedWidget.items.length, 1);
    draft.addItem(); draft.addItem();
    assert.equal(draft.addItem(), null);
    const lastKey = draft.selectedWidget.items[2].key;
    draft.moveItem(lastKey, -1);
    assert.equal(draft.selectedWidget.items[1].key, lastKey);
    for (const item of [...draft.selectedWidget.items]) draft.removeItem(item.key);
    assert.ok(draft.issues.some(issue => issue.messageKey === 'buttonCount'));
    assert.equal(JSON.stringify(draft.baseline), baseline);
    draft.discard();
    assert.equal(draft.selectedWidget.widgetType, 'LinkList');
});

test('image and button optional fields, false flags and extension properties survive serialization', () => {
    const draft = loaded([
        row(1, { widgetType: 'ImageLink', contentCode: '{"imageUrl":"/a.png","title":"Image title","altText":"Alt","linkUrl":"","cssClass":"rounded","openInNewTab":false,"extra":"keep"}' }),
        row(2, { widgetType: 'ButtonLink', contentCode: '[{"text":"About","url":"/about","cssClass":"btn-primary","openInNewTab":false,"extra":"keep"}]' })
    ]);
    draft.selectedWidget.image.altText = 'Edited alt';
    const image = JSON.parse(widgetPayload(draft.selectedWidget).contentCode);
    assert.equal(image.openInNewTab, false); assert.equal(image.linkUrl, ''); assert.equal(image.extra, 'keep');
    draft.selectWidget(row(2).id);
    draft.selectedWidget.items[0].data.text = 'Edited button';
    const button = JSON.parse(widgetPayload(draft.selectedWidget).contentCode)[0];
    assert.equal(button.cssClass, 'btn-primary'); assert.equal(button.openInNewTab, false); assert.equal(button.extra, 'keep');
});

test('title and serialized content limits identify the affected widget and item', () => {
    const draft = loaded([row()]);
    draft.selectedWidget.title = 'A'.repeat(129);
    draft.selectedWidget.items[0].data.url = 'https://example.com/' + 'a'.repeat(2000);
    const errors = draft.issues;
    assert.ok(errors.some(issue => issue.widgetId === draft.selectedId && issue.field === 'title'));
    assert.ok(errors.some(issue => issue.widgetId === draft.selectedId && issue.messageKey === 'contentTooLong'));
    draft.selectedWidget.items[0].data.name = '';
    assert.ok(draft.issues.some(issue => issue.itemKey === draft.selectedWidget.items[0].key && issue.field === 'name'));
});

test('cross-region drops and movement controls share ordering and retain widget and item identity', () => {
    const draft = loaded([row(1), row(2), row(3)]);
    const first = draft.widgets[0];
    const itemKey = first.items[0]?.key;
    draft.moveWidgetTo(first.id, 'BeforeTags');
    assert.equal(draft.regionWidgets('BeforeTags')[0], first);
    draft.moveWidgetTo(first.id, 'AfterCategories', draft.widgets[2].id);
    assert.deepEqual(draft.regionWidgets('AfterCategories').map(widget => widget.id), [draft.widgets[1].id, first.id, draft.widgets[2].id]);
    draft.selectWidget(first.id);
    draft.moveWidget(1);
    assert.deepEqual(draft.regionWidgets('AfterCategories').map(widget => widget.sortOrder), [0, 1, 2]);
    assert.equal(draft.regionWidgets('AfterCategories').at(-1), first);
    assert.equal(first.items[0]?.key, itemKey);
    const before = draft.payload();
    draft.moveWidgetTo(first.id, 'BeforeProfile', draft.widgets[1].id);
    draft.moveWidgetTo(first.id, 'Unknown');
    draft.moveWidgetTo(first.id, first.region, first.id);
    assert.deepEqual(draft.payload(), before);
    draft.discard();
    assert.equal(draft.dirty, false);
});
