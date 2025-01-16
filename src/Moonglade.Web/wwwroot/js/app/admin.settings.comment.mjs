import { handleSettingsSubmit } from './admin.settings.js';
function toggleCommentSettingsUI() {
    if (document.querySelector('#BuiltIn').checked) {
        document.querySelector('.comment-settings-built-in').style.display = 'block';
        document.querySelector('.comment-settings-3rd').style.display = 'none';
    } else {
        document.querySelector('.comment-settings-built-in').style.display = 'none';
        document.querySelector('.comment-settings-3rd').style.display = 'block';
    }
}

const form = document.querySelector('#form-settings');
form.addEventListener('submit', handleSettingsSubmit);

document.getElementById('BuiltIn').addEventListener('change', toggleCommentSettingsUI);
document.getElementById('ThirdParty').addEventListener('change', toggleCommentSettingsUI);

toggleCommentSettingsUI();