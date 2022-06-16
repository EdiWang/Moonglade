import * as blogToast from '/js/app/blogtoast.module.js'
window.blogToast = blogToast;

window.emptyGuid = '00000000-0000-0000-0000-000000000000';

var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
    return new bootstrap.Tooltip(tooltipTriggerEl);
});