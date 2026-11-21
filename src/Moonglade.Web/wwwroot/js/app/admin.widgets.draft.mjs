export const widgetRegions = ['BeforeProfile', 'BeforeTags', 'BeforeCategories', 'AfterCategories'];
export const widgetTypes = ['LinkList', 'ImageLink', 'ButtonLink'];
const emptyId = '00000000-0000-0000-0000-000000000000';
const validId = value => typeof value === 'string' && /^[0-9a-f]{8}(-[0-9a-f]{4}){3}-[0-9a-f]{12}$/i.test(value) && value.toLowerCase() !== emptyId;
const key = () => crypto.randomUUID();
const copy = value => JSON.parse(JSON.stringify(value));
const isObject = value => value !== null && typeof value === 'object' && !Array.isArray(value);

function normalizeProperties(data, properties) {
    return Object.fromEntries(Object.entries(data).map(([name, value]) =>
        [properties.find(property => property.toLowerCase() === name.toLowerCase()) ?? name, value]));
}

function contentValue(widget) {
    return widget.widgetType === 'ImageLink' ? widget.image : widget.items.map(item => item.data);
}

function contentCode(widget) {
    const content = JSON.stringify(contentValue(widget));
    return content === widget.originalContent ? widget.contentCode : content;
}

function readWidget(row) {
    if (!isObject(row) || !validId(row.id) || typeof row.title !== 'string' || !widgetTypes.includes(row.widgetType)
        || !widgetRegions.includes(row.region) || !Number.isInteger(row.sortOrder) || row.sortOrder < 0 || row.sortOrder > 2147483647
        || typeof row.isEnabled !== 'boolean' || typeof row.contentCode !== 'string' || (row.contentType !== undefined && row.contentType !== 'JSON'))
        throw new Error(validId(row?.id) ? `Invalid widget content: ${row.id}` : 'Invalid widget collection.');
    let parsed;
    try { parsed = JSON.parse(row.contentCode); } catch { throw new Error(`Invalid widget content: ${row.id}`); }
    const widget = { ...row, id: row.id.toLowerCase(), deleted: false, advanced: false, items: [], image: {} };
    if (row.widgetType === 'ImageLink') {
        if (!isObject(parsed)) throw new Error(`Invalid widget content: ${row.id}`);
        widget.image = normalizeProperties(parsed, ['imageUrl', 'cssClass', 'title', 'altText', 'linkUrl', 'openInNewTab']);
    } else {
        if (!Array.isArray(parsed) || parsed.some(item => !isObject(item))) throw new Error(`Invalid widget content: ${row.id}`);
        const properties = row.widgetType === 'LinkList' ? ['name', 'url', 'icon', 'order', 'openInNewTab'] : ['text', 'url', 'cssClass', 'openInNewTab'];
        widget.items = parsed.map((data, sourceIndex) => ({ key: key(), sourceIndex, expanded: false, advanced: false, data: normalizeProperties(data, properties) }));
        if (row.widgetType === 'LinkList') widget.items.sort((a, b) => (a.data.order ?? 0) - (b.data.order ?? 0));
        if (widget.items[0]) widget.items[0].expanded = true;
    }
    widget.originalContent = JSON.stringify(contentValue(widget));
    return widget;
}

export function widgetPayload(widget) {
    return {
        id: widget.id, title: widget.title, widgetType: widget.widgetType, region: widget.region,
        sortOrder: widget.sortOrder, isEnabled: widget.isEnabled, contentCode: contentCode(widget)
    };
}

