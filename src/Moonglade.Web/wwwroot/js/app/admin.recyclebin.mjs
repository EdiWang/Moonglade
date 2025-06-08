import { moongladeFetch } from './httpService.mjs'
import { formatUtcTime } from './utils.module.mjs'
import { success } from './toastService.mjs'

function deletePost(postid) {
    moongladeFetch(`/api/post/${postid}/destroy`, 'DELETE', {},
        (resp) => {
            document.querySelector(`#post-${postid}`).remove();
            success('Post deleted');
        });
}

function restorePost(postid) {
    moongladeFetch(`/api/post/${postid}/restore`, 'POST', {},
        (resp) => {
            document.querySelector(`#post-${postid}`).remove();
            success('Post restored');
        });
}

document.querySelectorAll('.btn-delete').forEach(function (button) {
    button.addEventListener('click', function () {
        var cfm = confirm('Delete Confirmation?');
        if (cfm) {
            deletePost(this.getAttribute('data-postid'));
        }
    });
});

document.querySelectorAll('.btn-restore').forEach(function (button) {
    button.addEventListener('click', function () {
        restorePost(this.getAttribute('data-postid'));
    });
});

document.querySelectorAll('.btn-empty-recbin').forEach(function (button) {
    button.addEventListener('click', function () {
        moongladeFetch('/api/post/recyclebin', 'DELETE', {}, function (resp) {
            success('Cleared');
            setTimeout(function () {
                window.location.reload();
            }, 800);
        });
    });
});

formatUtcTime();