function toMagicJson(value) {
    const newValue = {};
    for (let item in value) {
        if (Object.prototype.hasOwnProperty.call(value, item)) {
            if (!value[item]) {
                newValue[item.replace('ViewModel.', '')] = null;
            }
            else if (value[item] && !Array.isArray(value[item]) && value[item].toLowerCase() === 'true') {
                newValue[item.replace('ViewModel.', '')] = true;
            }
            else if (value[item] && !Array.isArray(value[item]) && value[item].toLowerCase() === 'false') {
                newValue[item.replace('ViewModel.', '')] = false;
            }
            else {
                newValue[item.replace('ViewModel.', '')] = value[item];
            }
        }
    }

    return newValue;
}

function handleSettingsSubmit(event) {
    event.preventDefault();

    onUpdateSettingsBegin();

    const data = new FormData(event.target);
    const value = Object.fromEntries(data.entries());
    const newValue = toMagicJson(value);

    callApi(event.currentTarget.action, 'POST', newValue,
        (resp) => {
            blogToast.success('Settings Updated');
            onUpdateSettingsComplete();
        });
}

var btnSaveSettings = '#btn-save-settings';
var onUpdateSettingsBegin = function () {
    document.querySelector(btnSaveSettings).classList.add('disabled');
    document.querySelector(btnSaveSettings).setAttribute('disabled', 'disabled');
};

