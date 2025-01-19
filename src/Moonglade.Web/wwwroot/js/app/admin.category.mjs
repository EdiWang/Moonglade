import { success } from './toastService.mjs'

function deleteCat(catid) {
    callApi(`/api/category/${catid}`, 'DELETE', {},
        (resp) => {
            document.querySelector(`#card-${catid}`).remove();
            success('Category deleted');
        });
}

function confirmDelete(catid) {
    var cfm = confirm("Delete?");
    if (cfm) deleteCat(catid);
}

function handleSubmit(event) {
    event.preventDefault();

    var apiAddress = '';
    var verb = '';

    if (catId == window.emptyGuid) {
        apiAddress = '/api/category';
        verb = 'POST';
    }
    else {
        apiAddress = `/api/category/${catId}`;
        verb = 'PUT';
    }

    const data = new FormData(event.target);
    const value = Object.fromEntries(data.entries());

    callApi(apiAddress, verb,
        {
            slug: value["EditCategoryRequest.Slug"],
            displayName: value["EditCategoryRequest.DisplayName"],
            note: value["EditCategoryRequest.Note"]
        },
        (resp) => {
            document.querySelector('#edit-form').reset();
            editCanvas.hide();
            window.location.reload();
        });
}

document.querySelectorAll('.btn-delete').forEach(button => {
    button.addEventListener('click', function () {
        const lid = this.getAttribute('data-catid');
        confirmDelete(lid);
    });
});

const form = document.querySelector('#edit-form');
form.addEventListener('submit', handleSubmit);