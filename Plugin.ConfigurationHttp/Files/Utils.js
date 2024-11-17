var Utils = {
	Async: {
		LoadAsync: function (ajax, callback) {
			/// <summary>Асинхронная загрузка с сервера</summary>
			var _this = this;

			var request = new XMLHttpRequest();
			request.onreadystatechange = function () { _this.OnLoaded(request, callback); };
			request.withCredentials = ajax.withCredentials;
			request.open(ajax.Method, ajax.Url, true);
			request.setRequestHeader("Content-Type", "application/json; charset=utf-8");
			request.setRequestHeader("Accept", "application/json");

			if (typeof (ajax.Params) === "undefined" || ajax.Params == null)
				request.send();
			else
				request.send(ajax.Params);

			return ajax.Method == "GET"
				? ajax.Url
				: Utils.Stringify("json", "undefined", ajax.Params);
		},

		OnLoaded: function (objRequest, callback) {
			if (objRequest.readyState != 4) return;
			//if (objRequest.status != 200){
			//	console.log("Error: Ajax.OnLoaded. Status: " + objRequest.status);
			//} else {
			Utils.Async.ProcessResponse("json", objRequest.responseText, callback)
			//}
		},

		ProcessResponse: function (outType, data, callback) {
			var text;
			switch (outType) {
				case "json":
					text = data;
					break;
				case "xml":
					if (typeof (data.responseText) != "undefined")
						text = data.responseText;
					else if (data.documentElement.outerHTML)
						text = data.documentElement.outerHTML;
					else if (typeof (data.xml) != "undefined")
						text = data.xml;
					else
						text = "You browser does not support XML schema";
					break;
				default:
					text = "Unknown accept header";
					break;
			}

			callback(text);
		},
	},

	i: { _templateCache: {} },

	GetSelected: function (id) {
		var ctl = document.getElementById(id);
		return ctl.options[ctl.selectedIndex].value;
	},

	CollectFormData: function (bodyId, isArray) {
		var body = document.getElementById(bodyId);
		var containers = body.getElementsByTagName("DL");
		var json = isArray ? [] : new Object();

		for (var loop = 0; loop < containers.length; loop++) {
			var inputs = Array.prototype.slice.call(containers[loop].getElementsByTagName("INPUT"));
			var selects = Array.prototype.slice.call(containers[loop].getElementsByTagName("SELECT"));
			var ctrls = inputs.concat(selects);

			var item = Object();
			for (var innerLoop = 0; innerLoop < ctrls.length; innerLoop++) {

				var input = ctrls[innerLoop];
				item[input.name] = input.value;
			}

			if (isArray)
				json.push(item);
			else
				json = item;
		}
		return json;
	},

	Stringify: function (type, name, value) {
		switch (type) {
			case "json":
				return JSON.stringify(value);
			case "xml":
				return Utils.XMLStringify(name, value);
			default:
				throw "Content-Type:Type: " + inType + " not supported";
				break;
		}
	},
	XMLStringify: function (name, value) {
		var result;
		if (Object.prototype.toString.call(value) == "[object Array]") {
			result = "<ArrayOf" + name + ">";
			for (var loop = 0; loop < value.length; loop++)
				result += Utils.XMLStringify(name, value[loop]);
			result += "</ArrayOf" + name + ">";
		} else {
			result = "<" + name + ">";
			var keys = Object.keys(value);
			for (var loop = 0; loop < keys.length; loop++)
				result += "<" + keys[loop] + ">" + value[keys[loop]] + "</" + keys[loop] + ">";
			result += "</" + name + ">";
		}
		return result;
	},

	CreateElement: function (tagName) {
		var result = document.createElement(tagName);
		for (var i = 1; i < arguments.length; i++)
			result.setAttribute(arguments[i][0], arguments[i][1]);
		return result;
	},

	FormatTemplate: function (templateId, jso) {
		var fn = !/\W/.test(templateId) ?
			Utils.i._templateCache[templateId] = Utils.i._templateCache[templateId] || Utils.FormatTemplate(document.getElementById(templateId).innerHTML) :

			new Function("obj",
				"var p=[],print=function(){p.push.apply(p,arguments);};" +

				"with(obj){p.push('" +

				templateId
					.replace(/[\r\t\n]/g, " ")
					.split("<#").join("\t")
					.replace(/((^|#>)[^\t]*)'/g, "$1\r")
					.replace(/\t=(.*?)#>/g, "',$1,'")
					.split("\t").join("');")
					.split("#>").join("p.push('")
					.split("\r").join("\\'") + "');}return p.join('');");

		return jso ? fn(jso) : fn;
	}
}