export function resizeImages() {
    const images = document.querySelectorAll('.post-content img');
    images.forEach(img => {
        img.removeAttribute('height');
        img.removeAttribute('width');
        img.classList.add('img-fluid', 'img-thumbnail');
    });
}

export function renderCodeHighlighter() {
    const pres = document.querySelectorAll('pre');
    pres.forEach(pre => {
        // Find <pre> that doesn't have a <code> inside it.
        if (!pre.querySelector('code')) {
            const code = document.createElement('code');
            while (pre.firstChild) {
                code.appendChild(pre.firstChild);
            }
            pre.appendChild(code);
        }

        // For code that can't be automatically detected, fall back to use XML
        if (pre.classList.contains('language-markup')) {
            pre.querySelector('code').classList.add('lang-xml');
        }
    });

    const codeBlocks = document.querySelectorAll('pre code');
    codeBlocks.forEach(block => {
        hljs.highlightElement(block);
    });
}

export function RenderLaTeX() {
    const codeBlocks = document.querySelectorAll('pre.language-latex code');
    codeBlocks.forEach(block => {
        const latex = block.textContent.trim();
        const container = document.createElement('div');
        try {
            katex.render(latex, container, { output: 'mathml' });
            block.parentNode.replaceWith(container);
        } catch (error) {
            console.error(error);
        }
    });
}

export function getImageWidthInDevicePixelRatio(width) {
    if (width <= 0) return 0;
    var dpr = window.devicePixelRatio;
    if (dpr === 1) return width;
    return width / dpr;
}

export function applyImageZooming() {
    document.querySelectorAll('.post-content img').forEach(function (img) {
        img.addEventListener('click', function (e) {
            var src = img.getAttribute('src');

            document.querySelector('#imgzoom').src = src;

            const fitImageToDevicePixelRatio = document.querySelector('meta[name="image-device-dpi"]')?.content === "true";
            if (fitImageToDevicePixelRatio) {
                setTimeout(function () {
                    var w = document.querySelector('#imgzoom').naturalWidth;
                    document.querySelector('#imgzoom').style.width = getImageWidthInDevicePixelRatio(w) + 'px';
                }, 100);
            }

            var imgzoomModal = new bootstrap.Modal(document.querySelector('#imgzoomModal'));
            imgzoomModal.show();
        });
    });
}

export function resetCaptchaImage() {
    const d = new Date();
    document.querySelector('#img-captcha').src = `/captcha-image?${d.getTime()}`;
}

export function showCaptcha() {
    var captchaContainer = document.getElementById('captcha-container');
    if (captchaContainer.style.display === 'none') {
        captchaContainer.style.display = 'flex';
        resetCaptchaImage();
    }
}

export function submitComment(pid) {
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

export function calculateReadingTime() {
    const englishWordsPerMinute = 225; // Average reading speed for English
    const chineseCharactersPerMinute = 450; // Average reading speed for Chinese
    const germanWordsPerMinute = 225; // Average reading speed for German
    const japaneseCharactersPerMinute = 400; // Average reading speed for Japanese

    // Get the content of the blog post
    const blogContent = document.querySelector('.post-content').innerText;

    const englishAndGermanWords = blogContent.match(/\b\w+\b/g) || [];
    const chineseCharacters = blogContent.match(/[\u4e00-\u9fa5]/g) || [];
    const japaneseCharacters = blogContent.match(/[\u3040-\u30FF\u31F0-\u31FF\uFF66-\uFF9F\u4E00-\u9FAF]/g) || [];

    // Calculate reading time for English and German (combined), Chinese, and Japanese
    const englishAndGermanReadingTime = englishAndGermanWords.length / englishWordsPerMinute;
    const chineseReadingTime = chineseCharacters.length / chineseCharactersPerMinute;
    const japaneseReadingTime = japaneseCharacters.length / japaneseCharactersPerMinute;

    // Total reading time in minutes
    const totalReadingTime = englishAndGermanReadingTime + chineseReadingTime + japaneseReadingTime;

    // Round to nearest minute
    const roundedReadingTime = Math.ceil(totalReadingTime);

    document.getElementById('reading-time').innerText = `Estimated Reading Time: ${roundedReadingTime} minute(s)`;
}
