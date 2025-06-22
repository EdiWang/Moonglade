import { moongladeFetch } from './httpService.mjs'
import { formatUtcTime } from './utils.module.mjs'
import { success } from './toastService.mjs'

function deleteConfirm(postid) {
    var cfm = confirm("Delete Confirmation?");
    if (cfm) {
        deletePost(postid);
    }
}

function deletePost(postid) {
    moongladeFetch(`/api/post/${postid}/recycle`,
        'DELETE',
        {},
        (resp) => {
            const postElement = document.getElementById(`post-${postid}`);
            if (postElement) {
                success('Post deleted');
                postElement.remove();
            }
        });
}

formatUtcTime();

document.addEventListener('DOMContentLoaded', () => {
    const exportButtons = document.querySelectorAll('.btn-delete');

    exportButtons.forEach(button => {
        button.addEventListener('click', () => {
            const postId = button.getAttribute('data-postId');
            deleteConfirm(postId);
        });
    });
});
