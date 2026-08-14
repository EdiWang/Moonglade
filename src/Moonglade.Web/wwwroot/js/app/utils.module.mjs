export function toMagicJson(value) {
    const newValue = {};

    for (const item in value) {
        if (Object.prototype.hasOwnProperty.call(value, item)) {
            const key = item.replace(/^ViewModel\.|^settings\./, '');
            const val = value[item];

            if (!val) {
                newValue[key] = null;
            } else if (typeof val === 'string') {
                const lowerVal = val.toLowerCase();
                if (lowerVal === 'true') {
                    newValue[key] = true;
                } else if (lowerVal === 'false') {
                    newValue[key] = false;
                } else {
                    newValue[key] = val;
                }
            } else {
                newValue[key] = val;
            }
        }
    }

    return newValue;
}

export function formatUtcTime(includeTime = true) {
    const timeElements = document.querySelectorAll('time');

    timeElements.forEach(e => {
        const utclabel = e.getAttribute('data-utc-label');
        if (!utclabel) return;

        const localTime = parseUtcDate(utclabel);
        if (!localTime) {
            e.textContent = utclabel.trim();
            return;
        }

        const formattedTime = includeTime
            ? localTime.toLocaleString()
            : localTime.toLocaleDateString();

        e.textContent = formattedTime;
    });
}

export function parseUtcDate(dateString) {
    if (!dateString) return null;

    let normalized = dateString.trim().replace(' ', 'T');
    if (/^\d{4}-\d{2}-\d{2}$/.test(normalized)) {
        normalized = `${normalized}T00:00:00Z`;
    } else if (!/(?:Z|[+-]\d{2}:\d{2})$/i.test(normalized)) {
        normalized = `${normalized}Z`;
    }

    const date = new Date(normalized);
    return Number.isNaN(date.getTime()) ? null : date;
}

export function toUtcDatePath(dateString) {
    const date = parseUtcDate(dateString);
    if (!date) return null;

    return `${date.getUTCFullYear()}/${date.getUTCMonth() + 1}/${date.getUTCDate()}`;
}

export function toLocalDateTimeInputValue(dateString) {
    const date = parseUtcDate(dateString);
    if (!date) return '';

    const pad = value => value.toString().padStart(2, '0');
    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
}

export function localDateBoundaryToUtcIso(dateString, endOfDay = false) {
    if (!dateString) return null;

    const localTime = new Date(`${dateString}T${endOfDay ? '23:59:59' : '00:00:00'}`);
    return Number.isNaN(localTime.getTime()) ? null : localTime.toISOString();
}

export function parseMetaContent(metaName) {
    const metaTag = document.querySelector(`meta[name="${metaName}"]`);
    if (!metaTag) return null;

    const contentValue = metaTag.content.trim();

    if (contentValue.toLowerCase() === "true") return true;
    if (contentValue.toLowerCase() === "false") return false;

    const numberValue = Number(contentValue);
    if (!isNaN(numberValue)) return numberValue;

    return contentValue;
}

export function getLocalizedString(key) {
    const container = document.getElementById('localizedStrings');
    return container ? container.dataset[key] : '';
}

export function slugify(text) {
    if (!/^[A-Za-z][A-Za-z0-9 \(\)#,\.\?]*$/.test(text)) {
        return '';
    }
    return text
        .toLowerCase()
        .replace(/[()#,.?]/g, '')
        .replace(/[^\w ]+/g, '')
        .replace(/ +/g, '-');
}

export function formatDateString(dateString) {
    if (!dateString) return '';
    const date = parseUtcDate(dateString);
    return date ? date.toLocaleString() : dateString;
}

