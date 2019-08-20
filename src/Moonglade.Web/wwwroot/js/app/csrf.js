var csrfFieldName = "CSRF-TOKEN-MOONGLADE-FORM";
function makeCSRFExtendedData(data) {
    var options = {};
    options[csrfFieldName] = $("input[name=" + csrfFieldName + "]").val();
    var extData = $.extend(data, options);
    return extData;
}

function ajaxPostWithCSRFToken(url, pData, funcSuccess) {
    var options = {
        type: "POST",
        url: url,
        headers: {},
        data: makeCSRFExtendedData(pData),
        success: function (data) {
            funcSuccess(data);
        },
        dataType: "json"
    };
    options.headers[csrfFieldName] = $("input[name=" + csrfFieldName + "]").val();
    $.ajax(options);
}