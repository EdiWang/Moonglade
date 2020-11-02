var csrfFieldName = 'CSRF-TOKEN-MOONGLADE-FORM';
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
        body: method === 'GET' ? null : JSON.stringify(request)
    }).then(async (response) => {
        if (!response.ok) {
            await handleHttpError(response);
        } else {
            funcDone(response);
        }
    }).catch(err => {
        toastr.error(err);
        console.error(err);
    });
}

async function handleHttpError(response) {
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