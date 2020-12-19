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
                headers: { 'XSRF-TOKEN': $(`input[name=${csrfFieldName}]`).val() },
                url: uploadUrl,
                data: rawData,
                success: function (data) {
                    console.info(data);
                    $(`#${targetName}modal`).modal('hide');
                    toastr.success('Updated');
                    d = new Date();
                    $(`.blogadmin-${targetName}`).attr('src', `/${targetName}?${d.getTime()}`);
                },
                statusCode: {
                    400: function (responseObject, textStatus, jqXHR) {
                        var message = buildErrorMessage(responseObject);
                        toastr.error(message);
                    },
                    401: function (responseObject, textStatus, jqXHR) {
                        toastr.error('Unauthorized');
                    },
                    404: function (responseObject, textStatus, jqXHR) {
                        toastr.error('Endpoint not found');
                    },
                    409: function (responseObject, textStatus, jqXHR) {
                        var message = buildErrorMessage(responseObject);
                        toastr.error(message);
                    },
                    500: function (responseObject, textStatus, jqXHR) {
                        toastr.error('Server went boom');
                    },
                    503: function (responseObject, textStatus, jqXHR) {
                        toastr.error('Server went boom boom');
                    }
                },
                error: function (xhr, status, err) {
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
                block_formats: 'Paragraph=p; Header 2=h2; Header 3=h3; Header 4=h4; Preformatted=pre',
                fontsize_formats: '8pt 10pt 12pt 14pt 18pt 24pt 36pt',
                plugins: 'advlist autolink hr autosave link image lists charmap print preview hr anchor pagebreak spellchecker searchreplace wordcount visualblocks visualchars code fullscreen insertdatetime media nonbreaking save table directionality template paste codesample imagetools',
                toolbar: 'undo redo | formatselect | fontsizeselect | bold italic strikethrough forecolor backcolor | removeformat | link image codesample media | alignleft aligncenter alignright alignjustify | bullist numlist outdent indent | code | fullscreen',
                paste_data_images: true,
                images_upload_url: '/image',
                images_upload_credentials: true,
                body_class: 'post-content',
                content_css: '/css/tinymce-bs-bundle.min.css',
                codesample_languages: [
                    { text: 'Bash', value: 'bash' },
                    { text: 'C#', value: 'csharp' },
                    { text: 'C', value: 'c' },
                    { text: 'C++', value: 'cpp' },
                    { text: 'CSS', value: 'css' },
                    { text: 'F#', value: 'fsharp' },
                    { text: 'HTML/XML', value: 'markup' },
                    { text: 'JavaScript', value: 'javascript' },
                    { text: 'Json', value: 'json' },
                    { text: 'PowerShell', value: 'powershell' },
                    { text: 'Python', value: 'python' },
                    { text: 'PHP', value: 'php' },
                    { text: 'Ruby', value: 'ruby' },
                    { text: 'SQL', value: 'sql' },
                    { text: 'Visual Basic', value: 'vb' }
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
                uploadUrl: '/image',
                urlText: '![file](/image/{filename})',
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
                url: '/api/tags/names',
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
    var message = buildErrorMessage(context);
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

        if ($('input[name="IsPublished"]:checked').val() === 'true') {
            $('#btn-preview').hide();
        }

        if (isPreviewRequired) {
            isPreviewRequired = false;
            window.open(`/page/preview/${data.pageId}`);
        }
    }
};

var onPageCreateEditFailed = function (context) {
    var message = buildErrorMessage(context);

    if (window.toastr) {
        window.toastr.error(message);
    } else {
        alert(`Error: ${message}`);
    }
};

function deletePost(postid) {
    $(`#span-processing-${postid}`).show();
    callApi(`/post/manage/${postid}/destroy`, 'DELETE', {},
        (resp) => {
            $(`#tr-${postid}`).hide();
            toastr.success('Post deleted');
        });
}

