const csrfFieldName = 'CSRF-TOKEN-MOONGLADE-FORM';

async function callApi(uri, method, request, funcSuccess, funcAlways) {
    try {
        const csrfValue = document.querySelector(`input[name="${csrfFieldName}"]`).value;
        const response = await fetch(uri, {
            method,
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json',
                'XSRF-TOKEN': csrfValue
            },
            credentials: 'include',
            body: method === 'GET' ? null : JSON.stringify(request)
        });

        if (!response.ok) {
            await handleHttpError(response);
        } else if (funcSuccess) {
            funcSuccess(response);
        }

        if (funcAlways) {
            funcAlways(response);
        }
    } catch (err) {
        blogToast.error(err);
        console.error(err);
    }
}

async function handleHttpError(response) {
    switch (response.status) {
        case 400:
        case 409:
            blogToast.error(await buildErrorMessage(response));
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

async function buildErrorMessage(response) {
    const contentType = response.headers.get('content-type');
    if (contentType && contentType.includes('application/json')) {
        const data = await response.json();
        if (typeof data === 'string') {
            return data;
        } else if (data.combinedErrorMessage) {
            return data.combinedErrorMessage;
        } else {
            return Object.entries(data)
                .map(([key, value]) => `${key}: ${value}`)
                .join('\n\r');
        }
    } else {
        return await response.text();
    }
}