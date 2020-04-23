$.validator.setDefaults({
    ignore: []
});

var emptyGuid = '00000000-0000-0000-0000-000000000000';

function slugify(text) {
    return text
        .toLowerCase()
        .replace(/[^\w ]+/g, '')
        .replace(/ +/g, '-');
}

function ImageUploader(targetName, hw, imgMimeType) {
    var imgDataUrl = '';

    this.uploadImage = function (uploadUrl) {
        if (imgDataUrl) {
            $(`#btn-upload-${targetName}`).addClass('disabled');
            $(`#btn-upload-${targetName}`).attr('disabled', 'disabled');

            var rawData = { base64Img: imgDataUrl.replace(/^data:image\/(png|jpeg|jpg);base64,/, '') };
            $.ajax({
                type: 'POST',
                headers: { csrfFieldName: $(`input[name=${csrfFieldName}]`).val() },
                url: uploadUrl,
                data: makeCSRFExtendedData(rawData),
                success: function (data) {
                    console.info(data);
                    $(`#${targetName}modal`).modal('hide');
                    toastr.success('Updated');
                    d = new Date();
                    $(`.blogadmin-${targetName}`).attr('src', `/${targetName}?${d.getTime()}`);
                },
                error: function (xhr, status, err) {
                    console.error(err);
                    toastr.error('Upload failed.');
                    $(`#btn-upload-${targetName}`).removeClass('disabled');
                    $(`#btn-upload-${targetName}`).removeAttr('disabled');
                }
            });
        } else {
            toastr.error('Please select an image');
        }
    }

    this.fileSelect = function (evt) {
        evt.stopPropagation();
        evt.preventDefault();

        if (window.File && window.FileReader && window.FileList && window.Blob) {
            var file;
            if (evt.dataTransfer) {
                file = evt.dataTransfer.files[0];
                $(`.custom-file-label-${targetName}`).text(file.name);
            } else {
                file = evt.target.files[0];
            }

            if (!file.type.match('image.*')) {
                toastr.error('Please select an image file.');
                return;
            }

            var reader = new FileReader();
            reader.onloadend = function () {
                var tempImg = new Image();
                tempImg.src = reader.result;
                tempImg.onload = function () {
                    var maxWidth = hw;
                    var maxHeight = hw;
                    var tempW = tempImg.width;
                    var tempH = tempImg.height;
                    if (tempW > tempH) {
                        if (tempW > maxWidth) {
                            tempH *= maxWidth / tempW;
                            tempW = maxWidth;
                        }
                    } else {
                        if (tempH > maxHeight) {
                            tempW *= maxHeight / tempH;
                            tempH = maxHeight;
                        }
                    }

                    var canvas = document.createElement('canvas');
                    canvas.width = tempW;
                    canvas.height = tempH;
                    var ctx = canvas.getContext('2d');
                    ctx.drawImage(this, 0, 0, tempW, tempH);
                    imgDataUrl = canvas.toDataURL(imgMimeType);

                    var div = $(`#${targetName}DropTarget`);
                    div.html(`<img class="img-fluid" src="${imgDataUrl}" />`);
                    $(`#btn-upload-${targetName}`).removeClass('disabled');
                    $(`#btn-upload-${targetName}`).removeAttr('disabled');
                }
            }
            reader.readAsDataURL(file);
        } else {
            toastr.error('The File APIs are not fully supported in this browser.');
        }
    }

    this.dragOver = function (evt) {
        evt.stopPropagation();
        evt.preventDefault();
        evt.dataTransfer.dropEffect = 'copy';
    }

    this.bindEvents = function () {
        document.getElementById(`${targetName}ImageFile`).addEventListener('change', this.fileSelect, false);
        var dropTarget = document.getElementById(`${targetName}DropTarget`);
        dropTarget.addEventListener('dragover', this.dragOver, false);
        dropTarget.addEventListener('drop', this.fileSelect, false);
    }

    this.getDataUrl = function () {
        return imgDataUrl;
    };
};

var isPreviewRequired = false;

