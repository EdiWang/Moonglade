import { fetch2 } from './httpService.mjs?v=1500'
import { success, error } from './toastService.mjs'

export class ImageUploader {
    constructor(targetName, hw, imgMimeType) {
        var imgDataUrl = '';

        this.uploadImage = async function (uploadUrl) {
            if (imgDataUrl) {
                var btnUpload = document.querySelector(`#btn-upload-${targetName}`);
                btnUpload.classList.add('disabled');
                btnUpload.setAttribute('disabled', 'disabled');

                var rawData = imgDataUrl.replace(/^data:image\/(png|jpeg|jpg);base64,/, '');

                try {
                    await fetch2(uploadUrl, 'POST', rawData);

                    var modalElement = document.getElementById(`${targetName}modal`);
                    var modal = bootstrap.Modal.getInstance(modalElement);
                    if (modal) modal.hide();

                    success('Updated');
                    var d = new Date();
                    document.querySelector(`.blogadmin-${targetName}`).src = `/${targetName}?${d.getTime()}`;
                } catch (err) {
                    error(err);
                } finally {
                    btnUpload.classList.remove('disabled');
                    btnUpload.removeAttribute('disabled');
                }
            } else {
                error('Please select an image');
            }
        };

        this.fileSelect = function (evt) {
            evt.stopPropagation();
            evt.preventDefault();

            if (window.File && window.FileReader && window.FileList && window.Blob) {
                var file;
                if (evt.dataTransfer) {
                    file = evt.dataTransfer.files[0];
                    document.querySelector(`.custom-file-label-${targetName}`).innerText = file.name;
                } else {
                    file = evt.target.files[0];
                }

                if (!file.type.match('image.*')) {
                    error('Please select an image file.');
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
                        ctx.drawImage(tempImg, 0, 0, tempW, tempH);
                        imgDataUrl = canvas.toDataURL(imgMimeType);

                        var div = document.querySelector(`#${targetName}DropTarget`);
                        div.innerHTML = `<img class="img-fluid" src="${imgDataUrl}" />`;
                        var btnUpload = document.querySelector(`#btn-upload-${targetName}`);
                        btnUpload.classList.remove('disabled');
                        btnUpload.removeAttribute('disabled');
                    };
                };
                reader.readAsDataURL(file);
            } else {
                error('The File APIs are not fully supported in this browser.');
            }
        };

        this.dragOver = function (evt) {
            evt.stopPropagation();
            evt.preventDefault();
            evt.dataTransfer.dropEffect = 'copy';
        };

        this.bindEvents = function () {
            document.getElementById(`${targetName}ImageFile`).addEventListener('change', this.fileSelect, false);
            var dropTarget = document.getElementById(`${targetName}DropTarget`);
            dropTarget.addEventListener('dragover', this.dragOver, false);
            dropTarget.addEventListener('drop', this.fileSelect, false);
        };

        this.getDataUrl = function () {
            return imgDataUrl;
        };
    }
}
