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
            503: function (responseObject, textStatus, jqXHR) {
                toastr.error('Server went boom boom');
            }
        }
    };
    options.headers['XSRF-TOKEN'] = $(`input[name=${csrfFieldName}]`).val();
    $.ajax(options);
}

function callApi(uri, method, request, funcDone) {
    const csrfValue = $(`input[name=${csrfFieldName}]`).val();
    fetch(uri, {
        method: method,
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json',
            'XSRF-TOKEN': csrfValue
        },
        credentials: 'include',
        body: JSON.stringify(request)
    }).then(async (response) => await handleHttpError(response)).then((response) => {
        funcDone(response);
    }).catch(err => {
        toastr.error(err);
        console.error(err);
    });
}

async function handleHttpError(response) {
    if (response.ok) {
        return;
    }

    switch (response.status) {
        case 400:
            toastr.error(await buildErrorMessage2(response));
            break;
        case 401:
            toastr.error('Unauthorized');
            break;
        case 404:
            toastr.error('Endpoint not found');
            break;
        case 409:
            toastr.error(await buildErrorMessage2(response));
            break;
        case 429:
            toastr.error('Too many requests');
            break;
        case 500:
        case 503:
            toastr.error('Server went boom');
            break;
        default:
            toastr.error(`Error ${response.status}`);
            break;
    }
}

async function buildErrorMessage2(response) {
    const contentType = response.headers.get('content-type');
    if (contentType && contentType.indexOf('application/json') !== -1) {
        var data = await response.json();
        console.info(data);

        var errorMessage2 = 'Error(s):\n\r';

        Object.keys(data).forEach(function (k) {
            errorMessage2 += (k + ': ' + data[k]) + '\n\r';
        });

        return errorMessage2;
    } else {
        var text = await response.text();
        return text;
    }
}