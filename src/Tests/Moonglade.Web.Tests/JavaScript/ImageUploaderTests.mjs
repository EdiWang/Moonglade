import assert from 'node:assert/strict';
import test from 'node:test';

test('ImageUploader', async (t) => {
    const nodes = new Map();
    const reads = [];
    const canvases = [];
    const messages = [];
    const originalGlobals = new Map(
        ['document', 'window', 'bootstrap', 'FileReader', 'Image']
            .map(name => [name, Object.getOwnPropertyDescriptor(globalThis, name)])
    );
    t.after(() => {
        for (const [name, descriptor] of originalGlobals) {
            if (descriptor) Object.defineProperty(globalThis, name, descriptor);
            else delete globalThis[name];
        }
    });

    const createElement = () => {
        const classes = new Set(['disabled']);
        return {
            classes,
            disabled: true,
            innerHTML: '',
            classList: {
                add: value => classes.add(value),
                remove: value => classes.delete(value)
            },
            removeAttribute(name) {
                assert.equal(name, 'disabled');
                this.disabled = false;
            },
            addEventListener(name, handler) {
                this[name] = handler;
            }
        };
    };
    nodes.set('liveToast', createElement());
    nodes.set('blogtoast-message', {});
    nodes.set('localizedStrings', {
        dataset: {
            pleaseSelectImage: 'Please select an image',
            pleaseSelectImageFile: 'Please select an image file.'
        }
    });
    globalThis.document = {
        getElementById: id => nodes.get(id),
        querySelector: selector => selector.startsWith('#') ? nodes.get(selector.slice(1)) : null,
        createElement(name) {
            assert.equal(name, 'canvas');
            const canvas = {
                getContext(type) {
                    assert.equal(type, '2d');
                    return { drawImage: (...args) => { this.drawArgs = args; } };
                },
                toDataURL: mimeType => `data:${mimeType};base64,cHJldmlldw==`
            };
            canvases.push(canvas);
            return canvas;
        }
    };
    globalThis.bootstrap = {
        Toast: class {
            show() { messages.push(nodes.get('blogtoast-message').textContent); }
        }
    };
    globalThis.FileReader = class {
        readAsDataURL(file) {
            reads.push(file);
            this.result = 'data:image/png;base64,c291cmNl';
            this.onloadend();
        }
    };
    globalThis.Image = class {
        width = 1024;
        height = 768;
        set src(value) {
            this.source = value;
            queueMicrotask(() => this.onload());
        }
    };
    globalThis.window = { File: class {}, FileReader, FileList: class {}, Blob };
    const { ImageUploader } = await import('../../../Moonglade.Web/wwwroot/js/app/imageuploader.mjs');

    const createUploader = (target, size, mimeType) => {
        reads.length = 0;
        canvases.length = 0;
        messages.length = 0;
        nodes.set(`${target}ImageFile`, createElement());
        nodes.set(`${target}DropTarget`, createElement());
        nodes.set(`btn-upload-${target}`, createElement());
        const uploader = new ImageUploader(target, size, mimeType);
        uploader.bindEvents();
        return uploader;
    };
    const createEvent = (source, files) => {
        const event = {
            stopped: false,
            prevented: false,
            stopPropagation() { this.stopped = true; },
            preventDefault() { this.prevented = true; }
        };
        if (source === 'drop') event.dataTransfer = { files };
        else event.target = { files };
        return event;
    };

    for (const source of ['drop', 'change']) {
        await t.test(`site icon ${source} creates a resized preview and enables upload`, async () => {
            const uploader = createUploader('siteicon', 512, 'image/png');
            const file = { name: 'photo.png', type: 'image/png' };
            const event = createEvent(source, [file]);
            const element = nodes.get(source === 'drop' ? 'siteiconDropTarget' : 'siteiconImageFile');
            element[source](event);
            await Promise.resolve();

            assert.equal(event.stopped, true);
            assert.equal(event.prevented, true);
            assert.deepEqual(reads, [file]);
            assert.equal(canvases.length, 1);
            assert.deepEqual(canvases[0].drawArgs.slice(1), [0, 0, 512, 384]);
            assert.equal(canvases[0].width, 512);
            assert.equal(canvases[0].height, 384);
            assert.equal(uploader.getDataUrl(), 'data:image/png;base64,cHJldmlldw==');
            assert.equal(nodes.get('siteiconDropTarget').innerHTML,
                '<img class="img-fluid" src="data:image/png;base64,cHJldmlldw==" />');
            assert.equal(nodes.get('btn-upload-siteicon').disabled, false);
            assert.equal(nodes.get('btn-upload-siteicon').classes.has('disabled'), false);
            assert.deepEqual(messages, []);
        });

        await t.test(`empty ${source} reports missing image without reading a file`, () => {
            const uploader = createUploader('siteicon', 512, 'image/png');
            uploader.fileSelect(createEvent(source, []));
            assert.deepEqual(messages, ['Please select an image']);
            assert.deepEqual(reads, []);
            assert.equal(uploader.getDataUrl(), '');
            assert.equal(nodes.get('siteiconDropTarget').innerHTML, '');
            assert.equal(nodes.get('btn-upload-siteicon').disabled, true);
        });

        await t.test(`non-image ${source} reports invalid file without creating a preview`, () => {
            const uploader = createUploader('siteicon', 512, 'image/png');
            uploader.fileSelect(createEvent(source, [{ name: 'notes.txt', type: 'text/plain' }]));
            assert.deepEqual(messages, ['Please select an image file.']);
            assert.deepEqual(reads, []);
            assert.equal(uploader.getDataUrl(), '');
            assert.equal(nodes.get('siteiconDropTarget').innerHTML, '');
            assert.equal(nodes.get('btn-upload-siteicon').disabled, true);
        });
    }
});