var postEditor = {
    loadRichEditor: function (textareaSelector) {
        if (window.tinyMCE !== undefined) {
            window.tinyMCE.init({
                selector: textareaSelector,
                themes: 'silver',
                skin: 'oxide',
                //height: 650,
                relative_urls: false, // avoid image upload fuck up
                browser_spellcheck: true,
                branding: false,
                fontsize_formats: '8pt 10pt 12pt 14pt 18pt 24pt 36pt',
                plugins: 'advlist autolink hr autosave link image lists charmap print preview hr anchor pagebreak spellchecker searchreplace wordcount visualblocks visualchars code fullscreen insertdatetime media nonbreaking save table directionality template paste codesample imagetools',
                toolbar: 'undo redo | formatselect | fontsizeselect | bold italic strikethrough forecolor backcolor | removeformat | link image codesample media | alignleft aligncenter alignright alignjustify | bullist numlist outdent indent | code | fullscreen',
                paste_data_images: true,
                images_upload_url: '/upload-image',
                images_upload_credentials: true,
                content_css: '/css/tinymce-editor-bs-bundle.min.css',
                codesample_languages: [
                    { text: 'C#', value: 'csharp' },
                    { text: 'Bash', value: 'bash' },
                    { text: 'HTML/XML', value: 'markup' },
                    { text: 'JavaScript', value: 'javascript' },
                    { text: 'CSS', value: 'css' },
                    { text: 'PHP', value: 'php' },
                    { text: 'Ruby', value: 'ruby' },
                    { text: 'Python', value: 'python' },
                    { text: 'Json', value: 'json' },
                    { text: 'C', value: 'c' },
                    { text: 'C++', value: 'cpp' },
                    { text: 'Visual Basic', value: 'vb' },
                    { text: 'PowerShell', value: 'powershell' }
                ],
                setup: function (editor) {
                    editor.on('NodeChange', function (e) {
                        if (e.element.tagName === "IMG") {
                            e.element.setAttribute("loading", "lazy");
                        }
                    });
                }
            });
        }
    },
    loadMdEditor: function (textareaSelector) {
        if (window.SimpleMDE) {
            var simplemde = new SimpleMDE({
                element: $(textareaSelector)[0],
                spellChecker: false,
                status: false
            });

            inlineAttachment.editors.codemirror4.attach(simplemde.codemirror, {
                uploadUrl: '/upload-image',
                urlText: '![file](/uploads/{filename})',
                onFileUploadResponse: function (xhr) {
                    var result = JSON.parse(xhr.responseText),
                        filename = result[this.settings.jsonFieldName];

                    if (result && filename) {
                        var newValue;
                        if (typeof this.settings.urlText === 'function') {
                            newValue = this.settings.urlText.call(this, filename, result);
                        } else {
                            newValue = this.settings.urlText.replace(this.filenameTag, filename);
                        }
                        var text = this.editor.getValue().replace(this.lastValue, newValue);
                        this.editor.setValue(text);
                        this.settings.onFileUploaded.call(this, filename);
                    }
                    return false;
                }
            });
        }
    },
    initEvents: function () {
        $('#Title').change(function () {
            $('#Slug').val(slugify($(this).val()));
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
            },
            trimValue: true
        });

        $('#Tags').on('beforeItemAdd', function (event) {
            if (!/^[a-zA-Z 0-9\.\-\+\#\s]*$/i.test(event.item)) {
                console.warn(`Invalid tag name: ${event.item}`);
                toastr.warning(`Invalid tag name: ${event.item}`);
                event.cancel = true;
            }
        });

        $('#btn-preview').click(function (e) {
            if ($('form').valid()) {
                submitForm(e);
                isPreviewRequired = true;
            }
        });

        $('#btn-save').click(function (e) {
            submitForm(e);
        });

        $('#btn-publish').click(function (e) {
            if ($('form').valid()) {
                $('input[name="IsPublished"]').val('True');
                submitForm(e);
            }
        });

        function submitForm(e) {
            if (window.tinyMCE) {
                window.tinyMCE.triggerSave();
            }

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
                if ($('input[name="IsPublished"]').val() === 'True') {
                    $('#btn-publish').hide();
                    $('#btn-preview').hide();
                }
            }
        }

        $('.post-edit-form').areYouSure({
            message: 'You have unsaved changes, are you sure to leave this page?'
        });

        $('#Title').focus();
    },
    keepAlive: function () {
        var tid = setInterval(postNonce, 60 * 1000);
        function postNonce() {
            var num = Math.random();
            $.post('/admin/keep-alive', { nonce: num }, function (data) {
                console.info(data);
            });
        }
        function abortTimer() {
            clearInterval(tid);
        }
    }
};

