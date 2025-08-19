import { fetch2 } from './httpService.mjs?v=1426'
import { formatUtcTime } from './utils.module.mjs'
import { success } from './toastService.mjs'

async function deletePost(postid) {
    await fetch2(`/api/post/${postid}/recycle`, 'DELETE', {});

    success('Post deleted.');
    document.querySelector(`#post-${postid}`).style.display = 'none';
}

document.querySelectorAll(".btn-delete").forEach(function (button) {
    button.addEventListener("click", async function () {
        var cfm = confirm("Delete Confirmation?");
        if (cfm) {
            await deletePost(this.dataset.postid);
        }
    });
});

formatUtcTime();