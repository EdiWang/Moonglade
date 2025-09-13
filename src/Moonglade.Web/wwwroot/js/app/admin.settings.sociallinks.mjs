import { handleSettingsSubmit } from './admin.settings.mjs';

const jsonValue = document.getElementById('settings_JsonData').value;
let links = jsonValue ? JSON.parse(jsonValue) : [];
let editIndex = null;

// DOM elements cache
const elements = {
    tbody: document.getElementById('linksTable').getElementsByTagName('tbody')[0],
    nameInput: document.getElementById('name'),
    iconInput: document.getElementById('icon'),
    urlInput: document.getElementById('url'),
    errorDiv: document.getElementById('error'),
    errorMessage: document.getElementById('error-message'),
    updateBtn: document.getElementById('btn-update'),
    updateBtnText: document.getElementById('btn-update-text'),
    cancelBtn: document.getElementById('btn-cancel'),
    emptyState: document.getElementById('empty-state')
};

function renderTable() {
    elements.tbody.innerHTML = '';
    
    if (links.length === 0) {
        elements.emptyState.style.display = 'table-row';
        return;
    }
    
    elements.emptyState.style.display = 'none';
    
    links.forEach((link, index) => {
        const row = elements.tbody.insertRow();
        
        // Name cell
        const nameCell = row.insertCell(0);
        nameCell.textContent = link.name;
        nameCell.className = 'fw-medium';
        
        // Icon cell
        const iconCell = row.insertCell(1);
        iconCell.innerHTML = `
            <div class="d-flex align-items-center">
                <i class="${link.icon}" aria-hidden="true"></i>
                <small class="text-muted ms-2">${link.icon}</small>
            </div>
        `;
        
        // URL cell
        const urlCell = row.insertCell(2);
        urlCell.innerHTML = `<a href="${link.url}" target="_blank" rel="noopener" class="text-break">${link.url}</a>`;
        
        // Actions cell
        const actionsCell = row.insertCell(3);
        actionsCell.className = 'text-center';
        actionsCell.innerHTML = `
            <div class="btn-group" role="group" aria-label="Link actions">
                <button type="button" class="btn btn-sm btn-outline-primary" 
                        onclick="editLink(${index})" 
                        title="Edit ${link.name}"
                        aria-label="Edit ${link.name}">
                    <i class="bi-pen" aria-hidden="true"></i>
                </button>
                <button type="button" class="btn btn-sm btn-outline-danger" 
                        onclick="deleteLink(${index})" 
                        title="Delete ${link.name}"
                        aria-label="Delete ${link.name}">
                    <i class="bi-trash" aria-hidden="true"></i>
                </button>
            </div>
        `;
    });

    updateTextareaValue();
}

function updateTextareaValue() {
    document.getElementById('settings_JsonData').value = JSON.stringify(links);
}

function showError(message) {
    elements.errorMessage.textContent = message;
    elements.errorDiv.style.display = 'block';
    elements.errorDiv.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
}

function hideError() {
    elements.errorDiv.style.display = 'none';
}

function validateInputs() {
    const name = elements.nameInput.value.trim();
    const icon = elements.iconInput.value.trim();
    const url = elements.urlInput.value.trim();

    // Validation only for Add/Update button
    if (!name || !icon || !url) {
        showError('All fields are required!');
        return false;
    }

    if (name.length > 50) {
        showError('Name must be 50 characters or less!');
        return false;
    }

    if (icon.length > 100) {
        showError('Icon class must be 100 characters or less!');
        return false;
    }

    if (links.some((link, index) => link.name.toLowerCase() === name.toLowerCase() && index !== editIndex)) {
        showError('Name must be unique!');
        return false;
    }

    if (!isValidUrl(url)) {
        showError('Please enter a valid URL starting with http:// or https://');
        return false;
    }

    return true;
}

function addOrUpdateLink() {
    hideError();

    // Only validate when adding/updating individual links
    if (!validateInputs()) {
        return;
    }

    const name = elements.nameInput.value.trim();
    const icon = elements.iconInput.value.trim();
    const url = elements.urlInput.value.trim();

    // Add or update link
    const linkData = { name, icon, url };
    
    if (editIndex !== null) {
        links[editIndex] = linkData;
        editIndex = null;
    } else {
        links.push(linkData);
    }

    clearForm();
    renderTable();
    
    // Show success feedback
    const action = editIndex !== null ? 'updated' : 'added';
    showToast(`Social link "${name}" ${action} successfully!`, 'success');
}

window.editLink = function (index) {
    const link = links[index];
    elements.nameInput.value = link.name;
    elements.iconInput.value = link.icon;
    elements.urlInput.value = link.url;
    
    editIndex = index;
    elements.updateBtnText.textContent = 'Update';
    elements.cancelBtn.style.display = 'inline-block';
    
    hideError();
    elements.nameInput.focus();
}

window.deleteLink = function (index) {
    const link = links[index];
    if (confirm(`Are you sure you want to delete "${link.name}"?`)) {
        links.splice(index, 1);
        renderTable();
        showToast(`Social link "${link.name}" deleted successfully!`, 'info');
    }
}

function clearForm() {
    elements.nameInput.value = '';
    elements.iconInput.value = '';
    elements.urlInput.value = '';
    elements.updateBtnText.textContent = 'Add';
    elements.cancelBtn.style.display = 'none';
    
    editIndex = null;
    hideError();
}

function isValidUrl(url) {
    try {
        const urlObj = new URL(url);
        return urlObj.protocol === 'http:' || urlObj.protocol === 'https:';
    } catch {
        return false;
    }
}

function showToast(message, type = 'info') {
    // Integration with your existing toast service
    if (window.toastr) {
        toastr[type](message);
    } else {
        console.log(`${type.toUpperCase()}: ${message}`);
    }
}

// Custom form submission handler that bypasses validation
function handleFormSubmission(event) {
    // Clear any existing validation errors before saving
    hideError();
    
    // Always update the textarea value before submitting
    updateTextareaValue();
    
    // Call the shared settings submit handler
    handleSettingsSubmit(event);
}

// Event listeners
elements.updateBtn.addEventListener('click', addOrUpdateLink);
elements.cancelBtn.addEventListener('click', clearForm);

// Form submission - NO validation for Save button
const form = document.querySelector('#form-settings');
form.addEventListener('submit', handleFormSubmission);

// Allow Enter key to trigger Add/Update (with validation)
[elements.nameInput, elements.iconInput, elements.urlInput].forEach(input => {
    input.addEventListener('keypress', (e) => {
        if (e.key === 'Enter') {
            e.preventDefault();
            addOrUpdateLink(); // This will trigger validation
        }
    });
});

// Initialize
renderTable();