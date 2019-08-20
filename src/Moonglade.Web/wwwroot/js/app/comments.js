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

var onCommentSuccess = function (context) {
    postSlug.resetCaptchaImage();
    $('#thx-for-comment').show();
    $('#form-comment')[0].reset();
};

var onCommentFailed = function (context) {
    $('#thx-for-comment').hide();
    var errorCode = context.responseJSON.responseCode;
    if (window.toastr) {
        if (errorCode === 1) {
            window.toastr.error('Server Error');
        }
        if (errorCode === 2) {
            window.toastr.error('Incorrect Captcha Code');
            postSlug.resetCaptchaImage();
            $('#CommentPostModel_CaptchaCode').val('');
            $('#CommentPostModel_CaptchaCode').focus();
        }
        if (errorCode === 3) {
            window.toastr.error('Your email domain has been blocked due to spam comments.');
        }
        if (errorCode === 4) {
            window.toastr.error('Comment is disabled.');
        }
        if (errorCode === 5) {
            window.toastr.error('Invalid input.');
        }
    } else {
        alert(`Error Code: ${errorCode}`);
    }
};