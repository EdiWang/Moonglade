import { initializeSettings } from './admin.settings.mjs';

const builtInRadio = document.getElementById('BuiltIn');
const builtInSettings = document.querySelector('.comment-settings-built-in');
const thirdPartySettings = document.querySelector('.comment-settings-3rd');

const toggleCommentSettingsUI = () => {
    if (builtInRadio?.checked) {
        builtInSettings && (builtInSettings.style.display = 'block');
        thirdPartySettings && (thirdPartySettings.style.display = 'none');
    } else {
        builtInSettings && (builtInSettings.style.display = 'none');
        thirdPartySettings && (thirdPartySettings.style.display = 'block');
    }
};

initializeSettings({ onSaved: toggleCommentSettingsUI });
toggleCommentSettingsUI();
