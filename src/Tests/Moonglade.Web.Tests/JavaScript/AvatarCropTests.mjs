import assert from 'node:assert/strict';
import test from 'node:test';

test('avatar crop centers a wide image and submits the adjusted square', async () => {
    const nodes = new Map();
    globalThis.document = {
        getElementById(id) {
            if (id === 'liveToast') return null;
            if (!nodes.has(id)) {
                nodes.set(id, {
                    dataset: {},
                    addEventListener(name, listener) { this[name] = listener; }
                });
            }
            return nodes.get(id);
        },
        querySelector: () => null,
        querySelectorAll: () => [],
        activeElement: null
    };
    const node = id => document.getElementById(id);
    const bitmap = { width: 800, height: 400, close() {} };
    globalThis.createImageBitmap = async () => bitmap;
    globalThis.bootstrap = { Modal: { getInstance: () => ({ hide() {} }) } };

    const preview = node('avatarCropPreview');
    preview.width = preview.height = 300;
    let crop;
    preview.getContext = () => ({
        fillRect() {},
        drawImage(source, ...coordinates) {
            assert.equal(source, bitmap);
            crop = coordinates;
        }
    });
    preview.toDataURL = mimeType => {
        assert.equal(mimeType, 'image/jpeg');
        assert.deepEqual(crop, [300 + 200 / 30, 100, 200, 200, 0, 0, 300, 300]);
        return 'data:image/jpeg;base64,Y3JvcA==';
    };
    const zoom = node('avatarCropZoom');
    Object.defineProperty(zoom, 'valueAsNumber', { get: () => Number(zoom.value) });
    zoom.stepUp = steps => { zoom.value = (zoom.valueAsNumber + steps * 0.01).toFixed(2); };
    zoom.stepDown = steps => { zoom.value = (zoom.valueAsNumber - steps * 0.01).toFixed(2); };
    const { bindAvatarCrop } = await import('../../../Moonglade.Web/wwwroot/js/app/avatarCrop.mjs');
    bindAvatarCrop('/api/account/avatar');
    const input = node('avatarImageFile');
    input.files = [{ type: 'image/png' }];
    await input.change();
    assert.deepEqual(crop, [200, 0, 400, 400, 0, 0, 300, 300]);
    zoom.value = '2';
    zoom.input();
    let prevented = 0;
    const wheel = deltaY => ({ deltaY, preventDefault() { ++prevented; } });
    preview.wheel(wheel(-100));
    assert.equal(zoom.valueAsNumber, 2.1);
    assert.equal(crop[2], 400 / 2.1);
    preview.wheel(wheel(100));
    assert.equal(zoom.valueAsNumber, 2);
    assert.equal(crop[2], 200);
    preview.wheel({ ...wheel(-100), ctrlKey: true });
    assert.equal(zoom.valueAsNumber, 2);
    assert.equal(prevented, 2);
    preview.keydown({ key: 'ArrowLeft', preventDefault() {} });

    const requests = [];
    globalThis.fetch = async (url, options) => {
        requests.push([url, options.method, options.body]);
        return new Response(null, { status: 204 });
    };
    await node('btn-upload-avatar').click();
    assert.deepEqual(requests, [['/api/account/avatar', 'POST', '"Y3JvcA=="']]);
});
