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
        if (utclabel) {
            const localTime = new Date(utclabel.replace(/-/g, "/"));
            const formattedTime = includeTime ? localTime.toLocaleString() : localTime.toLocaleDateString();
            e.innerHTML = formattedTime;
        }
    });
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
