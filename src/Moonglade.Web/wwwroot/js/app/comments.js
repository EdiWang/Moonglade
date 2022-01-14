var btnSubmitComment = '#btn-submit-comment';

function submitComment() {
    document.querySelector('#thx-for-comment').style.display = 'none';
    document.querySelector('#thx-for-comment-non-review').style.display = 'none';

    document.querySelector('#loadingIndicator').style.display = 'block';
    document.querySelector(btnSubmitComment).classList.add('disabled');
    document.querySelector(btnSubmitComment).setAttribute('disabled', 'disabled');

    callApi(`/api/comment/${pid}`, 'POST',
        {
            username: document.querySelector('#input-comment-name').value,
            content: document.querySelector('#input-comment-content').value,
            email: document.querySelector('#input-comment-email').value,
            captchaCode: document.querySelector('#input-comment-captcha').value
        },
        (success) => {
            document.querySelector('#comment-form').reset();
            postSlug.resetCaptchaImage();

            var httpCode = success.status;
            if (httpCode === 201) {
                document.querySelector('#thx-for-comment').style.display = 'block';
            }
            if (httpCode === 200) {
                document.querySelector('#thx-for-comment-non-review').style.display = 'block';
            }
        },
        (always) => {
            document.querySelector('#loadingIndicator').style.display = 'none';
            document.querySelector(btnSubmitComment).classList.remove('disabled');
            document.querySelector(btnSubmitComment).removeAttribute('disabled');
        });
}