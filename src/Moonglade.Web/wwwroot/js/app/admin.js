$.validator.setDefaults({
    ignore: []
});

function slugify(text) {
    return text
        .toLowerCase()
        .replace(/[^\w ]+/g, '')
        .replace(/ +/g, '-');
}

var postEditor = {
    loadRichEditor: function (textareaSelector) {
        if (window.tinyMCE !== undefined) {
            window.tinyMCE.init({
                selector: textareaSelector,
                themes: "silver",
                skin: "oxide",
                height: 500,
                relative_urls: false, // avoid image upload fuck up
                browser_spellcheck: true,
                branding: false,
                fontsize_formats: "8pt 10pt 12pt 14pt 18pt 24pt 36pt",
                plugins: "advlist autolink hr autosave link image lists charmap print preview hr anchor pagebreak spellchecker searchreplace wordcount visualblocks visualchars code fullscreen insertdatetime media nonbreaking save table directionality template paste codesample imagetools",
                toolbar: "undo redo | formatselect | fontsizeselect | bold italic strikethrough forecolor backcolor | removeformat | link image codesample media | alignleft aligncenter alignright alignjustify | bullist numlist outdent indent | code | fullscreen",
                paste_data_images: true,
                images_upload_url: '/image/upload',
                images_upload_credentials: true,
                content_css: "/css/tinymce-editor-bs-bundle.min.css"
            });
        }
    },
    initEvents: function () {
        $("#Title").change(function () {
            $("#Slug").val(slugify($(this).val()));
        });

        var tagnames = new Bloodhound({
            datumTokenizer: Bloodhound.tokenizers.obj.whitespace('name'),
            queryTokenizer: Bloodhound.tokenizers.whitespace,
            prefetch: {
                url: '/tags/get-all-tag-names',
                filter: function (list) {
                    return $.map(list, function (tagname) {
                        return { name: tagname };
                    });
                }
            }
        });

        tagnames.initialize();
        $('#Tags').tagsinput({
            typeaheadjs: {
                name: 'tagnames',
                displayKey: 'name',
                valueKey: 'name',
                source: tagnames.ttAdapter()
            }
        });

        $("#btn-save").click(function (e) {
            submitForm(e);
        });

        $("#btn-publish").click(function (e) {
            if ($('form').valid()) {
                $('input[name="IsPublished"]').val("True");
                submitForm(e);
            }
        });

        function submitForm(e) {
            window.tinyMCE.triggerSave();

            var selectCatCount = 0;
            $('input[name="SelectedCategoryIds"]').each(function () {
                if ($(this).prop('checked') === true) {
                    ++selectCatCount;
                }
            });

            if ($('.post-edit-form').valid() && selectCatCount === 0) {
                e.preventDefault();
                window.toastr.error('Please select at least one category');
            }
            else {
                if ($('input[name="IsPublished"]').val() == 'True') {
                    $('#btn-publish').hide();
                }
            }
        }

        $(".post-edit-form").areYouSure({
            message: "You have unsaved changes, are you sure to leave this page?"
        });

        $("#Title").focus();
    },
    keepAlive: function () {
        var tid = setInterval(postNonce, 60 * 1000);
        function postNonce() {
            var num = Math.random();
            $.post("/admin/keep-alive", { nonce: num }, function (data) {
                console.info(data);
            });
        }
        function abortTimer() {
            clearInterval(tid);
        }
    }
};

var sendTestEmail = function () {
    $("#a-send-test-mail").text("Sending...");
    $("#a-send-test-mail").addClass("disabled");
    $("#a-send-test-mail").attr("disabled", "disabled");

    $.post("/admin/settings/send-test-email",
        function (data) {
            if (data.isSuccess) {
                window.toastr.success("Email is sent.");
            } else {
                window.toastr.error(data.message);
            }
        })
        .fail(function (xhr, status, error) {
            var responseJson = $.parseJSON(xhr.responseText);
            window.toastr.error(responseJson.message);
        })
        .always(function () {
            $("#a-send-test-mail").text("Send Test Email");
            $("#a-send-test-mail").removeClass("disabled");
            $("#a-send-test-mail").removeAttr("disabled");
        });
};

var btnSaveSettings = "#btn-save-settings";
var onUpdateSettingsBegin = function () {
    $(btnSaveSettings).text("Processing...");
    $(btnSaveSettings).addClass("disabled");
    $(btnSaveSettings).attr("disabled", "disabled");
};

var onUpdateSettingsComplete = function () {
    $(btnSaveSettings).text("Save");
    $(btnSaveSettings).removeClass("disabled");
    $(btnSaveSettings).removeAttr("disabled");
};

var onUpdateSettingsSuccess = function (context) {
    if (window.toastr) {
        window.toastr.success("Settings Updated");
    } else {
        alert("Settings Updated");
    }
};

var onUpdateSettingsFailed = function (context) {
    var msg = context.responseJSON.message;
    if (window.toastr) {
        window.toastr.error("Server Error: " + msg);
    } else {
        alert("Error Code: " + msg);
    }
};

var btnSubmitPost = "#btn-save";
var onPostCreateEditBegin = function () {
    $(btnSubmitPost).text("Saving...");
    $(btnSubmitPost).addClass("disabled");
    $(btnSubmitPost).attr("disabled", "disabled");
};

var onPostCreateEditComplete = function () {
    $(btnSubmitPost).text("Save");
    $(btnSubmitPost).removeClass("disabled");
    $(btnSubmitPost).removeAttr("disabled");
};

var onPostCreateEditSuccess = function (data) {
    if (data.redirectToManage) {
        window.location.href = "/post/manage";
    }
    else {
        toastr.success("Post saved successfully.");
    }
};

var onPostCreateEditFailed = function (context) {
    var message = context.responseJSON.message;
    if (window.toastr) {
        window.toastr.error(message);
    } else {
        alert("Error: " + message);
    }
};