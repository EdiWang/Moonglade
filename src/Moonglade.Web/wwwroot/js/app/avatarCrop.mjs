import { fetch2 } from './httpService.mjs?v=1500';
import { success } from './toastService.mjs';
import { getLocalizedString } from './utils.module.mjs';

export function bindAvatarCrop(uploadUrl) {
    const modal = document.getElementById('avatarmodal');
    const dropTarget = document.getElementById('avatarDropTarget');
    const prompt = document.getElementById('avatarDropPrompt');
    const editor = document.getElementById('avatarCropEditor');
    const fileInput = document.getElementById('avatarImageFile');
    const preview = document.getElementById('avatarCropPreview');
    const zoom = document.getElementById('avatarCropZoom');
    const save = document.getElementById('btn-upload-avatar');
    const message = document.getElementById('avatarCropError');
    const context = preview.getContext('2d');
    const saveLabel = save.textContent;
    let image;
    let selection = 0;
    let saving = false;
    let centerX;
    let centerY;
    let drag;

    const cropSize = () => Math.min(image.width, image.height) / zoom.valueAsNumber;

    function render() {
        const size = cropSize();
        const half = size / 2;
        centerX = Math.min(image.width - half, Math.max(half, centerX));
        centerY = Math.min(image.height - half, Math.max(half, centerY));
        context.fillStyle = '#fff';
        context.fillRect(0, 0, preview.width, preview.height);
        context.drawImage(image, centerX - half, centerY - half, size, size, 0, 0, preview.width, preview.height);
    }

    function clearCrop() {
        image?.close();
        image = null;
        drag = null;
        editor.hidden = true;
        prompt.hidden = false;
        save.disabled = true;
        zoom.value = '1';
        message.textContent = '';
    }

    async function selectFile(file) {
        if (!file || saving) return;
        const currentSelection = ++selection;
        clearCrop();
        try {
            if (!['image/png', 'image/jpeg'].includes(file.type)) {
                throw new Error('Unsupported avatar image type');
            }
            const bitmap = await createImageBitmap(file);
            if (currentSelection !== selection) {
                bitmap.close();
                return;
            }
            image = bitmap;
            centerX = image.width / 2;
            centerY = image.height / 2;
            render();
            editor.hidden = false;
            prompt.hidden = true;
            save.disabled = false;
            if (document.activeElement === fileInput) preview.focus();
        } catch {
            if (currentSelection === selection) {
                clearCrop();
                fileInput.value = '';
                message.textContent = getLocalizedString('avatarInvalidImage');
            }
        }
    }

    fileInput.addEventListener('change', () => selectFile(fileInput.files[0]));
    dropTarget.addEventListener('dragover', event => event.preventDefault());
    dropTarget.addEventListener('drop', event => {
        event.preventDefault();
        if (saving || !event.dataTransfer.files.length) return;
        fileInput.files = event.dataTransfer.files;
        selectFile(fileInput.files[0]);
    });

    zoom.addEventListener('input', render);
    preview.addEventListener('wheel', event => {
        if (!image || saving || event.ctrlKey || !event.deltaY) return;
        event.preventDefault();
        if (event.deltaY < 0) zoom.stepUp(10);
        else zoom.stepDown(10);
        render();
    }, { passive: false });
    preview.addEventListener('pointerdown', event => {
        if (!image || saving || drag || event.button !== 0) return;
        preview.setPointerCapture(event.pointerId);
        drag = { id: event.pointerId, x: event.clientX, y: event.clientY };
    });
    preview.addEventListener('pointermove', event => {
        if (saving || drag?.id !== event.pointerId) return;
        const scale = cropSize() / preview.getBoundingClientRect().width;
        centerX -= (event.clientX - drag.x) * scale;
        centerY -= (event.clientY - drag.y) * scale;
        drag.x = event.clientX;
        drag.y = event.clientY;
        render();
    });
    preview.addEventListener('lostpointercapture', event => {
        if (drag?.id === event.pointerId) drag = null;
    });
    preview.addEventListener('keydown', event => {
        if (!image || saving) return;
        const step = cropSize() / 30;
        switch (event.key) {
            case 'ArrowLeft': centerX += step; break;
            case 'ArrowRight': centerX -= step; break;
            case 'ArrowUp': centerY += step; break;
            case 'ArrowDown': centerY -= step; break;
            default: return;
        }
        event.preventDefault();
        render();
    });

    modal.addEventListener('hide.bs.modal', event => {
        if (saving) {
            event.preventDefault();
            return;
        }
        ++selection;
    });
    modal.addEventListener('hidden.bs.modal', () => {
        clearCrop();
        fileInput.value = '';
        fileInput.disabled = zoom.disabled = false;
        save.textContent = saveLabel;
    });

    save.addEventListener('click', async () => {
        if (save.disabled || saving) return;
        saving = true;
        save.disabled = fileInput.disabled = zoom.disabled = true;
        save.textContent = save.dataset.savingLabel;
        message.textContent = '';
        try {
            const data = preview.toDataURL('image/jpeg', 0.9).split(',')[1];
            await fetch2(uploadUrl, 'POST', data);
        } catch (err) {
            message.textContent = err;
            save.disabled = fileInput.disabled = zoom.disabled = false;
            save.textContent = saveLabel;
            return;
        } finally {
            saving = false;
        }
        bootstrap.Modal.getInstance(modal).hide();
        success(getLocalizedString('imageUpdated'));
        const version = Date.now();
        for (const avatar of document.querySelectorAll('.blogadmin-avatar, .admin-nav-head-pic')) {
            const url = new URL(avatar.src);
            url.searchParams.set('v', version);
            avatar.src = url.href;
        }
    });
}
