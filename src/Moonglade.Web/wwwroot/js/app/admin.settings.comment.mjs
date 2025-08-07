import { handleSettingsSubmit } from './admin.settings.mjs';

const builtInRadio = document.getElementById('BuiltIn');
const thirdPartyRadio = document.getElementById('ThirdParty');
const builtInSettings = document.querySelector('.comment-settings-built-in');
const thirdPartySettings = document.querySelector('.comment-settings-3rd');
const form = document.getElementById('form-settings');

const toggleCommentSettingsUI = () => {
    if (builtInRadio?.checked) {
        builtInSettings && (builtInSettings.style.display = 'block');
        thirdPartySettings && (thirdPartySettings.style.display = 'none');
    } else {
        builtInSettings && (builtInSettings.style.display = 'none');
        thirdPartySettings && (thirdPartySettings.style.display = 'block');
    }
};

form?.addEventListener('submit', handleSettingsSubmit);
builtInRadio?.addEventListener('change', toggleCommentSettingsUI);
thirdPartyRadio?.addEventListener('change', toggleCommentSettingsUI);

toggleCommentSettingsUI();