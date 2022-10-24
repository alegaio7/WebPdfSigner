async function parseFetchResponse(response) {
    var ret;
    if (!response.ok) {
        var message = await response.text();
        if (!message)
            message = response.statusText;
        var tmp;
        try {
            tmp = JSON.parse(message); // check if response is a json object
        } catch (e) {
            tmp = { Result: false, Message: message };
        }

        if (!tmp.StatusCode)
            tmp.StatusCode = response.status;

        if (!tmp.Message && !tmp.Result)
            tmp.Message = "Generic fetch error";

        if (!tmp.Result)
            tmp.Result = false;

        ret = tmp;
    } else {
        ret = await response.json();
    }
    return ret;
}
