import { callApi } from './httpService.mjs'
import { success } from './toastService.mjs';

var editCanvas = new bootstrap.Offcanvas(document.getElementById('editTagCanvas'));

function initCreateTag() {
    document.querySelector('#edit-form').reset();
    editCanvas.show();
}

document.querySelectorAll(".btn-delete").forEach(function (element) {
    element.addEventListener("click", function () {
        var tagid = this.getAttribute("data-tagid");
        var cfm = confirm(`Confirm to delete tag: ${this.textContent.trim()}`);
        if (cfm) {
            callApi(`/api/tags/${tagid}`, 'DELETE', {}, function (resp) {
                document.querySelector(`#li-tag-${tagid}`).style.display = 'none';
                success('Tag deleted');
            });
        }
    });
});

document.querySelectorAll(".span-tagcontent-editable").forEach(function (element) {
    element.addEventListener("blur", function () {
        var tagId = this.getAttribute("data-tagid");
        var newTagName = this.textContent.trim();

        callApi(`/api/tags/${tagId}`, 'PUT', newTagName, function (resp) {
            success('Tag updated');
        });
    });
});

document.getElementById('tagFilter').addEventListener('keyup', function () {
    var value = this.value.toLowerCase();
    var items = document.querySelectorAll('.ul-tag-mgr li');

    items.forEach(function (item) {
        var show = item.textContent.toLowerCase().indexOf(value) > -1;
        item.style.display = show ? 'inline-block' : 'none';
    });
});

function handleSubmit(event) {
    event.preventDefault();

    const data = new FormData(event.target);
    const value = Object.fromEntries(data.entries());

    callApi(`/api/tags`, 'POST', value.tagName, function (resp) {
        document.querySelector('#edit-form').reset();
        window.location.reload();
    });
}

document.getElementById('btn-new-tag').addEventListener('click', initCreateTag);

const form = document.querySelector('#edit-form');
form.addEventListener('submit', handleSubmit);