import { moongladeFetch } from './httpService.mjs'
import { parseMetaContent, toMagicJson } from './utils.module.mjs'
import { success, error } from './toastService.mjs'
import { initEvents, loadTinyMCE, keepAlive, warnDirtyForm } from './admin.editor.module.mjs'

const btnSubmitPostSelector = '#btn-save';
const heroImageInputSelector = '#ViewModel_HeroImageUrl';
const heroImageFormSelector = '#form-hero-image';
const postEditFormSelector = '.post-edit-form';
const heroImageModalElement = document.getElementById('heroImageModal');
const editorChoice = parseMetaContent('editor-choice');

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

    moongladeFetch(event.currentTarget.action,
        'POST',
        requestData,
        async (resp) => {
            var respJson = await resp.json();
            if (respJson.postId) {
                document.querySelector('input[name="ViewModel.PostId"]').value = respJson.postId;
                success('Post saved successfully.');

                if (isPreviewRequired) {
                    isPreviewRequired = false;
                    window.open(`/admin/post/preview/${respJson.postId}`);
                }
            }
        }, function (resp) {
            btnSubmitPost.classList.remove('disabled');
            btnSubmitPost.removeAttribute('disabled');
        });
};

function UnpublishPost(postId) {
    moongladeFetch(
        `/api/post/${postId}/unpublish`,
        'PUT',
        {},
        (resp) => {
            success('Post unpublished');
            location.reload();
        });
}

function setMinScheduleDate() {
    const now = new Date();
    const minDate = now.toISOString().slice(0, 16); // Format: YYYY-MM-DDTHH:MM
    document.querySelector('input[name="ViewModel.ScheduledPublishTime"]').setAttribute('min', minDate);
}

function updateScheduleInfo() {
    const postStatus = document.querySelector('input[name="ViewModel.PostStatus"]').value;

    const scheduleInfoDiv = document.querySelector('.schedule-info');
    const scheduledTime = document.querySelector('input[name="ViewModel.ScheduledPublishTime"]').value;

    if (postStatus === 'scheduled') {
        scheduleInfoDiv.innerHTML = `<i class="bi-clock"></i> <span>Scheduled for: ${new Date(scheduledTime).toLocaleString()}</span>`;
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

document.getElementById('btn-unpublish-post').addEventListener('click', function () {
    const postId = document.querySelector('input[name="ViewModel.PostId"]').value;
    UnpublishPost(postId);
});

document.getElementById('btn-cancel-schedule').addEventListener('click', function () {
    document.querySelector('input[name="ViewModel.ScheduledPublishTime"]').value = '';
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