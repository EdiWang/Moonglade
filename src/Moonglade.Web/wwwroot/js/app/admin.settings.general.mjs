import { handleSettingsSubmit } from './admin.settings.mjs';
import { ImageUploader } from './imageuploader.mjs';

var avatarUploader = new ImageUploader('avatar', 300, 'image/jpeg');
avatarUploader.bindEvents();

var siteiconUploader = new ImageUploader('siteicon', 512, 'image/png');
siteiconUploader.bindEvents();

bsCustomFileInput.init();
document.getElementById('btn-upload-avatar').addEventListener('click', function () {
    avatarUploader.uploadImage('/avatar');
});

document.getElementById('btn-upload-siteicon').addEventListener('click', function () {
    siteiconUploader.uploadImage('/siteicon');
});

const form = document.querySelector('#form-settings');
form.addEventListener('submit', handleSettingsSubmit);