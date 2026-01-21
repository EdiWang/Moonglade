import { error } from './toastService.mjs'

const csrfFieldName = 'CSRF-TOKEN-MOONGLADE-FORM';

export async function fetch2(uri, method, request) {
    try {
        const csrfValue = document.querySelector(`input[name="${csrfFieldName}"]`)?.value;
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
        } else {
            if (response.status === 204) {
                // No content, no need to parse
                return;
            }

            // Check if response has content before parsing JSON
            const text = await response.text();
            if (!text) {
                return;
            }

            const data = JSON.parse(text);
            return data;
        }
    } catch (err) {
        error(err);
        console.error(err);
    }
}

async function handleHttpError(response) {
    switch (response.status) {
        case 400:
        case 409:
            error(await buildErrorMessage(response));
            break;
        case 401:
            error('Unauthorized');
            break;
        case 404:
            error('Endpoint not found');
            break;
        case 429:
            error('Too many requests');
            break;
        case 500:
        case 503:
            error('Server went boom');
            break;
        default:
            error(`Error ${response.status}`);
            break;
    }
}

async function buildErrorMessage(response) {
    const contentType = response.headers.get('content-type');
    if (contentType && contentType.includes('application/json')) {
        const data = await response.json();
        if (typeof data === 'string') {
            return data;
        } else if (Array.isArray(data.errors) && data.errors.length > 0) {
            return data.errors.join(', ');
        } else if ('errors' in data) {
            // errors is present but null, not an array, or an empty array
            return 'An error occurred';
        } else {
            return Object.entries(data)
                .map(([key, value]) => `${key}: ${value}`)
                .join('\n\r');
        }
    } else {
        return await response.text();
    }
}