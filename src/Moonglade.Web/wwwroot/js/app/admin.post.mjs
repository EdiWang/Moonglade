import { moongladeFetch2 } from './httpService.mjs?v=1426'
import { formatUtcTime } from './utils.module.mjs'
import { success } from './toastService.mjs'

async function deleteConfirm(postid) {
    var cfm = confirm("Delete Confirmation?");
    if (cfm) {
        await deletePost(postid);
    }
}

async function deletePost(postid) {
    await moongladeFetch2(`/api/post/${postid}/recycle`, 'DELETE', {});

    const postElement = document.getElementById(`post-${postid}`);
    if (postElement) {
        success('Post deleted');
        postElement.remove();
    }
}

formatUtcTime();

document.addEventListener('DOMContentLoaded', () => {
    const exportButtons = document.querySelectorAll('.btn-delete');

    exportButtons.forEach(button => {
        button.addEventListener('click', async () => {
            const postId = button.getAttribute('data-postId');
            await deleteConfirm(postId);
        });
    });
});
