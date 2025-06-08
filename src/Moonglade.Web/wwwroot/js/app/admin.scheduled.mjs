import { moongladeFetch } from './httpService.mjs'
import { formatUtcTime } from './utils.module.mjs'
import { success } from './toastService.mjs'

function deletePost(postid) {
    moongladeFetch(`/api/post/${postid}/recycle`, 'DELETE', {}, function (resp) {
        success('Post deleted.');
        document.querySelector(`#post-${postid}`).style.display = 'none';
    });
}

document.querySelectorAll(".btn-delete").forEach(function (button) {
    button.addEventListener("click", function () {
        var cfm = confirm("Delete Confirmation?");
        if (cfm) {
            deletePost(this.dataset.postid);
        }
    });
});

formatUtcTime();