export class WidgetRequestError extends Error {
    constructor(status, problem = {}) {
        super(problem.title || `Error ${status}`);
        this.status = status;
        this.problem = problem;
    }
}

async function request(method, payload) {
    const csrf = document.querySelector('input[name="CSRF-TOKEN-MOONGLADE-FORM"]')?.value;
    const response = await fetch('/api/widgets', {
        method, credentials: 'include',
        headers: { 'Accept': 'application/json', 'Content-Type': 'application/json', 'XSRF-TOKEN': csrf },
        body: method === 'GET' ? null : JSON.stringify(payload)
    });
    let data;
    try { data = await response.json(); } catch { throw new WidgetRequestError(response.status); }
    if (!response.ok) throw new WidgetRequestError(response.status, data);
    return data;
}

export const loadWidgetCollection = () => request('GET');
export const saveWidgetCollection = payload => request('PUT', payload);
