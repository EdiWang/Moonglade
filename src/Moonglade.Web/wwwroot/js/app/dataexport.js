function convertJSONtoCSV(data) {
    const csvRows = [];
    const headers = Object.keys(data[0]);
    csvRows.push(headers.join(','));

    for (const row of data) {
        const values = headers.map(header => {
            const escapedValue = ('' + row[header]).replace(/"/g, '\\"');
            return `"${escapedValue}"`;
        });
        csvRows.push(values.join(','));
    }

    return csvRows.join('\n');
}

function downloadCSV(csvData, filename) {
    const blob = new Blob([csvData], { type: 'text/csv' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.setAttribute('hidden', '');
    a.setAttribute('href', url);
    a.setAttribute('download', filename);
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
}

function exportCSV(api, filename) {
    fetch(api)
        .then(response => response.json())
        .then(data => {
            const csvData = convertJSONtoCSV(data);
            downloadCSV(csvData, filename);
        })
        .catch(error => {
            console.error('Error:', error);
        });
}