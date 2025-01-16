import { success } from './toastService.mjs';

function deletePage(pageid) {
    callApi(`/api/page/${pageid}`,
        'DELETE',
        {},
        (resp) => {
            document.querySelector(`#card-${pageid}`).remove();
            success('Page deleted');
        });
}

function deleteConfirm(pageid) {
    var cfm = confirm("Delete Confirmation?");
    if (cfm) deletePage(pageid);
}

document.addEventListener('DOMContentLoaded', () => {
    const exportButtons = document.querySelectorAll('.btn-delete');

    exportButtons.forEach(button => {
        button.addEventListener('click', () => {
            const pageId = button.getAttribute('data-pageId');
            deleteConfirm(pageId);
        });
    });
});
