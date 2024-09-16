export function toMagicJson(value) {
    const newValue = {};
    for (let item in value) {
        if (Object.prototype.hasOwnProperty.call(value, item)) {
            if (!value[item]) {
                newValue[item.replace('ViewModel.', '')] = null;
                newValue[item.replace('settings.', '')] = null;
            }
            else if (value[item] && !Array.isArray(value[item]) && value[item].toLowerCase() === 'true') {
                newValue[item.replace('ViewModel.', '')] = true;
                newValue[item.replace('settings.', '')] = true;
            }
            else if (value[item] && !Array.isArray(value[item]) && value[item].toLowerCase() === 'false') {
                newValue[item.replace('ViewModel.', '')] = false;
                newValue[item.replace('settings.', '')] = false;
            }
            else {
                newValue[item.replace('ViewModel.', '')] = value[item];
                newValue[item.replace('settings.', '')] = value[item];
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
