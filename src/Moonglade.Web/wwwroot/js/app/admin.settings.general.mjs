import { initializeSettings } from './admin.settings.mjs?v=2';
import { ImageUploader } from './imageuploader.mjs';

var siteiconUploader = new ImageUploader('siteicon', 512, 'image/png');
siteiconUploader.bindEvents();

document.getElementById('btn-upload-siteicon').addEventListener('click', async function () {
    await siteiconUploader.uploadImage('/siteicon');
});

const showSidebar = document.getElementById('settings-show-sidebar');
const sidebarPosition = document.getElementById('ViewModel_SideBarOption');

initializeSettings({
    fieldBindings: {
        SideBarOption: {
            read: () => showSidebar.checked ? sidebarPosition.value : 'Disabled',
            write: value => {
                showSidebar.checked = value !== 'Disabled';
                if (showSidebar.checked) sidebarPosition.value = value;
            }
        }
    }
});
