import { fetch2 } from './httpService.mjs?v=1500'
import { formatUtcTime, parseMetaContent } from './utils.module.mjs';
import { resizeImages, applyImageZooming } from './post.imageutils.mjs';
import { renderCodeHighlighter, renderLaTeX } from './post.highlight.mjs';
import { renderMermaid } from './post.mermaid.mjs';
import { calculateReadingTime } from './post.readingtime.mjs';
import { recordPostView } from './postview.mjs';
import { error } from './toastService.mjs';

const commentEditorStylesheetId = 'comment-markdown-editor-stylesheet';
const commentEditorStylesheetPath = '/_content/Moonglade.Editor.StaticAssets/moonglade-editor/moonglade-editor.css';
const commentEditorModulePath = '/_content/Moonglade.Editor.StaticAssets/moonglade-editor/moonglade-editor.code.js';
let commentEditorModulePromise = null;
let commentMarkdownEditor = null;
let commentMarkdownEditorEnabled = false;

function ensureCommentEditorStylesheet() {
    if (document.getElementById(commentEditorStylesheetId)) {
        return;
    }

    const link = document.createElement('link');
    link.id = commentEditorStylesheetId;
    link.rel = 'stylesheet';
    link.href = commentEditorStylesheetPath;
    document.head.append(link);
}

async function ensureCommentEditorModule() {
    commentEditorModulePromise ??= import(commentEditorModulePath);
    return await commentEditorModulePromise;
}

async function enableCommentMarkdownEditor() {
    if (commentMarkdownEditorEnabled) {
        return;
    }

    const editorElement = document.querySelector('#comment-markdown-editor');
    const textarea = document.querySelector('#input-comment-content');
    if (!editorElement || !textarea) {
        return;
    }

    ensureCommentEditorStylesheet();
    const { createMoongladeCodeEditor } = await ensureCommentEditorModule();

    textarea.removeAttribute('required');
    textarea.classList.add('d-none');
    editorElement.classList.remove('d-none');
    commentMarkdownEditor = createMoongladeCodeEditor({
        element: editorElement,
        textarea,
        content: textarea.value || '',
        language: 'markdown',
        height: '180px',
        lineWrapping: true
    });
    commentMarkdownEditorEnabled = true;
    commentMarkdownEditor.focus();
}

function disableCommentMarkdownEditor() {
    const editorElement = document.querySelector('#comment-markdown-editor');
    const textarea = document.querySelector('#input-comment-content');

    if (commentMarkdownEditor) {
        commentMarkdownEditor.syncToTextarea();
        commentMarkdownEditor.destroy();
        commentMarkdownEditor = null;
    }

    editorElement?.replaceChildren();
    editorElement?.classList.add('d-none');

    if (textarea) {
        textarea.classList.remove('d-none');
        textarea.setAttribute('required', 'required');
    }

    commentMarkdownEditorEnabled = false;
}

function resetCommentMarkdownEditor() {
    const toggle = document.querySelector('#input-comment-use-markdown-editor');
    if (toggle) {
        toggle.checked = false;
    }

    disableCommentMarkdownEditor();
}

function syncCommentMarkdownEditor() {
    commentMarkdownEditor?.syncToTextarea();
}

function isCommentMarkdownEditorEnabled() {
    return commentMarkdownEditorEnabled && !!commentMarkdownEditor;
}

async function toggleCommentMarkdownEditor(event) {
    const checkbox = event.currentTarget;
    checkbox.disabled = true;

    try {
        if (checkbox.checked) {
            await enableCommentMarkdownEditor();
        }
        else {
            disableCommentMarkdownEditor();
        }
    }
    catch (err) {
        checkbox.checked = false;
        disableCommentMarkdownEditor();
        error(err);
    }
    finally {
        checkbox.disabled = false;
    }
}

async function submitComment(pid) {
    const thxForComment = document.querySelector('#thx-for-comment');
    const thxForCommentNonReview = document.querySelector('#thx-for-comment-non-review');
    const loadingIndicator = document.querySelector('#loadingIndicator');
    const btnSubmitComment = document.querySelector('#btn-submit-comment');
    const commentForm = document.querySelector('#comment-form');
    const commentContentInput = document.querySelector('#input-comment-content');

    syncCommentMarkdownEditor();

    const username = document.querySelector('#input-comment-name').value;
    const content = commentContentInput.value;
    const email = document.querySelector('#input-comment-email').value;
    const source = document.querySelector('#input-comment-source')?.value ?? '';
    const formRenderedUtc = Number(document.querySelector('#input-comment-form-rendered-utc')?.value ?? 0);

    if (isCommentMarkdownEditorEnabled() && !content.trim()) {
        commentMarkdownEditor.focus();
        error(commentContentInput.dataset.commentRequiredMessage || commentContentInput.placeholder);
        return;
    }

    thxForComment.style.display = 'none';
    thxForCommentNonReview.style.display = 'none';
    loadingIndicator.style.display = 'block';
    btnSubmitComment.classList.add('disabled');
    btnSubmitComment.setAttribute('disabled', 'disabled');

    try {
        const data = await fetch2(`/api/comment/${pid}`, 'POST', { username, content, email, source, formRenderedUtc });
        resetCommentMarkdownEditor();
        commentForm.reset();
        resetCommentFormRenderedAt(data.formRenderedUtc);

        if (data.requireCommentReview) {
            thxForComment.style.display = 'block';
        }
        else {
            thxForCommentNonReview.style.display = 'block';
        }
    } catch (err) {
        error(err);
    } finally {
        loadingIndicator.style.display = 'none';
        btnSubmitComment.classList.remove('disabled');
        btnSubmitComment.removeAttribute('disabled');
    }
}

function resetCommentFormRenderedAt(formRenderedUtc) {
    const input = document.querySelector('#input-comment-form-rendered-utc');
    if (input && Number.isFinite(formRenderedUtc)) {
        input.value = formRenderedUtc.toString();
    }
}

document.addEventListener('DOMContentLoaded', () => {
    resizeImages('.post-content img');
    if (window.innerWidth >= 768) {
        applyImageZooming('.post-content img');
    }

    renderMermaid().catch(err => console.error(err));
    renderCodeHighlighter();
    renderLaTeX('pre.language-latex code');

    if (parseMetaContent('post-is-published')) {

        let pid = document.querySelector('article').dataset.postid;

        const blogContent = document.querySelector('.post-content').innerText;
        let roundedReadingTime = calculateReadingTime(blogContent);

        const elem = document.getElementById('reading-time');
        if (elem) {
            elem.innerText = `Estimated Reading Time: ${roundedReadingTime} minute(s)`;
        }

        document.getElementById('comment-form')?.addEventListener('submit', function (e) {
            e.preventDefault();
            submitComment(pid);
        });
        document.getElementById('input-comment-use-markdown-editor')?.addEventListener('change', toggleCommentMarkdownEditor);
        document.getElementById('btn-submit-comment')?.addEventListener('click', syncCommentMarkdownEditor);

        formatUtcTime();

        recordPostView(pid);
    }
});
