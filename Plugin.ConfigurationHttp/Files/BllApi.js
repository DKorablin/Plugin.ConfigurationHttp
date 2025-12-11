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
			if (data != null) {
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

		// Helper: detect plain object
		_isPlainObject: function(o){ return o && Object.prototype.toString.call(o)==='[object Object]'; },

		// Recursively build complex object view (read-only leafs if parent is read-only or unknown type)
		_buildComplexObject: function(rootItem, value, evtPropertyChanged, readOnly){
			var wrapper = document.createElement('DIV');
			wrapper.className = 'complex';
			for(var key in value){
				if(!value.hasOwnProperty(key)) continue;
				var row = document.createElement('DIV');
				row.className = 'complex-row';
				var lbl = document.createElement('SPAN');
				lbl.className = 'complex-key';
				lbl.innerHTML = key + ':'; 
				row.appendChild(lbl);
				var val = value[key];
				var ctrl;
				if(this._isPlainObject(val)){
					ctrl = this._buildComplexObject(rootItem, val, evtPropertyChanged, true); // nested objects read-only for now
				}else if(Array.isArray(val)){
					ctrl = document.createElement('DIV');
					ctrl.className='complex-array';
					for(var i=0;i<val.length;i++){
						var aItem = val[i];
						var aRow = document.createElement('DIV');
						aRow.className='complex-array-item';
						if(this._isPlainObject(aItem))
							aRow.appendChild(this._buildComplexObject(rootItem, aItem, evtPropertyChanged, true));
						else{
							var pre = document.createElement('PRE');
							pre.innerHTML = aItem === null ? 'null' : aItem.toString();
							aRow.appendChild(pre);
						}
						ctrl.appendChild(aRow);
					}
				}else{
					// primitive
					if(typeof val === 'boolean'){
						ctrl = document.createElement('INPUT');
						ctrl.type='checkbox';
						ctrl.checked = val;
						ctrl.disabled = true; // read-only for nested editing scope
					}else if(typeof val === 'number'){
						ctrl = document.createElement('INPUT');
						ctrl.type='number';
						ctrl.value = val;
						ctrl.readOnly = true;
					}else{
						ctrl = document.createElement('INPUT');
						ctrl.type='text';
						ctrl.value = val === null ? 'null' : val.toString();
						ctrl.readOnly = true;
					}
				}
				row.appendChild(ctrl);
				wrapper.appendChild(row);
			}
			return wrapper;
		},

		CreateSettingsCtrl: function (item, evtPropertyChanged) {
			var result;
			// Complex object rendering (non-null object Value, not primitive, not handled types)
			if(item && item.Value && typeof item.Value === 'object' && !Array.isArray(item.Value) && [
				'System.Boolean','System.SByte','System.UInt16','System.Int16','System.UInt32','System.Int32','System.UInt64','System.Int64','System.TimeSpan'
			].indexOf(item.Type) === -1){
				result = document.createElement('DIV');
				result.className='complex-root';
				var header = document.createElement('DIV');
				header.className='complex-header';
				header.innerHTML = '{object}';
				var content = this._buildComplexObject(item, item.Value, evtPropertyChanged, !item.CanWrite);
				content.style.display = 'none'; // hidden by default
				header.onclick = function(){
					content.style.display = content.style.display==='none' ? '' : 'none';
					this.className = content.style.display==='none' ? 'complex-header collapsed' : 'complex-header expanded';
				};
				result.appendChild(header);
				result.appendChild(content);
				result.data = item;
				return result; // read-only tree (editing nested not supported yet)
			}

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
				case "System.TimeSpan":
					result = Utils.CreateElement("INPUT", ["type", "text"], ["placeholder", item.DefaultValue ? item.DefaultValue : "dd.hh:mm:ss"]);
					result.title = "Format: d.hh:mm:ss[.fffffff]";
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
				case "System.TimeSpan":
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
			else if (e.data.Type === "System.TimeSpan") {
				if (value && !/^\d*\.??\d{0,2}:?\d{1,2}:\d{1,2}(\.\d+)?$/.test(value) && !/^\d{1,2}:\d{1,2}:\d{1,2}(\.\d+)?$/.test(value))
					return;
			}
			else if (e.data.Value != null && e.data.Value.toString() == value)
				return;

			var fValue = value.replace(/\n/g, "\r\n");
			if (e.data.Value != fValue)
				callback();
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
	},

	// Retreiving VAPID public key for web push subscription from server
	GetVapidPublicKey: function (callback) {
		var pluginId = "d10da6bc-77fd-4ada-8b3f-b850023e59ae";
		this.GetPluginParams(null, pluginId, function (payload) {
			var vapidKey = null;
			if (payload) {
				for (var i = 0; i < payload.length; i++) {
					var items = payload[i].Items || [];
					for (var j = 0; j < items.length; j++) {
						if (items[j].Name === "VapidPublicKey") { vapidKey = items[j].Value; break; }
					}
					if (vapidKey) break;
				}
			}
			if (callback) callback(vapidKey);
		}, function(){}); // pass noop to avoid null callback usage in blur handler
	}
}