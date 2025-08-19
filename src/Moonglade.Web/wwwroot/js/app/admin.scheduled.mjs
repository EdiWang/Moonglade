import { fetch2 } from './httpService.mjs?v=1426'
import { formatUtcTime } from './utils.module.mjs'
import { success } from './toastService.mjs'

async function deletePost(postid) {
    await fetch2(`/api/post/${postid}/recycle`, 'DELETE', {});

    success('Post deleted.');
    document.querySelector(`#post-${postid}`).style.display = 'none';
}

async function publishPost(postId) {
    await fetch2(`/api/post/${postId}/publish`, 'PUT', {});

    success('Post published');
    location.reload();
}

async function postponePost(postId, hours) {
    await fetch2(`/api/post/${postId}/postpone?hours=${hours}`, 'PUT', {});

    success(`Post postponed for ${hours} hour(s)`);
    setTimeout(() => {
        location.reload();
    }, 3000);
}

document.querySelectorAll(".btn-delete").forEach(function (button) {
    button.addEventListener("click", async function () {
        var cfm = confirm("Delete Confirmation?");
        if (cfm) {
            await deletePost(this.dataset.postid);
        }
    });
});

document.querySelectorAll(".btn-publish").forEach(function (button) {
    button.addEventListener("click", function () {
        const postId = this.dataset.postid;
        document.getElementById('btn-publish-post').dataset.postid = postId;
    });
});

document.getElementById('btn-publish-post').addEventListener('click', async function () {
    const postId = this.dataset.postid;
    await publishPost(postId);
});

document.querySelectorAll(".btn-postpone").forEach(function (button) {
    button.addEventListener("click", async function () {
        const postId = this.dataset.postid;
        const hours = 24; // parseInt(this.dataset.hours);
        await postponePost(postId, hours);
    });
});

formatUtcTime();