import { fetch2 } from './httpService.mjs?v=1427'
import { success } from './toastService.mjs';

async function deletePage(pageid) {
    await fetch2(`/api/page/${pageid}`, 'DELETE', {});

    document.querySelector(`#card-${pageid}`).remove();
    success('Page deleted');
}

async function deleteConfirm(pageid) {
    var cfm = confirm("Delete Confirmation?");
    if (cfm) await deletePage(pageid);
}

document.addEventListener('DOMContentLoaded', () => {
    const exportButtons = document.querySelectorAll('.btn-delete');

    exportButtons.forEach(button => {
        button.addEventListener('click', async () => {
            const pageId = button.getAttribute('data-pageId');
            await deleteConfirm(pageId);
        });
    });
});
