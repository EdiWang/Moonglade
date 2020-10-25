var btnSubmitComment = '#btn-submit-comment';
var onCommentBegin = function () {
    $('#loadingIndicator').show();
    $(btnSubmitComment).text('Processing...');
    $(btnSubmitComment).addClass('disabled');
    $(btnSubmitComment).attr('disabled', 'disabled');
};

var onCommentComplete = function () {
    $('#loadingIndicator').hide();
    $(btnSubmitComment).text('Submit');
    $(btnSubmitComment).removeClass('disabled');
    $(btnSubmitComment).removeAttr('disabled');
};

var onCommentSuccess = function (data, s, x) {
    postSlug.resetCaptchaImage();
    $('#form-comment')[0].reset();

    var httpCode = x.status;
    if (httpCode === 201) {
        $('#thx-for-comment').show();
    }
    if (httpCode === 200) {
        $('#thx-for-comment-non-review').show();
    }
};

var onCommentFailed = function (context) {
    $('#thx-for-comment').hide();

    var httpCode = context.status;
    if (window.toastr) {
        if (httpCode === 400) {
            window.toastr.error('Invalid input.');
        }
        if (httpCode === 403) {
            window.toastr.error('Comment is disabled.');
        }
        if (httpCode === 409) {
            window.toastr.error('Incorrect Captcha Code');
            postSlug.resetCaptchaImage();
            $('#CommentPostModel_CaptchaCode').val('');
            $('#CommentPostModel_CaptchaCode').focus();
        }
        if (httpCode === 500 || httpCode === 503) {
            window.toastr.error('Server went boom');
        }
    } else {
        alert(`Error ${httpCode}: ${errorCode}`);
    }
};