export function validateWidget(widget) {
    const errors = [];
    const add = (field, messageKey, itemKey = null) => errors.push({ widgetId: widget.id, field, messageKey, itemKey });
    if (!widget.title.trim()) add('title', 'titleRequired');
    if (widget.title.length > 128) add('title', 'titleTooLong');
    if (!widgetRegions.includes(widget.region)) add('region', 'widgetRegionRequired');
    if (!Number.isInteger(widget.sortOrder) || widget.sortOrder < 0 || widget.sortOrder > 2147483647) add('region', 'sortOrderRange');
    if (contentCode(widget).length > 2000) add('contentCode', 'contentTooLong');
    const checkTypes = (data, fields, itemKey = null) => {
        if (fields.some(field => data[field] !== undefined && data[field] !== null && typeof data[field] !== 'string')
            || (data.openInNewTab !== undefined && typeof data.openInNewTab !== 'boolean')) add('contentCode', 'invalidContent', itemKey);
    };
    if (widget.widgetType === 'ImageLink') {
        checkTypes(widget.image, ['imageUrl', 'cssClass', 'title', 'altText', 'linkUrl']);
        if (typeof widget.image.imageUrl !== 'string' || !widget.image.imageUrl.trim()) add('imageUrl', 'imageUrlRequired');
    } else {
        if (widget.widgetType === 'ButtonLink' && (widget.items.length < 1 || widget.items.length > 3)) add('contentCode', 'buttonCount');
        widget.items.forEach(item => {
            const nameField = widget.widgetType === 'LinkList' ? 'name' : 'text';
            checkTypes(item.data, [nameField, 'url', widget.widgetType === 'LinkList' ? 'icon' : 'cssClass'], item.key);
            if (typeof item.data[nameField] !== 'string' || !item.data[nameField].trim()) add(nameField, 'itemNameRequired', item.key);
            if (typeof item.data.url !== 'string' || !item.data.url.trim()) add('url', 'itemUrlRequired', item.key);
            if (widget.widgetType === 'LinkList' && item.data.order !== undefined && (!Number.isInteger(item.data.order) || item.data.order < -2147483648 || item.data.order > 2147483647))
                add('contentCode', 'invalidContent', item.key);
        });
    }
    return errors;
}

