import { moongladeFetch } from './httpService.mjs'
import { formatUtcTime } from '/js/app/utils.module.mjs'

document.querySelectorAll('.btn-delete').forEach(button => {
    button.addEventListener('click', function () {
        var cid = this.getAttribute('data-commentid');
        moongladeFetch('/api/comment', 'DELETE', [cid], (success) => {
            document.querySelector(`#panel-comment-${cid}`).remove();
        });
    });
});

document.querySelectorAll('.btn-reply').forEach(button => {
    button.addEventListener('click', function () {
        var cid = this.getAttribute('data-commentid');
        document.querySelector(`#panel-comment-${cid} .reply-ui`).style.display = 'block';
    });
});

document.querySelectorAll('.btn-reply-post').forEach(button => {
    button.addEventListener('click', function () {
        var cid = this.getAttribute('data-commentid');
        var replyContent = document.querySelector(`#replycontent-${cid}`).value;

        moongladeFetch(`/api/comment/${cid}/reply`, 'POST', replyContent, async (resp) => {
            const data = await resp.json();
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
});

document.querySelectorAll('.btn-approve').forEach(button => {
    button.addEventListener('click', function () {
        var cid = this.getAttribute('data-commentid');
        moongladeFetch(`/api/comment/${cid}/approval/toggle`, 'PUT', {}, async (resp) => {
            var data = await resp.json();
            var approveButton = document.querySelector(`#panel-comment-${data} .btn-approve`);
            approveButton.classList.toggle('btn-outline-success');
            approveButton.classList.toggle('btn-success');
        });
    });
});

window.deleteSelectedComments = function () {
    var cids = [];
    document.querySelectorAll('.chk-cid:checked').forEach(function (checkbox) {
        cids.push(checkbox.getAttribute('data-cid'));
    });

    moongladeFetch('/api/comment', 'DELETE', cids, function (success) {
        cids.forEach(function (value) {
            var commentPanel = document.querySelector(`#panel-comment-${value}`);
            if (commentPanel) {
                commentPanel.remove();
            }
        });
    });
}

formatUtcTime();