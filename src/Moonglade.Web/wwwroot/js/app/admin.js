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
                document.querySelector(`.custom-file-label-${targetName}`).innerText(file.name);
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

                    var div = document.querySelector(`#${targetName}DropTarget`);
                    div.innerHTML = `<img class="img-fluid" src="${imgDataUrl}" />`;
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