import { callApi } from './httpService.mjs'
import { formatUtcTime, parseMetaContent } from './utils.module.mjs';
import { resetCaptchaImage, showCaptcha } from './captchaService.mjs';
import { resizeImages, applyImageZooming } from './post.imageutils.mjs';
import { renderCodeHighlighter, renderLaTeX } from './post.highlight.mjs';
import { calculateReadingTime } from './post.readingtime.mjs';
import { cleanupLocalStorage, recordPostView } from './postview.mjs';

function submitComment(pid) {
    const thxForComment = document.querySelector('#thx-for-comment');
    const thxForCommentNonReview = document.querySelector('#thx-for-comment-non-review');
    const loadingIndicator = document.querySelector('#loadingIndicator');
    const btnSubmitComment = document.querySelector('#btn-submit-comment');
    const commentForm = document.querySelector('#comment-form');

    const username = document.querySelector('#input-comment-name').value;
    const content = document.querySelector('#input-comment-content').value;
    const email = document.querySelector('#input-comment-email').value;
    const captchaCode = document.querySelector('#input-comment-captcha').value;

    thxForComment.style.display = 'none';
    thxForCommentNonReview.style.display = 'none';
    loadingIndicator.style.display = 'block';
    btnSubmitComment.classList.add('disabled');
    btnSubmitComment.setAttribute('disabled', 'disabled');

    callApi(
        `/api/comment/${pid}`,
        'POST',
        { username, content, email, captchaCode },
        (success) => {
            commentForm.reset();
            resetCaptchaImage();

            const { status: httpCode } = success;
            if (httpCode === 201) {
                thxForComment.style.display = 'block';
            } else if (httpCode === 200) {
                thxForCommentNonReview.style.display = 'block';
            }
        },
        (always) => {
            loadingIndicator.style.display = 'none';
            btnSubmitComment.classList.remove('disabled');
            btnSubmitComment.removeAttribute('disabled');
        },
        //(error) => {
        //    console.error('Error submitting comment:', error);
        //    // Optionally handle specific error cases
        //}
    );
}

document.addEventListener('DOMContentLoaded', () => {
    resizeImages('.post-content img');
    if (window.innerWidth >= 768) {
        applyImageZooming('.post-content img');
    }

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

        document.getElementById('input-comment-content')?.addEventListener('focus', function () {
            showCaptcha();
        });

        document.getElementById('img-captcha')?.addEventListener('click', function () {
            resetCaptchaImage();
        });

        formatUtcTime();

        cleanupLocalStorage();
        recordPostView(pid);
    }
});