export function createWidgetDraft() {
    return {
        widgets: [], baseline: null, selectedId: null, loaded: false,
        get orderedWidgets() {
            return this.widgets.filter(widget => !widget.deleted).sort((a, b) => widgetRegions.indexOf(a.region) - widgetRegions.indexOf(b.region) || a.sortOrder - b.sortOrder);
        },
        get selectedWidget() { return this.widgets.find(widget => widget.id === this.selectedId && !widget.deleted) ?? null; },
        get removedWidgets() { return this.widgets.filter(widget => widget.deleted); },
        get dirty() { return this.loaded && JSON.stringify(this.payload()) !== JSON.stringify(this.baseline); },
        get issues() { return this.orderedWidgets.flatMap(validateWidget); },
        regionWidgets(region) { return this.orderedWidgets.filter(widget => widget.region === region); },
        payload() { return { widgets: this.orderedWidgets.map(widgetPayload) }; },
        acceptSnapshot(payload) {
            if (!isObject(payload) || !Array.isArray(payload.widgets)) throw new Error('A complete widget collection is required.');
            const widgets = payload.widgets.map(readWidget);
            if (new Set(widgets.map(widget => widget.id)).size !== widgets.length) throw new Error('Widget IDs must be unique.');
            const invalid = widgets.flatMap(validateWidget);
            if (invalid.length) throw new Error(`Invalid widget content: ${invalid[0].widgetId}`);
            const selected = this.selectedId;
            this.widgets = widgets;
            this.baseline = copy(this.payload());
            this.loaded = true;
            this.selectedId = widgets.some(widget => widget.id === selected) ? selected : (widgets[0]?.id ?? null);
        },
        selectWidget(id) { if (this.widgets.some(widget => widget.id === id && !widget.deleted)) this.selectedId = id; },
        createWidget(type) {
            if (!this.loaded || !widgetTypes.includes(type)) return null;
            const last = this.regionWidgets('AfterCategories').at(-1);
            const widget = {
                id: key(), title: '', widgetType: type, region: 'AfterCategories', sortOrder: last ? Math.min(last.sortOrder + 1, 2147483647) : 0,
                isEnabled: true, contentCode: '', originalContent: null, deleted: false, advanced: false,
                image: { imageUrl: '', title: '', altText: '', linkUrl: '', cssClass: '', openInNewTab: true }, items: []
            };
            if (type === 'ButtonLink') widget.items.push(this.newItem(type));
            this.widgets.push(widget);
            this.selectedId = widget.id;
            return widget;
        },
        newItem(type) {
            return { key: key(), expanded: true, advanced: false, data: type === 'LinkList'
                ? { name: '', url: '', icon: '', order: 0, openInNewTab: true }
                : { text: '', url: '', cssClass: 'btn-outline-primary', openInNewTab: true } };
        },
        switchType(type) {
            const widget = this.selectedWidget;
            if (!widget || !widgetTypes.includes(type) || widget.widgetType === type) return;
            widget.widgetType = type;
            widget.originalContent = null;
            widget.image = { imageUrl: '', title: '', altText: '', linkUrl: '', cssClass: '', openInNewTab: true };
            widget.items = type === 'ButtonLink' ? [this.newItem(type)] : [];
        },
        normalizeRegion(region) { this.regionWidgets(region).forEach((widget, index) => widget.sortOrder = index); },
        moveWidget(direction) {
            const widget = this.selectedWidget;
            if (!widget) return;
            const region = this.regionWidgets(widget.region);
            const index = region.findIndex(item => item.id === widget.id);
            const destination = index + direction;
            if (destination < 0 || destination >= region.length) return;
            [region[index], region[destination]] = [region[destination], region[index]];
            region.forEach((item, order) => item.sortOrder = order);
        },
        canMoveWidget(direction) {
            const widget = this.selectedWidget;
            if (!widget) return false;
            const region = this.regionWidgets(widget.region);
            const destination = region.findIndex(item => item.id === widget.id) + direction;
            return destination >= 0 && destination < region.length;
        },
        placeWidget(region) {
            const widget = this.selectedWidget;
            if (!widget || !widgetRegions.includes(region) || widget.region === region) return;
            const previous = widget.region;
            const nextOrder = this.regionWidgets(region).length;
            this.normalizeRegion(region);
            widget.region = region;
            widget.sortOrder = nextOrder;
            this.normalizeRegion(previous);
            this.normalizeRegion(region);
        },
        removeWidget() {
            const widget = this.selectedWidget;
            if (!widget) return;
            const visible = this.orderedWidgets;
            const index = visible.findIndex(item => item.id === widget.id);
            widget.deleted = true;
            this.selectedId = visible[index + 1]?.id ?? visible[index - 1]?.id ?? null;
        },
        restoreWidget(id) {
            const widget = this.widgets.find(item => item.id === id && item.deleted);
            if (widget) { widget.deleted = false; this.selectedId = id; }
        },
        addItem() {
            const widget = this.selectedWidget;
            if (!widget || widget.widgetType === 'ImageLink' || (widget.widgetType === 'ButtonLink' && widget.items.length >= 3)) return null;
            const item = this.newItem(widget.widgetType);
            widget.items.push(item);
            if (widget.widgetType === 'LinkList') widget.items.forEach((entry, index) => entry.data.order = index);
            return item;
        },
        removeItem(itemKey) {
            const widget = this.selectedWidget;
            if (!widget) return;
            widget.items = widget.items.filter(item => item.key !== itemKey);
            if (widget.widgetType === 'LinkList') widget.items.forEach((item, index) => item.data.order = index);
        },
        moveItem(itemKey, direction) {
            const widget = this.selectedWidget;
            if (!widget) return;
            const index = widget.items.findIndex(item => item.key === itemKey);
            const destination = index + direction;
            if (index < 0 || destination < 0 || destination >= widget.items.length) return;
            const [item] = widget.items.splice(index, 1);
            widget.items.splice(destination, 0, item);
            if (widget.widgetType === 'LinkList') widget.items.forEach((entry, order) => entry.data.order = order);
        },
        discard() { if (this.baseline) this.acceptSnapshot(copy(this.baseline)); }
    };
}
