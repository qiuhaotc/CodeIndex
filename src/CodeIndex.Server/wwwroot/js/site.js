function ScrollTextAreaToBottom(itemID) {
    var textarea = document.getElementById(itemID);
    textarea.scrollTop = textarea.scrollHeight;
}

function DoAjaxPost(url, data) {
    var result = null;

    $.ajax({
        type: "POST",
        url: url,
        data: JSON.stringify(data),
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
        success: function (data) {
            result = data;
        },
        async: false,
        error: function (err) {
            result = {
                Status: {
                    Success: false,
                    StatusDesc: err.status === 401 ? "401" : "Unknow Error"
                }
            };
        }
    })

    return result;
}

function DoAjaxGet(url, parameters) {
    var result = null;

    $.ajax({
        type: "GET",
        url: url,
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
        data: parameters,
        success: function (data) {
            result = data;
        },
        async: false,
        error: function (err) {
            result = {
                Status: {
                    Success: false,
                    StatusDesc: err.status === 401 ? "401" : "Unknow Error"
                }
            };
        }
    })

    return result;
}

function ShowConfirm(message) {
    return confirm(message);
}