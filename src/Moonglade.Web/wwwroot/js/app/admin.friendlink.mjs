import { moongladeFetch2 } from './httpService.mjs?v=1426b2'
import { success } from './toastService.mjs'

const editCanvas = new bootstrap.Offcanvas(document.getElementById('editLinkCanvas'));
let fid = window.emptyGuid;

function initCreateFriendLink() {
    fid = window.emptyGuid;
    document.querySelector('#edit-form').reset();
    editCanvas.show();
}

async function editFriendLink(id) {
    const data = await moongladeFetch2(`/api/friendlink/${id}`, 'GET', {});

    fid = id;
    document.querySelector('#EditLinkRequest_Title').value = data.title;
    document.querySelector('#EditLinkRequest_LinkUrl').value = data.linkUrl;
    document.querySelector('#EditLinkRequest_Rank').value = data.rank;
    editCanvas.show();
}

async function deleteFriendLink(friendlinkid) {
    await moongladeFetch2(`/api/friendlink/${friendlinkid}`, 'DELETE', {});

    document.querySelector(`#tr-${friendlinkid}`).remove();
    success('Friend link deleted');
}

async function deleteFriendLinkConfirm(id) {
    var cfm = confirm("Delete Confirmation?");
    if (cfm) await deleteFriendLink(id);
}

async function handleSubmit(event) {
    event.preventDefault();

    var httpVerb = fid == window.emptyGuid ? 'POST' : 'PUT';
    var apiAddress = fid == window.emptyGuid ? `/api/friendlink` : `/api/friendlink/${fid}`;

    const data = new FormData(event.target);
    const value = Object.fromEntries(data.entries());

    await moongladeFetch2(apiAddress, httpVerb,
        {
            id: fid,
            title: value["EditLinkRequest.Title"],
            linkUrl: value["EditLinkRequest.LinkUrl"],
            rank: value["EditLinkRequest.Rank"]
        });

    document.querySelector('#edit-form').reset();

    const modalElement = document.getElementById(`editFriendlinkModal`);
    const modal = bootstrap.Modal.getInstance(modalElement);
    if (modal) modal.hide();

    window.location.reload();
}

document.querySelector('#btn-new-friendlink').addEventListener('click', initCreateFriendLink);

document.querySelectorAll('.btn-edit').forEach(button => {
    button.addEventListener('click', async function () {
        const lid = this.getAttribute('data-linkid');
        await editFriendLink(lid);
    });
});

document.querySelectorAll('.btn-delete').forEach(button => {
    button.addEventListener('click', async function () {
        const lid = this.getAttribute('data-linkid');
        await deleteFriendLinkConfirm(lid);
    });
});

const form = document.querySelector('#edit-form');
form.addEventListener('submit', handleSubmit);