import { handleSettingsSubmit } from './admin.settings.mjs';

const form = document.querySelector('#form-settings');
form.addEventListener('submit', handleSettingsSubmit);