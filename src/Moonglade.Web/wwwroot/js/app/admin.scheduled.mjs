import { moongladeFetch } from './httpService.mjs'
import { formatUtcTime } from './utils.module.mjs'
import { success } from './toastService.mjs'

function deletePost(postid) {
    moongladeFetch(`/api/post/${postid}/recycle`, 'DELETE', {}, function (resp) {
        success('Post deleted.');
        document.querySelector(`#post-${postid}`).style.display = 'none';
    });
}

function publishPost(postId) {
    moongladeFetch(
        `/api/post/${postId}/publish`,
        'PUT',
        {},
        (resp) => {
            success('Post published');
            location.reload();
        });
}

function postponePost(postId, hours) {
    moongladeFetch(
        `/api/post/${postId}/postpone?hours=${hours}`,
        'PUT',
        {},
        (resp) => {
            success(`Post postponed for ${hours} hour(s)`);
            setTimeout(() => {
                location.reload();
            }, 3000);
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

document.querySelectorAll(".btn-publish").forEach(function (button) {
    button.addEventListener("click", function () {
        const postId = this.dataset.postid;
        document.getElementById('btn-publish-post').dataset.postid = postId;
    });
});

document.getElementById('btn-publish-post').addEventListener('click', function () {
    const postId = this.dataset.postid;
    publishPost(postId);
});

document.querySelectorAll(".btn-postpone").forEach(function (button) {
    button.addEventListener("click", function () {
        const postId = this.dataset.postid;
        const hours = 24; // parseInt(this.dataset.hours);
        postponePost(postId, hours);
    });
});

formatUtcTime();