var onUpdateSettingsComplete = function () {
    document.querySelector(btnSaveSettings).classList.remove('disabled');
    document.querySelector(btnSaveSettings).removeAttribute('disabled');
};

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
            document.querySelector(`#btn-upload-${targetName}`).classList.add('disabled');
            document.querySelector(`#btn-upload-${targetName}`).setAttribute('disabled', 'disabled');

            var rawData = imgDataUrl.replace(/^data:image\/(png|jpeg|jpg);base64,/, '');

            callApi(uploadUrl,
                'POST',
                rawData,
                function(resp) {
                    $(`#${targetName}modal`).modal('hide');
                    blogToast.success('Updated');
                    d = new Date();
                    document.querySelector(`.blogadmin-${targetName}`).src = `/${targetName}?${d.getTime()}`;
                },
                function(always) {
                    document.querySelector(`#btn-upload-${targetName}`).classList.remove('disabled');
                    document.querySelector(`#btn-upload-${targetName}`).removeAttribute('disabled');
                });

        } else {
            blogToast.error('Please select an image');
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
                blogToast.error('Please select an image file.');
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
                    document.querySelector(`#btn-upload-${targetName}`).classList.remove('disabled');
                    document.querySelector(`#btn-upload-${targetName}`).removeAttribute('disabled');
                }
            }
            reader.readAsDataURL(file);
        } else {
            blogToast.error('The File APIs are not fully supported in this browser.');
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
                // height: ,
                relative_urls: false, // avoid image upload fuck up
                browser_spellcheck: true,
                branding: false,
                block_formats: 'Paragraph=p; Header 2=h2; Header 3=h3; Header 4=h4; Preformatted=pre',
                fontsize_formats: '8pt 10pt 12pt 14pt 18pt 24pt 36pt',
                plugins: 'advlist autolink hr autosave link image lists charmap print preview hr anchor pagebreak searchreplace wordcount visualblocks visualchars code fullscreen insertdatetime media nonbreaking save table directionality template paste codesample emoticons',
                toolbar: 'formatselect | fontsizeselect | bold italic underline strikethrough | forecolor backcolor | removeformat | emoticons link hr image table codesample media | alignleft aligncenter alignright alignjustify | bullist numlist outdent indent | code | fullscreen',
                save_onsavecallback: function () {
                    $('#btn-save').trigger('click');
                },
                paste_data_images: true,
                images_upload_url: '/image',
                images_upload_credentials: true,
                extended_valid_elements: 'img[class|src|border=0|alt|title|hspace|vspace|width|height|align|onmouseover|onmouseout|name|loading=lazy]',
                body_class: 'post-content',
                content_css: '/css/tinymce-bs-bundle.min.css',
                codesample_languages: [
                    { text: 'Bash', value: 'bash' },
                    { text: 'C#', value: 'csharp' },
                    { text: 'C', value: 'c' },
                    { text: 'C++', value: 'cpp' },
                    { text: 'CSS', value: 'css' },
                    { text: 'Dart', value: 'dart' },
                    { text: 'F#', value: 'fsharp' },
                    { text: 'Go', value: 'go' },
                    { text: 'HTML/XML', value: 'markup' },
                    { text: 'JavaScript', value: 'javascript' },
                    { text: 'Json', value: 'json' },
                    { text: 'Markdown', value: 'markdown' },
                    { text: 'PowerShell', value: 'powershell' },
                    { text: 'Plain Text', value: 'plaintext' },
                    { text: 'Python', value: 'python' },
                    { text: 'PHP', value: 'php' },
                    { text: 'Ruby', value: 'ruby' },
                    { text: 'Rust', value: 'rust' },
                    { text: 'SQL', value: 'sql' },
                    { text: 'TypeScript', value: 'typescript' },
                    { text: 'Visual Basic', value: 'vb' },
                    { text: 'YAML', value: 'yaml' }
                ],
                setup: function (editor) {
                    editor.on('NodeChange', function (e) {
                        if (e.element.tagName === 'IMG') {
                            e.element.setAttribute('loading', 'lazy');
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
                uploadUrl: '/image',
                urlText: '![file]({filename})',
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
        $('#ViewModel_Title').change(function () {
            document.querySelector('#ViewModel_Slug').value = slugify($(this).val());
        });

        var tagnames = new Bloodhound({
            datumTokenizer: Bloodhound.tokenizers.obj.whitespace('name'),
            queryTokenizer: Bloodhound.tokenizers.whitespace,
            prefetch: {
                url: '/api/tags/names',
                filter: function (list) {
                    return $.map(list, function (tagname) {
                        return { name: tagname };
                    });
                }
            }
        });

        tagnames.initialize();
        $('#ViewModel_Tags').tagsinput({
            typeaheadjs: {
                name: 'tagnames',
                displayKey: 'name',
                valueKey: 'name',
                source: tagnames.ttAdapter()
            },
            trimValue: true
        });

        $('#btn-preview').click(function (e) {
            submitForm(e);
        });

        $('#btn-save').click(function (e) {
            submitForm(e);
        });

        $('#btn-publish').click(function (e) {
            $('input[name="ViewModel.IsPublished"]').val('True');
            submitForm(e);
        });

        function submitForm(e) {
            if (window.tinyMCE) {
                window.tinyMCE.triggerSave();
            }

            if ($('input[name="ViewModel.IsPublished"]').val() === 'True') {
                document.querySelector('#btn-publish').style.display = 'none';
                document.querySelector('#btn-preview').style.display = 'none';
            }
        }

        $('.post-edit-form').areYouSure({
            message: 'You have unsaved changes, are you sure to leave this page?'
        });

        $('#ViewModel_Title').focus();
    },
    keepAlive: function () {
        var tid = setInterval(postNonce, 60 * 1000);
        function postNonce() {
            var num = Math.random();
            fetch('/api/post/keep-alive',
                {
                    method: 'POST',
                    headers: {
                        'Accept': 'application/json',
                        'Content-Type': 'application/json'
                    },
                    credentials: 'include',
                    body: JSON.stringify({ nonce: num })
                }).then(async (response) => {
                    console.info('live');
                });
        }
        function abortTimer() {
            clearInterval(tid);
        }
    }
};

var btnSubmitPost = '#btn-save';
var onPostCreateEditBegin = function () {
    document.querySelector(btnSubmitPost).classList.add('disabled');
    document.querySelector(btnSubmitPost).setAttribute('disabled', 'disabled');
};

var onPostCreateEditComplete = function () {
    document.querySelector(btnSubmitPost).classList.remove('disabled');
    document.querySelector(btnSubmitPost).removeAttribute('disabled');
};

var onPostCreateEditSuccess = function (data) {
    if (data.postId) {
        $('input[name="ViewModel.PostId"]').val(data.postId);
        blogToast.success('Post saved successfully.');

        if (isPreviewRequired) {
            isPreviewRequired = false;
            window.open(`/post/preview/${data.postId}`);
        }
    }
};

var onPageCreateEditFailed = function (context) {
    var message = buildErrorMessage(context);

    if (blogToast) {
        blogToast.error(message);
    } else {
        alert(`Error: ${message}`);
    }
};

function deletePost(postid) {
    callApi(`/api/post/${postid}/destroy`, 'DELETE', {},
        (resp) => {
            document.querySelector(`#tr-${postid}`).remove();
            blogToast.success('Post deleted');
        });
}

function restorePost(postid) {
    callApi(`/api/post/${postid}/restore`, 'POST', {},
        (resp) => {
            document.querySelector(`#tr-${postid}`).remove();
            blogToast.success('Post restored');
        });
}

function deleteSelectedComments() {
    var cids = [];
    $('.chk-cid:checked').each(function () {
        cids.push($(this).data('cid'));
    });

    callApi('/api/comment/delete', 'DELETE', cids,
        (success) => {
            $.each(cids, function (index, value) {
                document.querySelector(`#panel-comment-${value}`).remove();
            });
        });
}