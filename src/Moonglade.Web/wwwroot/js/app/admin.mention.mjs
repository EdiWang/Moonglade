import { moongladeFetch2 } from './httpService.mjs?v=1426'
import { formatUtcTime } from './utils.module.mjs';
import { success } from './toastService.mjs';

async function deleteMention(mentionId) {
    await moongladeFetch2(`/api/mention/${mentionId}`, 'DELETE', {});
    document.querySelector(`#mention-box-${mentionId}`).remove();
}

async function clearMention() {
    await moongladeFetch2(`/api/mention/clear`, 'DELETE', {});

    success('Mention logs are cleared');
    setTimeout(function () {
        window.location.reload();
    }, 800);
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
        button.addEventListener('click', async () => {
            const mentionId = button.getAttribute('data-mentionId');
            await deleteMention(mentionId);
        });
    });
});

document.getElementById('btn-clear-all').addEventListener('click', async () => {
    await clearMention();
});
