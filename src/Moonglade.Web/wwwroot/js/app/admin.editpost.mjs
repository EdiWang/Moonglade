import { toMagicJson } from '/js/app/utils.module.mjs'
import { success, error } from '/js/app/blogtoast.module.mjs'

var btnSubmitPost = '#btn-save';
var isPreviewRequired = false;

window.addEventListener('keydown', function (event) {
    if (event.ctrlKey || event.metaKey) {
        switch (String.fromCharCode(event.which).toLowerCase()) {
            case 's':
                event.preventDefault();
                document.getElementById('btn-save').click();
                break;
        }
    }
});

var heroImageModal = new bootstrap.Modal(document.getElementById('heroImageModal'));

window.ajaxImageUpload = function (oFormElement) {
    const formData = new FormData(oFormElement);

    fetch(oFormElement.action,
        {
            method: 'POST',
            body: formData
        }).then(async (response) => {
            if (!response.ok) {
                error('API Boom');
                console.error(err);
            } else {
                var data = await response.json();
                document.querySelector('#ViewModel_HeroImageUrl').value = data.location;
            }
        }).then(response => {
            document.querySelector('#form-hero-image').reset();
            heroImageModal.hide();
        }).catch(err => {
            error(err);
            console.error(err);
        });
}

function handlePostSubmit(event) {
    event.preventDefault();

    isPreviewRequired = event.submitter.id == 'btn-preview';

    const data = new FormData(event.target);
    const value = Object.fromEntries(data.entries());

    const content = value["ViewModel.EditorContent"];
    if (!content) {
        error('Please enter content.');
        return;
    }

    var selectedCatIds = data.getAll('SelectedCatIds');

    value["SelectedCatIds"] = selectedCatIds;
    const newValue = toMagicJson(value);

    document.querySelector(btnSubmitPost).classList.add('disabled');
    document.querySelector(btnSubmitPost).setAttribute('disabled', 'disabled');

    callApi(event.currentTarget.action,
        'POST',
        newValue,
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
            document.querySelector(btnSubmitPost).classList.remove('disabled');
            document.querySelector(btnSubmitPost).removeAttribute('disabled');
        });
}

const form = document.querySelector('.post-edit-form');
form.addEventListener('submit', handlePostSubmit);
