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
    const contentType = response.headers.get('content-type');
    if (contentType && contentType.includes('application/problem+json')) {
        const data = await response.json();

        // RFC 7807/9457: validation errors in "errors" property
        if (data.errors && typeof data.errors === 'object') {
            return Object.values(data.errors)
                .flat()
                .join('\n');
        }

        // RFC 7807/9457: "detail" is the human-readable explanation
        if (data.detail) {
            return data.detail;
        }

        // Fallback to "title"
        if (data.title) {
            return data.title;
        }
    }

    return `Error ${response.status}`;
}