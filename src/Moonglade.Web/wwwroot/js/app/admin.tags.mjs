import { moongladeFetch } from './httpService.mjs';
import { success, error } from './toastService.mjs';

const editCanvas = new bootstrap.Offcanvas(document.getElementById('editTagCanvas'));
const tagList = document.querySelector('.ul-tag-mgr');
const editForm = document.querySelector('#edit-form');
const tagFilter = document.getElementById('tagFilter');
const btnNewTag = document.getElementById('btn-new-tag');

function showEditCanvas() {
    editForm.reset();
    editCanvas.show();
}

tagList.addEventListener('click', async (e) => {
    const btn = e.target.closest('.btn-delete');
    if (!btn) return;
    const tagid = btn.getAttribute('data-tagid');
    const tagName = btn.textContent.trim();
    if (!window.confirm(`Confirm to delete tag: ${tagName}`)) return;
    try {
        await moongladeFetch(`/api/tags/${tagid}`, 'DELETE');
        const li = document.querySelector(`#li-tag-${tagid}`);
        if (li) li.style.display = 'none';
        success('Tag deleted');
    } catch (err) {
        error('Tag deletion failed.');
    }
});

tagList.addEventListener('blur', async (e) => {
    const span = e.target.closest('.span-tagcontent-editable');
    if (!span) return;
    const tagId = span.getAttribute('data-tagid');
    const newTagName = span.textContent.trim();
    const originalTagName = span.getAttribute('data-original') || '';
    if (newTagName === originalTagName || !newTagName) return;
    try {
        await moongladeFetch(`/api/tags/${tagId}`, 'PUT', newTagName);
        span.setAttribute('data-original', newTagName);
        success('Tag updated');
    } catch (err) {
        error('Tag update failed.');
    }
}, true); // useCapture: true, to catch blur

tagFilter.addEventListener('keyup', function () {
    const value = this.value.toLowerCase();
    tagList.querySelectorAll('li').forEach((item) => {
        const show = item.textContent.toLowerCase().includes(value);
        item.style.display = show ? 'inline-block' : 'none';
    });
});

btnNewTag.addEventListener('click', showEditCanvas);

editForm.addEventListener('submit', async function (event) {
    event.preventDefault();
    const formData = new FormData(editForm);
    const tagName = formData.get('tagName').trim();
    
    try {
        const resp = await moongladeFetch(`/api/tags`, 'POST', tagName);
        editForm.reset();
        //insertNewTagElement(resp.id, tagName);
        success('Tag added');
        editCanvas.hide();

        window.location.reload();
    } catch (err) {
        console.error(err);
        error('Tag creation failed.');
    }
});

//function insertNewTagElement(id, name) {
//    const li = document.createElement('li');
//    li.id = `li-tag-${id}`;
//    li.innerHTML = `
//        <span class="span-tagcontent-editable" contenteditable="true" data-tagid="${id}" data-original="${name}">${name}</span>
//        <button class="btn btn-danger btn-delete" data-tagid="${id}">Delete</button>
//    `;
//    tagList.appendChild(li);
//}
