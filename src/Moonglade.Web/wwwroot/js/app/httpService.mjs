const csrfFieldName = 'CSRF-TOKEN-MOONGLADE-FORM';

export async function fetch2(uri, method, request) {
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
        throw await buildErrorMessage(response);
    }

    if (response.status === 204) {
        return;
    }

    // Check if response has content before parsing JSON
    const text = await response.text();
    if (!text) {
        return;
    }

    return JSON.parse(text);
}

async function buildErrorMessage(response) {
    switch (response.status) {
        case 400:
        case 409: {
            const contentType = response.headers.get('content-type');
            if (contentType && contentType.includes('application/json')) {
                const data = await response.json();
                if (typeof data === 'string') {
                    return data;
                } else if (data.errors && Array.isArray(data.errors)) {
                    return data.errors.join(', ');
                } else {
                    return Object.entries(data)
                        .map(([key, value]) => `${key}: ${value}`)
                        .join('\n\r');
                }
            }
            return await response.text();
        }
        case 401:
            return 'Unauthorized';
        case 404:
            return 'Endpoint not found';
        case 429:
            return 'Too many requests';
        case 500:
        case 503:
            return 'Server went boom';
        default:
            return `Error ${response.status}`;
    }
}