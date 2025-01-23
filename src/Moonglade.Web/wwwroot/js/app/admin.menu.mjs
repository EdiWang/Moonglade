import { handleSettingsSubmit } from './admin.settings.mjs';

let jsonContentEditor = null;

require(['vs/editor/editor.main'], function () {
    jsonContentEditor = initEditor('JsonContentEditor', "#settings_MenuJson", 'json');
});

function clearMenus() {
    jsonContentEditor.setValue("[]");
    document.querySelector("#settings_MenuJson").value = "[]";
}

function handleSubmit(event) {
    assignEditorValues(jsonContentEditor, "#settings_MenuJson");
    handleSettingsSubmit(event);
}

document.getElementById('btn-clear-menus').addEventListener('click', clearMenus);

const form = document.querySelector('#form-settings');
form.addEventListener('submit', handleSubmit);