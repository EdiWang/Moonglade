var csrfFieldName = 'CSRF-TOKEN-MOONGLADE-FORM';
function makeCSRFExtendedData(data) {
    var options = {};
    options[csrfFieldName] = $(`input[name=${csrfFieldName}]`).val();
    var extData = $.extend(data, options);
    return extData;
}

function ajaxPostWithCSRFToken(url, pData, funcSuccess) {
    var options = {
        type: 'POST',
        url: url,
        headers: {},
        data: makeCSRFExtendedData(pData),
        success: function (data) {
            funcSuccess(data);
        },
        statusCode: {
            404: function (responseObject, textStatus, jqXHR) {
                toastr.error('Endpoint not found.');
            },
            409: function (responseObject, textStatus, jqXHR) {
                var json = responseObject.responseJSON;
                var errorMessage = 'Error(s):\n\r';

                Object.keys(json).forEach(function (k) {
                    errorMessage += (k + ': ' + json[k]) + '\n\r';
                });

                toastr.error(errorMessage);
            }
        },
        dataType: 'json'
    };
    options.headers[csrfFieldName] = $(`input[name=${csrfFieldName}]`).val();
    $.ajax(options);
}