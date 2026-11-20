import assert from 'node:assert/strict';
import test from 'node:test';
import { renderWidgetContent } from '../../../Moonglade.Web/wwwroot/js/app/admin.widgets.render.mjs';

test('widget preview encodes untrusted link labels and rejects executable URLs', t => {
    const previous = Object.getOwnPropertyDescriptor(globalThis, 'window');
    globalThis.window = { location: { origin: 'https://fixture.example' } };
    t.after(() => {
        if (previous) Object.defineProperty(globalThis, 'window', previous);
        else delete globalThis.window;
    });

    const html = renderWidgetContent({
        widgetType: 'LinkList',
        contentCode: JSON.stringify([
            { name: '<script>alert(1)</script>', url: 'javascript:alert(1)', openInNewTab: true, order: 0 }
        ])
    });

    assert.ok(html.includes('&lt;script&gt;alert(1)&lt;/script&gt;'));
    assert.ok(!html.includes('<script>'));
    assert.ok(html.includes('href="#"'));
    assert.ok(!html.includes('href="javascript:'));
    assert.ok(html.includes('rel="noopener noreferrer"'));
});

test('widget preview rejects executable image and button destinations', t => {
    const previous = Object.getOwnPropertyDescriptor(globalThis, 'window');
    globalThis.window = { location: { origin: 'https://fixture.example' } };
    t.after(() => {
        if (previous) Object.defineProperty(globalThis, 'window', previous);
        else delete globalThis.window;
    });

    const image = renderWidgetContent({
        widgetType: 'ImageLink',
        contentCode: JSON.stringify({ imageUrl: 'javascript:alert(1)', altText: '\"><script>x</script>' })
    });
    const buttons = renderWidgetContent({
        widgetType: 'ButtonLink',
        contentCode: JSON.stringify([{ text: '<script>x</script>', url: 'data:text/html,test' }])
    });

    assert.ok(image.includes('src="#"'));
    assert.ok(image.includes('&quot;&gt;&lt;script&gt;x&lt;/script&gt;'));
    assert.ok(!image.includes('<script>'));
    assert.ok(buttons.includes('href="#"'));
    assert.ok(!buttons.includes('<script>'));
});
