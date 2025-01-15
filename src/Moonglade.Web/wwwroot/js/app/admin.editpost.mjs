import { toMagicJson } from '/js/app/utils.module.mjs'
import { success, error } from '/js/app/blogtoast.module.mjs'

const btnSubmitPostSelector = '#btn-save';
const heroImageInputSelector = '#ViewModel_HeroImageUrl';
const heroImageFormSelector = '#form-hero-image';
const postEditFormSelector = '.post-edit-form';
const heroImageModalElement = document.getElementById('heroImageModal');

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

    callApi(event.currentTarget.action,
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

const postEditForm = document.querySelector(postEditFormSelector);
postEditForm.addEventListener('submit', handlePostSubmit);