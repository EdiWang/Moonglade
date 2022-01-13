var btnSubmitComment = '#btn-submit-comment';

function submitComment() {
    $('#thx-for-comment').hide();
    $('#thx-for-comment-non-review').hide();

    $('#loadingIndicator').show();
    $(btnSubmitComment).addClass('disabled');
    $(btnSubmitComment).attr('disabled', 'disabled');

    callApi(`/api/comment/${pid}`, 'POST',
        {
            "username": $('#input-comment-name').val().trim(),
            "content": $('#input-comment-content').val(),
            "Email": $('#input-comment-email').val(),
            "captchaCode": $('#input-comment-captcha').val()
        },
        (success) => {
            document.querySelector('#comment-form').reset();
            postSlug.resetCaptchaImage();

            var httpCode = success.status;
            if (httpCode === 201) {
                $('#thx-for-comment').show();
            }
            if (httpCode === 200) {
                $('#thx-for-comment-non-review').show();
            }
        },
        (always) => {
            $('#loadingIndicator').hide();
            $(btnSubmitComment).removeClass('disabled');
            $(btnSubmitComment).removeAttr('disabled');
        });
}