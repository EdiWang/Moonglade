import { initializeSettings } from './admin.settings.mjs?v=3';

const enableSubscription = document.getElementById('settings_EnableSubscription');
const feedOptions = document.getElementById('settings-feed-options');

initializeSettings({
    fieldBindings: {
        EnableSubscription: {
            read: () => enableSubscription.checked,
            write: value => {
                enableSubscription.checked = value;
                feedOptions.disabled = !value;
            }
        }
    }
});
