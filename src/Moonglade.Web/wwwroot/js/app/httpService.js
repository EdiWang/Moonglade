var csrfFieldName = 'CSRF-TOKEN-MOONGLADE-FORM';
function callApi(uri, method, request, funcSuccess, funcAlways) {
    const csrfValue = document.querySelector(`input[name=${csrfFieldName}]`).value;
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
            if (funcSuccess) {
                funcSuccess(response);
            }
        }
    }).then(response => {
        if (funcAlways) {
            funcAlways(response);
        }
    }).catch(err => {
        blogToast.error(err);
        console.error(err);
    });
}

async function handleHttpError(response) {
    switch (response.status) {
        case 400:
        case 409:
            blogToast.error(await buildErrorMessage2(response));
            break;
        case 401:
            blogToast.error('Unauthorized');
            break;
        case 404:
            blogToast.error('Endpoint not found');
            break;
        case 429:
            blogToast.error('Too many requests');
            break;
        case 500:
        case 503:
            blogToast.error('Server went boom');
            break;
        default:
            blogToast.error(`Error ${response.status}`);
            break;
    }
}

async function buildErrorMessage2(response) {
    const contentType = response.headers.get('content-type');
    if (contentType && contentType.indexOf('application/json') !== -1) {
        var data = await response.json();
        var t = typeof (data);
        
        if (data.combinedErrorMessage) {
            return data.combinedErrorMessage;
        } else if (t == 'string') {
            return data;
        } else {
            var errorMessage2 = 'Error(s):\n\r';

            Object.keys(data).forEach(function (k) {
                errorMessage2 += (k + ': ' + data[k]) + '\n\r';
            });

            return errorMessage2;
        }
    } else {
        var text = await response.text();
        return text;
    }
}