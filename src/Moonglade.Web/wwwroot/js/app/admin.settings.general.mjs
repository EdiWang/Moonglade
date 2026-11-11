import { initializeSettings } from './admin.settings.mjs?v=2';
import { ImageUploader } from './imageuploader.mjs';

var siteiconUploader = new ImageUploader('siteicon', 512, 'image/png');
siteiconUploader.bindEvents();

document.getElementById('btn-upload-siteicon').addEventListener('click', async function () {
    await siteiconUploader.uploadImage('/siteicon');
});

initializeSettings();
