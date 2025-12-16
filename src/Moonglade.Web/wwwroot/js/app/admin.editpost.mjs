import { fetch2 } from './httpService.mjs?v=1500'
import { parseMetaContent, toMagicJson } from './utils.module.mjs'
import { success, error } from './toastService.mjs'
import { initEvents, loadTinyMCE, keepAlive, warnDirtyForm } from './admin.editor.module.mjs'

const btnSubmitPostSelector = '#btn-save';
const heroImageInputSelector = '#ViewModel_HeroImageUrl';
const heroImageFormSelector = '#form-hero-image';
const postEditFormSelector = '.post-edit-form';
const heroImageModalElement = document.getElementById('heroImageModal');
const editorChoice = parseMetaContent('editor-choice');
const scheduledPublishTimeElement = document.querySelector('input[name="ViewModel.ScheduledPublishTime"]');
const postIdElement = document.querySelector('input[name="ViewModel.PostId"]');

let isPreviewRequired = false;
const heroImageModal = new bootstrap.Modal(heroImageModalElement);

const handleKeyboardShortcuts = (event) => {
    if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 's') {
        event.preventDefault();
        document.querySelector(btnSubmitPostSelector).click();
    }
};

window.addEventListener('keydown', handleKeyboardShortcuts);

window.ajaxImageUpload = async (formElement) => {
    const formData = new FormData(formElement);

    try {
        const response = await fetch(formElement.action, {
            method: 'POST',
            body: formData,
        });

        if (!response.ok) {
            throw new Error('Failed to upload image');
        }

        const data = await response.json();
        document.querySelector(heroImageInputSelector).value = data.location;

        // Reset form and hide modal
        document.querySelector(heroImageFormSelector).reset();
        heroImageModal.hide();
    } catch (err) {
        error('Image upload failed.');
        console.error(err);
    }
};

const handlePostSubmit = async (event) => {
    event.preventDefault();

    isPreviewRequired = event.submitter.id === 'btn-preview';

    const formData = new FormData(event.target);
    const formValues = Object.fromEntries(formData.entries());

    // Validate content
    const content = formValues['ViewModel.EditorContent'];
    if (!content) {
        error('Please enter content.');
        return;
    }

    // Collect selected category IDs
    const selectedCatIds = formData.getAll('SelectedCatIds');
    formValues['SelectedCatIds'] = selectedCatIds;

    // Convert form data to the required JSON format
    const requestData = toMagicJson(formValues);

    const btnSubmitPost = document.querySelector(btnSubmitPostSelector);
    btnSubmitPost.classList.add('disabled');
    btnSubmitPost.setAttribute('disabled', 'disabled');

    var respJson = await fetch2(event.currentTarget.action, 'POST', requestData);

    if (respJson.postId) {
        postIdElement.value = respJson.postId;
        success('Post saved successfully.');

        if (isPreviewRequired) {
            isPreviewRequired = false;
            window.open(`/admin/post/preview/${respJson.postId}`);
        }
    }

    btnSubmitPost.classList.remove('disabled');
    btnSubmitPost.removeAttribute('disabled');
};

async function UnpublishPost(postId) {
    await fetch2(`/api/post/${postId}/unpublish`, 'PUT', {});

    success('Post unpublished');
    location.reload();
}

function setInputDateTime(dateObj, inputElement) {
    const pad = n => n < 10 ? '0' + n : n;

    const year = dateObj.getFullYear();
    const month = pad(dateObj.getMonth() + 1); // Months are zero-based!
    const day = pad(dateObj.getDate());
    const hours = pad(dateObj.getHours());
    const minutes = pad(dateObj.getMinutes());

    // No seconds in datetime-local value attribute
    const localDatetime = `${year}-${month}-${day}T${hours}:${minutes}`;
    inputElement.value = localDatetime;
}

function setMinScheduleDate() {
    const now = new Date();
    const minDate = now.toISOString().slice(0, 16); // Format: YYYY-MM-DDTHH:MM
    scheduledPublishTimeElement.setAttribute('min', minDate);
}

function updateScheduleInfo() {
    const postStatus = document.querySelector('input[name="ViewModel.PostStatus"]').value;

    const scheduleInfoDiv = document.querySelector('.schedule-info');
    const scheduledTime = scheduledPublishTimeElement.value;
    const scheduledTimeUtc = document.querySelector('input[name="ViewModel.ScheduledPublishTimeUtc"]').value;

    if (postStatus === 'scheduled') {
        let displayTime;

        if (scheduledTime) {
            displayTime = new Date(scheduledTime).toLocaleString();
        }
        else if (scheduledTimeUtc) {
            const utcDate = new Date(scheduledTimeUtc);
            const localDate = new Date(utcDate.getTime() - utcDate.getTimezoneOffset() * 60000);
            displayTime = localDate.toLocaleString();

            setInputDateTime(localDate, scheduledPublishTimeElement);
        }

        scheduleInfoDiv.innerHTML = `<i class="bi-clock"></i> <span>Scheduled for: ${displayTime}</span>`;
    } else {
        scheduleInfoDiv.innerHTML = '';
    }
}

document.addEventListener('DOMContentLoaded', function () {
    const warnSlugModification = parseMetaContent('warn-slug-modification');
    initEvents(!warnSlugModification);

    if (editorChoice === 'html') {
        loadTinyMCE(".post-content-textarea");
    }
    if (editorChoice === 'markdown') {
        require(['vs/editor/editor.main'], function () {
            window.mdContentEditor = initEditor('markdown-content-editor', ".post-content-textarea", 'markdown');

            inlineAttachment.editors.monaco.attach(
                window.mdContentEditor, document.getElementsByClassName('md-editor-image-upload-area')[0], {
                uploadUrl: '/image',
                urlText: '![file]({filename})',
                onFileUploadResponse: function (xhr) {
                    var result = JSON.parse(xhr.responseText),
                        filename = result[this.settings.jsonFieldName];

                    if (result && filename) {
                        var newValue;
                        if (typeof this.settings.urlText === 'function') {
                            newValue = this.settings.urlText.call(this, filename, result);
                        } else {
                            newValue = this.settings.urlText.replace(this.filenameTag, filename);
                        }
                        var text = this.editor.getValue().replace(this.lastValue, newValue);
                        this.editor.setValue(text);
                        this.settings.onFileUploaded.call(this, filename);
                    }
                    return false;
                }
            });
        });
    }

    keepAlive();
    warnDirtyForm('.post-edit-form');
});

document.getElementById('btn-unpublish-post').addEventListener('click', async function () {
    const postId = postIdElement.value;
    await UnpublishPost(postId);
});

document.getElementById('btn-cancel-schedule').addEventListener('click', function () {
    scheduledPublishTimeElement.value = '';
    document.querySelector('input[name="ViewModel.PostStatus"]').value = 'draft';

    updateScheduleInfo();
});

document.getElementById('btn-schedule-publish').addEventListener('click', function () {
    document.querySelector('input[name="ViewModel.PostStatus"]').value = 'scheduled';

    updateScheduleInfo();
});

setMinScheduleDate();
updateScheduleInfo();

const timeZone = Intl.DateTimeFormat().resolvedOptions().timeZone;
document.querySelector('input[name="ViewModel.ClientTimeZoneId"]').value = timeZone;

const postEditForm = document.querySelector(postEditFormSelector);
postEditForm.addEventListener('submit', handlePostSubmit);