import { handleSettingsSubmit } from './admin.settings.js';

const jsonValue = document.getElementById('settings_JsonData').value;
let links = jsonValue ? JSON.parse(jsonValue) : [];
let editIndex = null;

function renderTable() {
    const tbody = document.getElementById('linksTable').getElementsByTagName('tbody')[0];
    tbody.innerHTML = '';
    links.forEach((link, index) => {
        const row = tbody.insertRow();
        row.insertCell(0).textContent = link.name;
        row.insertCell(1).innerHTML = `<i class="${link.icon}"></i> <span class="text-muted">${link.icon}</span>`;
        row.insertCell(2).innerHTML = `<a href="${link.url}" target="_blank">${link.url}</a>`;
        const actions = row.insertCell(3);
        actions.innerHTML = `<button type="button" class="btn btn-sm btn-outline-accent me-1" onclick="editLink(${index})"><i class="bi-pen"></i></button><button type="button" class="btn btn-sm btn-outline-danger" onclick="deleteLink(${index})"><i class="bi-trash"></i></button>`;
    });

    updateTextareaValue();
}

function updateTextareaValue() {
    document.getElementById('settings_JsonData').value = JSON.stringify(links);
}

function addOrUpdateLink() {
    const name = document.getElementById('name').value.trim();
    const icon = document.getElementById('icon').value.trim();
    const url = document.getElementById('url').value.trim();
    const error = document.getElementById('error');

    if (!name || !icon || !url) {
        error.textContent = 'All fields are required!';
        return;
    }

    if (links.some((link, index) => link.name === name && index !== editIndex)) {
        error.textContent = 'Name must be unique!';
        return;
    }

    if (!isValidUrl(url)) {
        error.textContent = 'Invalid URL!';
        return;
    }

    if (editIndex !== null) {
        links[editIndex] = { name: name, icon: icon, url: url };
        editIndex = null;
    } else {
        links.push({ name: name, icon: icon, url: url });
    }

    clearForm();
    renderTable();
}

window.editLink = function (index) {
    const link = links[index];
    document.getElementById('name').value = link.name;
    document.getElementById('icon').value = link.icon;
    document.getElementById('url').value = link.url;
    editIndex = index;
}

window.deleteLink = function (index) {
    links.splice(index, 1);
    renderTable();
}

function clearForm() {
    document.getElementById('name').value = '';
    document.getElementById('icon').value = '';
    document.getElementById('url').value = '';
    document.getElementById('error').textContent = '';

    editIndex = null;
}

function isValidUrl(url) {
    try {
        new URL(url);
        return true;
    } catch {
        return false;
    }
}

document.getElementById('btn-update').addEventListener('click', addOrUpdateLink);
document.getElementById('btn-cancel').addEventListener('click', clearForm);

const form = document.querySelector('#form-settings');
form.addEventListener('submit', handleSettingsSubmit);

renderTable();