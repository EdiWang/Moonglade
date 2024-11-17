function convertJSONtoCSV(data) {
    if (!data || !data.length) {
        console.error('No data provided');
        return '';
    }

    const csvRows = [];
    const headers = Object.keys(data[0]);
    csvRows.push(headers.join(','));

    for (const row of data) {
        const values = headers.map(header => {
            let value = row[header];
            if (value === null || value === undefined) {
                value = '';
            }
            const escapedValue = ('' + value).replace(/"/g, '""');
            return `"${escapedValue}"`;
        });
        csvRows.push(values.join(','));
    }

    return csvRows.join('\n');
}

function downloadCSV(csvData, filename) {
    if (!csvData) {
        console.error('No CSV data to download');
        return;
    }

    const blob = new Blob([csvData], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.style.display = 'none';
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
}

function exportCSV(api, filename) {
    fetch(api)
        .then(response => {
            if (!response.ok) {
                throw new Error('Network response was not ok');
            }
            return response.json();
        })
        .then(data => {
            const csvData = convertJSONtoCSV(data);
            downloadCSV(csvData, filename);
        })
        .catch(error => {
            console.error('Error:', error);
        });
}
