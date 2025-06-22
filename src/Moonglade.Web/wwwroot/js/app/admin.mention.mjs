import { moongladeFetch } from './httpService.mjs'
import { formatUtcTime } from './utils.module.mjs';
import { success } from './toastService.mjs';

function deleteMention(mentionId) {
    moongladeFetch(`/api/mention/${mentionId}`, 'DELETE', {},
        (resp) => {
            document.querySelector(`#mention-box-${mentionId}`).remove();
        });
}

function clearMention() {
    moongladeFetch(`/api/mention/clear`, 'DELETE', {},
        (resp) => {
            success('Mention logs are cleared');
            setTimeout(function () {
                window.location.reload();
            }, 800);
        });
}

formatUtcTime();

document.getElementById("mentionFilter").addEventListener("keyup", function () {
    var value = this.value.toLowerCase();
    var mentionItems = document.querySelectorAll(".mention-item-entry");

    mentionItems.forEach(function (item) {
        var text = item.textContent.toLowerCase();
        item.style.display = text.indexOf(value) > -1 ? "" : "none";
    });
});

document.addEventListener('DOMContentLoaded', () => {
    const exportButtons = document.querySelectorAll('.btn-delete');

    exportButtons.forEach(button => {
        button.addEventListener('click', () => {
            const mentionId = button.getAttribute('data-mentionId');
            deleteMention(mentionId);
        });
    });
});

document.getElementById('btn-clear-all').addEventListener('click', () => {
    clearMention();
});
