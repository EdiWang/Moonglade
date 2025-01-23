import { callApi } from './httpService.mjs'
import { success } from './toastService.mjs'

const editCanvas = new bootstrap.Offcanvas(document.getElementById('editCatCanvas'));
let catId = window.emptyGuid;

function initCreateCategory() {
    catId = window.emptyGuid;
    document.querySelector('#edit-form').reset();
    editCanvas.show();
}

function editCat(id) {
    callApi(`/api/category/${id}`, 'GET', {},
        async (resp) => {
            var data = await resp.json();
            catId = data.id;
            document.querySelector('#EditCategoryRequest_Slug').value = data.slug;
            document.querySelector('#EditCategoryRequest_DisplayName').value = data.displayName;
            document.querySelector('#EditCategoryRequest_Note').value = data.note;
            editCanvas.show();
        });
}

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

document.querySelector('#btn-new-cat').addEventListener('click', initCreateCategory);

document.querySelectorAll('.btn-edit').forEach(button => {
    button.addEventListener('click', function () {
        const lid = this.getAttribute('data-catid');
        editCat(lid);
    });
});

document.querySelectorAll('.btn-delete').forEach(button => {
    button.addEventListener('click', function () {
        const lid = this.getAttribute('data-catid');
        confirmDelete(lid);
    });
});

const form = document.querySelector('#edit-form');
form.addEventListener('submit', handleSubmit);