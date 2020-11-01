var csrfFieldName = 'CSRF-TOKEN-MOONGLADE-FORM';
function ajaxPostWithCSRFToken(url, pData, funcSuccess) {
    var options = {
        type: 'POST',
        url: url,
        headers: {},
        data: pData,
        dataType: 'json',
        success: function (data) {
            funcSuccess(data);
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
            503: function(responseObject, textStatus, jqXHR) {
                toastr.error('Server went boom boom');
            }
        }
    };
    options.headers['XSRF-TOKEN'] = $(`input[name=${csrfFieldName}]`).val();
    $.ajax(options);
}