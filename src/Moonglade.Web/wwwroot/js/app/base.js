var isDarkMode = false;

var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
    return new bootstrap.Tooltip(tooltipTriggerEl);
});

function buildErrorMessage(responseObject) {
    if (responseObject.responseJSON) {
        var json = responseObject.responseJSON;
        if (json.combinedErrorMessage) {
            return json.combinedErrorMessage;
        } else {
            var errorMessage = 'Error(s):\n\r';

            Object.keys(json).forEach(function (k) {
                errorMessage += (k + ': ' + json[k]) + '\n\r';
            });

            return errorMessage;
        }
    }

    if (responseObject.responseText) {
        return responseObject.responseText.trim();
    }

    return responseObject.status;
}