function restorePost(postid) {
    $(`#span-processing-${postid}`).show();
    callApi(`/post/manage/${postid}/restore`, 'POST', {},
        (resp) => {
            $(`#tr-${postid}`).hide();
            toastr.success('Post restored');
        });
}

function deleteFriendLink(friendlinkid) {
    $(`#span-processing-${friendlinkid}`).show();

    callApi(`/admin/settings/friendlink/${friendlinkid}`, 'DELETE', {},
        (resp) => {
            $(`#tr-${friendlinkid}`).hide();
        });
}

function deleteMenu(menuid) {
    $(`#span-processing-${menuid}`).show();

    callApi(`/api/menu/${menuid}`, 'DELETE', {},
        (resp) => {
            $(`#tr-${menuid}`).hide();
        });
}

function initCreateFriendLink() {
    $("#FriendLinkEditViewModel_Id").val(emptyGuid);
    $("#edit-form")[0].reset();
    $('#editFriendlinkModal').modal();
}

function editFriendLink(id) {
    $.get(`/admin/settings/friendlink/edit/${id}`, function (data) {
        $("#FriendLinkEditViewModel_Id").val(data.id);
        $("#FriendLinkEditViewModel_Title").val(data.title);
        $("#FriendLinkEditViewModel_LinkUrl").val(data.linkUrl);
        $("#editFriendlinkModal").modal();
    });
}

function deleteAccount(accountid) {
    $(`#span-processing-${accountid}`).show();

    callApi(`/admin/settings/account/${accountid}`, 'DELETE', {},
        (resp) => {
            $(`#tr-${accountid}`).hide();
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
                $(`#panel-comment-${value}`).slideUp();
            });
        });
}

function initCreateCategory() {
    $("#CategoryEditViewModel_Id").val(emptyGuid);
    $("#edit-form")[0].reset();
    $('#editCatModal').modal();
}

function editCat(id) {
    callApi(`/api/category/edit/${id}`, 'GET', {},
        async (resp) => {
            var data = await resp.json();
            $("#CategoryEditViewModel_Id").val(data.id);
            $("#CategoryEditViewModel_RouteName").val(data.routeName);
            $("#CategoryEditViewModel_DisplayName").val(data.displayName);
            $("#CategoryEditViewModel_Note").val(data.note);
            $("#editCatModal").modal();
        });
}

function deleteCat(catid) {
    $(`#span-processing-${catid}`).show();

    callApi(`/api/category/delete/${catid}`, 'DELETE', {},
        (resp) => {
            $(`#card-${catid}`).hide();
            toastr.success('Category deleted');
        });
}

function initCreateMenu() {
    $("#MenuEditViewModel_Id").val(emptyGuid);
    $("#edit-form")[0].reset();
    $('#editMenuModal').modal();
}

function editMenu(id) {
    callApi(`/api/menu/edit/${id}`, 'GET', {},
        async (resp) => {
            var data = await resp.json();
            $("#MenuEditViewModel_Id").val(data.id);
            $("#MenuEditViewModel_Title").val(data.title);
            $("#MenuEditViewModel_Url").val(data.url);
            $("#MenuEditViewModel_Icon").val(data.icon);
            $("#MenuEditViewModel_DisplayOrder").val(data.displayOrder);
            if (data.isOpenInNewTab) {
                $("#MenuEditViewModel_IsOpenInNewTab").prop('checked', 'checked');
            }
            $("#editMenuModal").modal();
        });
}

function deletePage(pageid, slug) {
    $(`#span-processing-${pageid}`).show();

    callApi(`/page/${pageid}/${slug}`,
        'DELETE',
        {},
        (resp) => {
            $(`#card-${pageid}`).hide();
            toastr.success("Page deleted");
        });
}

function deletePingback(pingbackId) {
    $(`#span-processing-${pingbackId}`).show();

    callApi(`/pingback/${pingbackId}`, 'DELETE', {},
        (resp) => {
            $(`#pingback-box-${pingbackId}`).slideUp();
        });
}