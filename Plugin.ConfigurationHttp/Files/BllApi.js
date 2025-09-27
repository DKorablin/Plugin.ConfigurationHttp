/// <reference path="/Files/Utils.js" />

function BllApi(requestLogId, responseLogId) {
	this.i.RequestLogId = requestLogId;
	this.i.ResponseLogId = responseLogId;
}

BllApi.prototype = {
	i: {
		RequestLogId: null,
		ResponseLogId: null,
		PluginsCache:[],

		SetRequestLog: function (request) {
			this.RequestLogId = document.getElementById(this.RequestLogId);
			if (this.RequestLogId) {
				this.SetRequestLog = (request) => this.RequestLogId.value = request;
				this.SetRequestLog(request);
			}
		},

		SetResponseLog: function (response) {
			this.ResponseLogId = document.getElementById(this.ResponseLogId);
			if (this.ResponseLogId) {
				this.SetResponseLog = (response) => this.ResponseLogId.value = response;
				this.SetResponseLog(response);
			}
		},

		OnRawResponseI: function (data, callback) {
			this.SetResponseLog(data);

			var payload = null;
			if (data != "") {
				payload = JSON.parse(data);
				if (payload.responseText) {
					alert(payload.responseText);
					return null;
				}
			}

			if (callback != null)
				callback(payload);
			return payload;
		},

		OnGetPluginsI:function(instanceId,data,callback) {
			var payload = this.OnRawResponseI(data);
			if (payload != null) {
				this.PluginsCache[instanceId] = payload;

				for (var i = 0; i < payload.length; i++)
					payload[i].InstanceId = instanceId;
			}

			if (callback != null)
				callback(payload);
		},

		OnGetSettingsI: function (instanceId, pluginId, data, callback, evtPropertyChanged) {
			var payload = this.OnRawResponseI(data);
			if (payload != null)
				for (var i = 0; i < payload.length; i++)
					for (var s = 0; s < payload[i].Items.length; s++) {
						var item = payload[i].Items[s];
						item.InstanceId = instanceId;
						item.PluginId = pluginId;
						item.ValueCtrl = this.CreateSettingsCtrl(item, evtPropertyChanged);
					}

			if (callback != null)
				callback(payload);
		},

		CreateSettingsCtrl: function (item, evtPropertyChanged) {
			var result;
			if (!item.CanWrite) {
				result = document.createElement("PRE");
				result.innerHTML = item.Value;
				return result;
			}

			switch (item.Type) {
				case "System.Boolean":
					result = document.createElement("SELECT");
					var oTrue = document.createElement("OPTION");
					oTrue.value = oTrue.innerHTML = "true";
					var oFalse = document.createElement("OPTION");
					oFalse.value = oFalse.innerHTML = "false";
					result.appendChild(oTrue);
					result.appendChild(oFalse);
					break;
				case "System.SByte":
					result = Utils.CreateElement("INPUT", ["type", "number"], ["min", "-128"], ["max", "127"]);
					break;
				case "System.UInt16":
					result = Utils.CreateElement("INPUT", ["type", "number"], ["min", "0"], ["max", "65535"]);
					break;
				case "System.Int16":
					result = Utils.CreateElement("INPUT", ["type", "number"], ["min", "0"], ["max", "4294967295"]);
					break;
				case "System.UInt32":
					result = Utils.CreateElement("INPUT", ["type", "number"], ["min", "0"], ["max", "4294967295"]);
					break;
				case "System.Int32":
					result = Utils.CreateElement("INPUT", ["type", "number"], ["min", "-2147483648"], ["max", "2147483647"]);
					break;
				case "System.UInt64":
					result = Utils.CreateElement("INPUT", ["type", "number"], ["min", "0"], ["max", "18446744073709551615"]);
					break;
				case "System.Int64":
					result = Utils.CreateElement("INPUT", ["type", "number"], ["min", "-9223372036854775808"], ["max", "9223372036854775807"]);
					break;
				default:
					result = item.Editors && Array.isArray(item.Editors) && item.Editors.some(editor => editor.indexOf("System.ComponentModel.Design.MultilineStringEditor")-1)
						? document.createElement("TEXTAREA")
						: Utils.CreateElement("INPUT", ["type", item.IsPassword ? "password" : "text"]);
					break;
			}

			this.SetPropertyValue(result, item);
			if (item.IsReadOnly)
				result.setAttribute("readonly", "readonly");
			if (item.DefaultValue)
				result.setAttribute("placeholder", item.DefaultValue);
			result.data = item;

			var _this = this;
			result.onkeydown = (e) => _this.OnPropertyCancel(e);
			result.onblur = (e) => _this.OnPropertyBlur(evtPropertyChanged, e);
			return result;
		},

		OnPropertyCancel:function(event){
			if (event.keyCode != 27) return;
			this.SetPropertyValue(event.target, event.target.data);
		},

		SetPropertyValue:function(ctrl, item){
			switch (item.Type) {
				case "System.Boolean":
					ctrl.selectedIndex = item.Value == false ? 1 : 0;
					break;
				case "System.SByte":
				case "System.UInt16":
				case "System.Int16":
				case "System.UInt32":
				case "System.Int32":
				case "System.UInt64":
				case "System.Int64":
					ctrl.value = item.Value;
					break;
				default:
					ctrl.value = item.Value;
					break;
			}

			ctrl.className = item.DefaultValue != item.Value
				? "C"
				: "";
		},

		OnPropertyBlur: function (callback, event) {
			var e = event.target;
			var value = e.value;
			if (e.data.Value == null && value == "null")
				return;
			else if (e.data.Value != null && e.data.Value.toString() == value)
				return;//Проверка Boolean
			else {
				var fValue = value.replace(/\n/g, "\r\n");
				if (e.data.Value != fValue)
					callback();
			}
		}
	},

	GetInstances: function (callback) {
		var _this = this;

		var ajax = { Method: "GET", Url: "/Instance", };
		var request = Utils.Async.LoadAsync(ajax, (data) => _this.i.OnRawResponseI(data, callback));
	},

	GetPlugins: function (instanceId, searchText, callback) {
		if (this.i.PluginsCache[instanceId] && callback != null)
			callback(this.i.PluginsCache[instanceId]);

		var _this = this;

		var ajax = instanceId
			? { Method: "GET", Url: "/Plugins/?searchText="+searchText+"&instanceId=" + instanceId, }
			: { Method: "GET", Url: "/Plugins/?searchText="+searchText, };
		var request = Utils.Async.LoadAsync(ajax, (data) => _this.i.OnGetPluginsI(instanceId, data, callback));

		this.i.SetRequestLog(request);
	},

	GetPluginParams: function (instanceId, pluginId, callback, evtPropertyChanged) {
		var _this = this;

		var ajax = instanceId
			? { Method: "GET", Url: "/PluginParams/?instanceId=" + instanceId + "&pluginId=" + pluginId, }
			: { Method: "GET", Url: "/PluginParams/?pluginId=" + pluginId, };
		var request = Utils.Async.LoadAsync(ajax, (data) => _this.i.OnGetSettingsI(instanceId, pluginId, data, callback, evtPropertyChanged));

		this.i.SetRequestLog(request);
	},

	SetPluginParams: function (instanceId, pluginId, paramName, value, callback) {
		var _this = this;

		var ajax = instanceId
			? { Method: "POST", Url: "/SetPluginParams/?instanceId=" + instanceId + "&pluginId=" + pluginId + "&paramName=" + paramName, Params: "value=" + value, }
			: { Method: "POST", Url: "/SetPluginParams/?pluginId=" + pluginId + "&paramName=" + paramName, Params: "value=" + value, };
		var request = Utils.Async.LoadAsync(ajax, (data) => _this.i.OnRawResponseI(data, callback));

		this.i.SetRequestLog(request);
	}
}