export class WidgetRequestError extends Error {
    constructor(status, problem = {}) {
        super(problem.title || `Error ${status}`);
        this.status = status;
        this.problem = problem;
    }
}

async function request(method, payload, preview = false, signal) {
    const csrf = document.querySelector('input[name="CSRF-TOKEN-MOONGLADE-FORM"]')?.value;
    const culture = preview ? document.querySelector('#localizedStrings')?.dataset?.previewCulture ?? document.documentElement.lang ?? 'en-US' : '';
    const address = preview ? `/api/widgets/preview?dark=${document.documentElement.dataset.bsTheme === 'dark'}&culture=${encodeURIComponent(culture)}&ui-culture=${encodeURIComponent(culture)}` : '/api/widgets';
    const response = await fetch(address, {
        method, credentials: 'include', signal,
        headers: { 'Accept': preview ? 'text/html' : 'application/json', 'Content-Type': 'application/json', 'XSRF-TOKEN': csrf },
        body: method === 'GET' ? null : JSON.stringify(payload)
    });
    if (preview && response.ok && response.headers.get('Content-Type')?.startsWith('text/html')) {
        const html = await response.text();
        if (!html.trim()) throw new WidgetRequestError(response.status);
        return html;
    }
    let data;
    try { data = await response.json(); } catch { throw new WidgetRequestError(response.status); }
    if (!response.ok || preview) throw new WidgetRequestError(response.status, data);
    return data;
}

export const loadWidgetCollection = () => request('GET');
export const saveWidgetCollection = payload => request('PUT', payload);
export const previewWidgetCollection = (payload, signal) => request('POST', payload, true, signal);
