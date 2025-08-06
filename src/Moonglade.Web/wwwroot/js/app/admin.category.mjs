import { moongladeFetch2 } from './httpService.mjs?v=1426'
import { success } from './toastService.mjs'

const editCanvas = new bootstrap.Offcanvas(document.getElementById('editCatCanvas'));
let catId = window.emptyGuid;

function initCreateCategory() {
    catId = window.emptyGuid;
    document.querySelector('#edit-form').reset();
    editCanvas.show();
}

async function editCat(id) {
    var data = await moongladeFetch2(`/api/category/${id}`, 'GET');
    catId = data.id;
    document.querySelector('#EditCategoryRequest_Slug').value = data.slug;
    document.querySelector('#EditCategoryRequest_DisplayName').value = data.displayName;
    document.querySelector('#EditCategoryRequest_Note').value = data.note;
    editCanvas.show();
}

async function deleteCat(catid) {
    await moongladeFetch2(`/api/category/${catid}`, 'DELETE', {});
    document.querySelector(`#card-${catid}`).remove();
    success('Category deleted');
}

async function confirmDelete(catid) {
    var cfm = confirm("Delete?");
    if (cfm) await deleteCat(catid);
}

async function handleSubmit(event) {
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

    await moongladeFetch2(apiAddress, verb,
        {
            slug: value["EditCategoryRequest.Slug"],
            displayName: value["EditCategoryRequest.DisplayName"],
            note: value["EditCategoryRequest.Note"]
        });

    document.querySelector('#edit-form').reset();
    editCanvas.hide();
    window.location.reload();
}

document.querySelector('#btn-new-cat').addEventListener('click', initCreateCategory);

document.querySelectorAll('.btn-edit').forEach(button => {
    button.addEventListener('click', async function () {
        const lid = this.getAttribute('data-catid');
        await editCat(lid);
    });
});

document.querySelectorAll('.btn-delete').forEach(button => {
    button.addEventListener('click', async function () {
        const lid = this.getAttribute('data-catid');
        await confirmDelete(lid);
    });
});

const form = document.querySelector('#edit-form');
form.addEventListener('submit', handleSubmit);