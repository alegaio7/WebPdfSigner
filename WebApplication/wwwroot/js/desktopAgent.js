const DEFAULT_AGENT_PORT = 20202;

export default class DesktopAgent {
    constructor(agentPort) {
        if (!agentPort)
            agentPort = DEFAULT_AGENT_PORT;
        this._agentUrl = `https://localhost:${agentPort}/api`;
    }

    async checkAgent(silent) {
        silent = !!silent;
        var _t = this;
        var options = {
            method: 'GET',
            headers: {}
        };
        options.headers['Content-Type'] = 'application/json';
        var response;
        try {
            response = await fetch(`${_t._agentUrl}/health?silent=${silent ? "true" : "false"}`, options);
            response = await parseFetchResponse(response);
        } catch (e) {
            response = { Result: false, Message: "Fetch failed" };
        }
        return response;
    }

    async selectDigitalId() {
        var _t = this;
        var options = {
            method: 'GET',
            headers: {}
        };
        options.headers['Content-Type'] = 'application/json';
        var response;
        try {
            response = await fetch(`${_t._agentUrl}/signatures/selectdigitalid`, options);
            response = await parseFetchResponse(response);
        } catch (e) {
            response = { Result: false, Message: "Fetch failed" };
        }
        return response;
    }

    async signHashes(payload) {
        var _t = this;
        var options = {
            method: 'POST',
            headers: {},
            body: JSON.stringify(payload)
        };
        options.headers['Content-Type'] = 'application/json';
        var response;
        try {
            response = await fetch(`${_t._agentUrl}/signatures/signhashes`, options);
            response = await parseFetchResponse(response);
        } catch (e) {
            response = { Result: false, Message: "Fetch failed" };
        }
        return response;
    }
}
