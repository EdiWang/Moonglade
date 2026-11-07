import { fetch2 } from './httpService.mjs?v=1500';
import { success } from './toastService.mjs';

const fieldName = control => control.name.split('.').at(-1);

function fieldValue(control) {
    if (control.type === 'checkbox') return control.checked;
    if (control.type === 'number' || control.type === 'range') return control.valueAsNumber;
    if (control.hasAttribute('data-setting-number')) return Number(control.value);
    return control.value;
}

function setFieldValue(controls, value) {
    for (const control of controls) {
        if (control.type === 'radio') control.checked = fieldValue(control) === value;
        else if (control.type === 'checkbox') control.checked = value;
        else control.value = value;
    }
}

export function initializeSettings({ createEditor, onSaved } = {}) {
    const form = document.getElementById('form-settings');
    const dialog = document.getElementById('setting-edit-modal');
    const modal = new bootstrap.Modal(dialog);
    const dialogForm = dialog.querySelector('form');
    const dialogTitle = dialog.querySelector('.modal-title');
    const dialogError = document.getElementById('setting-edit-error');
    const fieldHost = document.getElementById('setting-edit-field');
    const saveButton = dialogForm.querySelector('[type=submit]');
    const cancelButton = dialogForm.querySelector('[data-bs-dismiss]');
    const saveText = saveButton.textContent;

    const editFields = new Map();
    const controlsByField = new Map();
    const confirmed = new Map();
    const fieldErrors = new Map();
    let currentEdit;
    let editor;
    let saving = false;

    form.addEventListener('submit', event => event.preventDefault());

    function showError(element, message) {
        element.textContent = message;
        element.hidden = !message;
    }

    function getFieldError(name, control) {
        if (!fieldErrors.has(name)) {
            const message = document.createElement('div');
            message.className = 'col-12 text-danger small mt-2';
            message.setAttribute('role', 'alert');
            message.hidden = true;
            (control.closest('.settings-entry') ?? control.parentElement).append(message);
            fieldErrors.set(name, message);
        }
        return fieldErrors.get(name);
    }

    async function save(fields) {
        await fetch2(form.action, 'PATCH', fields);
        for (const [name, value] of Object.entries(fields)) {
            confirmed.set(name, value);
            const edit = editFields.get(name);
            if (edit) edit.preview.textContent = value === '' ? '—' : value;
            else setFieldValue(controlsByField.get(name), value);
        }
        success(dialog.dataset.updated);
        onSaved?.(fields);
    }

    function openEditor(edit, trigger, dependency) {
        const { control, label, name } = edit;
        currentEdit = { edit, trigger, dependency, required: control.required };
        dialogTitle.textContent = label;
        dialog.querySelector('.modal-dialog').classList.toggle('modal-lg', control.tagName === 'TEXTAREA');
        control.value = confirmed.get(name);
        control.required ||= Boolean(dependency);
        fieldHost.replaceChildren(control);
        showError(dialogError, '');
        modal.show(trigger);
    }

    for (const control of form.querySelectorAll('[data-setting-edit]')) {
        const name = fieldName(control);
        const label = control.dataset.settingEdit || control.labels[0]?.textContent.trim() || name;
        const summary = document.createElement('div');
        summary.className = 'd-flex align-items-center justify-content-between gap-2';
        const preview = document.createElement('span');
        preview.className = 'text-body-secondary text-truncate';
        preview.textContent = control.value || '—';
        const button = document.createElement('button');
        button.type = 'button';
        button.className = 'btn btn-sm btn-outline-accent flex-shrink-0';
        button.textContent = dialog.dataset.change;
        button.setAttribute('aria-label', label + ' · ' + dialog.dataset.change);
        button.setAttribute('aria-haspopup', 'dialog');

        const edit = { control, name, label, preview };
        summary.append(preview, button);
        control.parentElement.classList.remove('form-floating');
        control.setAttribute('aria-labelledby', dialogTitle.id);
        control.setAttribute('aria-describedby', dialogError.id);
        control.replaceWith(summary);
        if (control.tagName === 'TEXTAREA') {
            control.classList.remove('h-100px');
            if (!control.hasAttribute('rows')) control.rows = 10;
        }

        button.addEventListener('click', () => openEditor(edit, button));
        editFields.set(name, edit);
        confirmed.set(name, fieldValue(control));
    }

    const directControls = form.querySelectorAll('select[name], input[type=checkbox][name], input[type=radio][name], input[type=range][name]');
    for (const control of directControls) {
        const name = fieldName(control);
        if (!controlsByField.has(name)) controlsByField.set(name, []);
        controlsByField.get(name).push(control);
        if (control.type !== 'radio' || control.checked) confirmed.set(name, fieldValue(control));
    }

    form.addEventListener('change', async event => {
        const control = event.target;
        const name = control.name && fieldName(control);
        const controls = controlsByField.get(name);
        if (!controls?.includes(control)) return;

        const value = fieldValue(control);
        const previous = confirmed.get(name);
        if (value === previous) return;

        const message = getFieldError(name, control);
        showError(message, '');
        const dependency = control.dataset.settingRequires;
        if (value === true && dependency) {
            const current = confirmed.get(dependency).trim();
            const needsHttps = editFields.get(dependency).control.type === 'url';
            if (!current || (needsHttps && (!/^https:\/\//i.test(current) || !URL.canParse(current)))) {
                setFieldValue(controls, previous);
                openEditor(editFields.get(dependency), control, name);
                return;
            }
        }

        const hadFocus = document.activeElement === control;
        for (const item of controls) item.disabled = true;
        try {
            await save({ [name]: value });
        } catch (err) {
            setFieldValue(controls, previous);
            showError(message, err);
        } finally {
            for (const item of controls) item.disabled = false;
            if (hadFocus && document.activeElement === document.body) {
                (controls.find(item => item.checked) ?? control).focus();
            }
        }
    });

    dialog.addEventListener('shown.bs.modal', () => {
        const { control, name } = currentEdit.edit;
        editor = createEditor?.(name, control);
        if (editor) editor.focus();
        else control.focus();
    });

    dialog.addEventListener('hide.bs.modal', event => {
        if (saving) event.preventDefault();
    });

    dialog.addEventListener('hidden.bs.modal', () => {
        const { edit, trigger, required } = currentEdit;
        editor?.destroy();
        editor = null;
        edit.control.required = required;
        fieldHost.replaceChildren();
        currentEdit = null;
        trigger.focus();
    });

    dialogForm.addEventListener('submit', async event => {
        event.preventDefault();
        if (saving) return;

        const { edit: { control, name }, dependency } = currentEdit;
        editor?.syncToTextarea();
        if (!editor && !control.reportValidity()) return;
        const value = fieldValue(control);
        if (!dependency && value === confirmed.get(name)) {
            modal.hide();
            return;
        }

        const fields = { [name]: value };
        if (dependency) fields[dependency] = true;
        showError(dialogError, '');
        saving = saveButton.disabled = cancelButton.disabled = fieldHost.inert = true;
        saveButton.textContent = dialog.dataset.saving;
        try {
            await save(fields);
        } catch (err) {
            showError(dialogError, err);
            return;
        } finally {
            saving = saveButton.disabled = cancelButton.disabled = fieldHost.inert = false;
            saveButton.textContent = saveText;
        }
        modal.hide();
    });
}
