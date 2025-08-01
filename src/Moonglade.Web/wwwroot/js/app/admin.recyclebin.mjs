import { moongladeFetch2 } from './httpService.mjs'
import { formatUtcTime } from './utils.module.mjs'
import { success } from './toastService.mjs'

async function deletePost(postid) {
    await moongladeFetch2(`/api/post/${postid}/destroy`, 'DELETE', {});

    document.querySelector(`#post-${postid}`).remove();
    success('Post deleted');
}

async function restorePost(postid) {
    await moongladeFetch2(`/api/post/${postid}/restore`, 'POST', {});

    document.querySelector(`#post-${postid}`).remove();
    success('Post restored');
}

document.querySelectorAll('.btn-delete').forEach(function (button) {
    button.addEventListener('click', async function () {
        var cfm = confirm('Delete Confirmation?');
        if (cfm) {
            await deletePost(this.getAttribute('data-postid'));
        }
    });
});

document.querySelectorAll('.btn-restore').forEach(function (button) {
    button.addEventListener('click', async function () {
        await restorePost(this.getAttribute('data-postid'));
    });
});

document.querySelectorAll('.btn-empty-recbin').forEach(function (button) {
    button.addEventListener('click', async function () {
        await moongladeFetch2('/api/post/recyclebin', 'DELETE', {});

        success('Cleared');
        setTimeout(function () {
            window.location.reload();
        }, 800);
    });
});

formatUtcTime();