var sendTestEmail = function () {
    $('#a-send-test-mail').text('Sending...');
    $('#a-send-test-mail').addClass('disabled');
    $('#a-send-test-mail').attr('disabled', 'disabled');

    $.post('/admin/settings/send-test-email',
        function (data) {
            if (data.isSuccess) {
                window.toastr.success('Email is sent.');
            } else {
                window.toastr.error(data.message);
            }
        })
        .fail(function (xhr, status, error) {
            var responseJson = $.parseJSON(xhr.responseText);
            window.toastr.error(responseJson.message);
        })
        .always(function () {
            $('#a-send-test-mail').text('Send Test Email');
            $('#a-send-test-mail').removeClass('disabled');
            $('#a-send-test-mail').removeAttr('disabled');
        });
};

var btnSaveSettings = '#btn-save-settings';
var onUpdateSettingsBegin = function () {
    $(btnSaveSettings).text('Processing...');
    $(btnSaveSettings).addClass('disabled');
    $(btnSaveSettings).attr('disabled', 'disabled');
};

var onUpdateSettingsComplete = function () {
    $(btnSaveSettings).text('Save');
    $(btnSaveSettings).removeClass('disabled');
    $(btnSaveSettings).removeAttr('disabled');
};

var onUpdateSettingsSuccess = function (context) {
    if (window.toastr) {
        window.toastr.success('Settings Updated');
    } else {
        alert('Settings Updated');
    }
};

var onUpdateSettingsFailed = function (context) {
    var msg = context.responseJSON.message;
    if (window.toastr) {
        window.toastr.error(`Server Error: ${msg}`);
    } else {
        alert(`Error Code: ${msg}`);
    }
};

var btnSubmitPost = '#btn-save';
var onPostCreateEditBegin = function () {
    $(btnSubmitPost).text('Saving...');
    $(btnSubmitPost).addClass('disabled');
    $(btnSubmitPost).attr('disabled', 'disabled');
};

var onPostCreateEditComplete = function () {
    $(btnSubmitPost).text('Save');
    $(btnSubmitPost).removeClass('disabled');
    $(btnSubmitPost).removeAttr('disabled');
};

var onPostCreateEditSuccess = function (data) {
    if (data.postId) {
        $('input[name="PostId"]').val(data.postId);
        toastr.success('Post saved successfully.');

        if (isPreviewRequired) {
            isPreviewRequired = false;
            window.open(`/post/preview/${data.postId}`);
        }
    }
};

var onPostCreateEditFailed = function (context) {
    var message = context.responseJSON.message;
    if (window.toastr) {
        window.toastr.error(message);
    } else {
        alert(`Error: ${message}`);
    }
};

var btnSubmitPage = '#btn-submit';
var onPageCreateEditBegin = function () {
    $(btnSubmitPage).text('Saving...');
    $(btnSubmitPage).addClass('disabled');
    $(btnSubmitPage).attr('disabled', 'disabled');
};

var onPageCreateEditComplete = function () {
    $(btnSubmitPage).text('Save');
    $(btnSubmitPage).removeClass('disabled');
    $(btnSubmitPage).removeAttr('disabled');
};

var onPageCreateEditSuccess = function (data) {
    if (data.pageId) {
        $('input[name="Id"]').val(data.pageId);
        toastr.success('Page saved successfully.');
    }
};

var onPageCreateEditFailed = function (context) {
    var message = context.responseJSON.message;
    if (window.toastr) {
        window.toastr.error(message);
    } else {
        alert(`Error: ${message}`);
    }
};

function tryRestartWebsite() {
    var nonce = Math.floor((Math.random() * 128) + 1);
    ajaxPostWithCSRFToken('shutdown', { nonce }, function () { });
    $('.btn-restart').text('Wait...');
    $('.btn-restart').addClass('disabled');
    $('.btn-restart').attr('disabled', 'disabled');
    startTimer(30, $('.btn-restart'));
    setTimeout(function () {
        location.href = '/admin/settings';
    }, 30000);
}

function tryResetWebsite() {
    var nonce = Math.floor((Math.random() * 128) + 1);
    ajaxPostWithCSRFToken('reset', { nonce }, function () { });
    $('.btn-reset').text('Wait...');
    $('.btn-reset').addClass('disabled');
    $('.btn-reset').attr('disabled', 'disabled');
    startTimer(30, $('.btn-reset'));
    setTimeout(function () {
        location.href = '/';
    }, 30000);
}

function startTimer(duration, display) {
    var timer = duration, minutes, seconds;
    setInterval(function () {
        minutes = parseInt(timer / 60, 10);
        seconds = parseInt(timer % 60, 10);

        minutes = minutes < 10 ? '0' + minutes : minutes;
        seconds = seconds < 10 ? '0' + seconds : seconds;

        display.text(minutes + ':' + seconds);

        if (--timer < 0) {
            timer = duration;
        }
    }, 1000);
}