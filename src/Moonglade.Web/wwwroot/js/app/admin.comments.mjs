import { fetch2 } from './httpService.mjs?v=1500'
import { formatUtcTime } from '/js/app/utils.module.mjs'

document.querySelectorAll('.btn-delete').forEach(button => {
    button.addEventListener('click', async function () {
        var cid = this.getAttribute('data-commentid');
        await fetch2('/api/comment', 'DELETE', [cid]);

        document.querySelector(`#panel-comment-${cid}`).remove();
    });
});

document.querySelectorAll('.btn-reply').forEach(button => {
    button.addEventListener('click', function () {
        var cid = this.getAttribute('data-commentid');
        document.querySelector(`#panel-comment-${cid} .reply-ui`).style.display = 'block';
    });
});

document.querySelectorAll('.btn-reply-post').forEach(button => {
    button.addEventListener('click', async function () {
        var cid = this.getAttribute('data-commentid');
        var replyContent = document.querySelector(`#replycontent-${cid}`).value;

        const data = await fetch2(`/api/comment/${cid}/reply`, 'POST', replyContent);

        const replyUi = document.querySelector(`#panel-comment-${data.commentId} .reply-ui`);
        const replyList = document.querySelector(`#panel-comment-${data.commentId} .reply-list`);

        if (replyUi) replyUi.style.display = 'none';
        if (replyList) {
            replyList.insertAdjacentHTML('afterbegin', `
                <hr />
                <div class="reply-container">
                    <div>
                        <span class="pull-right text-muted">${data.replyTimeUtc}</span>
                    </div>
                    <p>${data.replyContentHtml}</p>
                </div>
            `);
        }
    });
});

document.querySelectorAll('.btn-approve').forEach(button => {
    button.addEventListener('click', async function () {
        var cid = this.getAttribute('data-commentid');
        const data = await fetch2(`/api/comment/${cid}/approval/toggle`, 'PUT', {});

        var approveButton = document.querySelector(`#panel-comment-${data} .btn-approve`);
        approveButton.classList.toggle('btn-outline-success');
        approveButton.classList.toggle('btn-success');
    });
});

window.deleteSelectedComments = async function () {
    var cids = [];
    document.querySelectorAll('.chk-cid:checked').forEach(function (checkbox) {
        cids.push(checkbox.getAttribute('data-cid'));
    });

    await fetch2('/api/comment', 'DELETE', cids);

    cids.forEach(function (value) {
        var commentPanel = document.querySelector(`#panel-comment-${value}`);
        if (commentPanel) {
            commentPanel.remove();
        }
    });
}

formatUtcTime();