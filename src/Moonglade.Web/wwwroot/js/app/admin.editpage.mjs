import { fetch2 } from './httpService.mjs?v=1500'
import { parseMetaContent } from './utils.module.mjs'
import { success } from './toastService.mjs'

let isPreviewRequired = false;
let pageId = parseMetaContent('page-id');
const btnSubmitPage = '#btn-submit';

let htmlContentEditor = null;
let cssContentEditor = null;
let hasCssEditorInitialized = false;

require(['vs/editor/editor.main'], function () {
    htmlContentEditor = initEditor('RawHtmlContentEditor', "#EditPageRequest_RawHtmlContent", 'html');
});

function assignEditorValues2() {
    assignEditorValues(htmlContentEditor, "#EditPageRequest_RawHtmlContent")

    // Edit Mode, preserve old value when user has not clicked on CSS tab
    let oldCssValue = document.querySelector("#EditPageRequest_CssContent").value;
    let cssValue = hasCssEditorInitialized ? cssContentEditor.getValue() : oldCssValue;

    document.querySelector("#EditPageRequest_CssContent").value = cssValue;
}

function onPageCreateEditBegin() {
    document.querySelector(btnSubmitPage).classList.add('disabled');
    document.querySelector(btnSubmitPage).disabled = true;
};

function onPageCreateEditComplete() {
    document.querySelector(btnSubmitPage).classList.remove('disabled');
    document.querySelector(btnSubmitPage).removeAttribute('disabled');
};

async function postCreateOrEdit() {
    onPageCreateEditBegin();

    const apiAddress = pageId == window.emptyGuid ? `/api/page` : `/api/page/${pageId}`;
    const verb = pageId == window.emptyGuid ? 'POST' : 'PUT';

    const data = await fetch2(apiAddress, verb,
        {
            title: document.querySelector("#EditPageRequest_Title").value,
            slug: document.querySelector("#EditPageRequest_Slug").value,
            metaDescription: document.querySelector("#EditPageRequest_MetaDescription").value,
            rawHtmlContent: document.querySelector("#EditPageRequest_RawHtmlContent").value,
            cssContent: document.querySelector("#EditPageRequest_CssContent").value,
            hideSidebar: document.querySelector('#EditPageRequest_HideSidebar').checked,
            isPublished: document.querySelector('#EditPageRequest_IsPublished').checked
        });

    onPageCreateEditComplete();

    if (data.pageId) {
        pageId = data.pageId;
        success('Page saved successfully.');

        if (document.querySelector('#EditPageRequest_IsPublished').checked) {
            if (document.querySelector('#btn-preview')) {
                document.querySelector('#btn-preview').style.display = 'none';
            }
        }

        if (isPreviewRequired) {
            isPreviewRequired = false;
            window.open(`/admin/page/preview/${data.pageId}`);
        }
    }
}

document.querySelectorAll('a[data-bs-toggle="tab"]').forEach(function (element) {
    element.addEventListener('shown.bs.tab', function (e) {
        var isCssTab = e.target.id === "csscontent-tab";
        if (isCssTab && !hasCssEditorInitialized) {
            cssContentEditor = initEditor('CssContentEditor', "#EditPageRequest_CssContent", 'css');
            hasCssEditorInitialized = true;
        }
    });
});

const handleKeyboardShortcuts = (event) => {
    if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 's') {
        event.preventDefault();
        document.querySelector(btnSubmitPostSelector).click();
    }
};

window.addEventListener('keydown', handleKeyboardShortcuts);

async function handleSubmit(e) {
    e.preventDefault();

    isPreviewRequired = e.submitter.id == 'btn-preview';
    assignEditorValues2();

    await postCreateOrEdit();
}

const form = document.querySelector('#page-edit-form');
form.addEventListener('submit', handleSubmit);