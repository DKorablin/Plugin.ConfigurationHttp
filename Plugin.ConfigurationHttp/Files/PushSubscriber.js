var PushPermission;
(function (PushPermission) {
	PushPermission[PushPermission["Undefined"] = 0] = "Undefined";
	PushPermission[PushPermission["Granted"] = 1] = "Granted";
	PushPermission[PushPermission["Denied"] = 2] = "Denied";
	PushPermission[PushPermission["Unsupported"] = 3] = "Unsupported";
})(PushPermission || (PushPermission = {}));

function PushEndpointPayload(subscription) {
	var rP256dh = subscription.getKey ? subscription.getKey("p256dh") : "";
	var rAuth = subscription.getKey ? subscription.getKey("auth") : "";
	this.Endpoint = subscription.endpoint;
	this.p256dh = rP256dh ? btoa(String.fromCharCode.apply(null, new Uint8Array(rP256dh))) : "";
	this.auth = rAuth ? btoa(String.fromCharCode.apply(null, new Uint8Array(rAuth))) : "";
}

// Создание замыкания, с указанием серверных параметров
// serviceWorkerUri - Ссылка на JS файла ServiceWorker'а
// subscriberUri - Ссылка на AJAX сервис подписки на PUSH'ы
// unsubscribeUri - Ссылка на AJAX сервис отписки на PUSH'ы
// auto - Автоподписка сразу при загрузке формы
function PushSubscriber(serviceWorkerUri, subscribeUri, unsubscribeUri, auto) {
	this.i._seviceWorkerUri = serviceWorkerUri;

	this.Async._subscribeUri = subscribeUri;
	this.Async._unsubscribeUri = unsubscribeUri;

	var _this = this;
	document.addEventListener("readystatechange", function () { if (document.readyState == "interactive") _this.OnReadyStateChanged(auto); }, false);
}

PushSubscriber.prototype = {
	i: {
		_isSubscribed: false,
		_seviceWorkerUri: null,

		RegisterWorker: function () {
			if (this._seviceWorkerUri == null) return;

			navigator.serviceWorker.register(this._seviceWorkerUri)
				.then(function (registration) {
					console.log("OK: i.RegisterWorker()");
				}).catch(function (exc) {
					console.error("Error: i.RegisterWorker()", exc);
				});
		}
	},

	Async: {
		_subscribeUri: null,
		_unsubscribeUri: null,

		SendPayload: function (isSubscribe, payload) {
			var uri = isSubscribe ? this._subscribeUri : this._unsubscribeUri;
			var params = isSubscribe ? { endpoint: payload.Endpoint, p256dh: payload.p256dh, auth: payload.auth } : null;
			var ajax = { Method: "POST", withCredentials: false, Url: uri, Params: params };
			this.LoadAsync(ajax);
		},

		Stringify: function (value) {
			var str = [];
			for (var p in value)
				if (value.hasOwnProperty(p))
					str.push(encodeURIComponent(p) + "=" + encodeURIComponent(value[p]));
			return str.join("&");
		},

		LoadAsync: function (ajax) {
			/// <summary>Асинхронная загрузка с сервера</summary>
			var _this = this;

			var request = new XMLHttpRequest();
			request.onreadystatechange = function () { _this.OnLoaded(request); };
			request.withCredentials = ajax.withCredentials;
			request.open(ajax.Method, ajax.Url, true);
			request.setRequestHeader("Content-Type", "application/json; charset=utf-8");
			request.send(this.Stringify(ajax.Params));
		},
		OnLoaded: function (objRequest) {
			if (objRequest.readyState != 4) return;
			if (objRequest.status == 204) return;
			else
				console.log("Error: Ajax.OnLoaded. Status: " + objRequest.status);
		}
	},

	IsPushSupported: function () {
		return ("serviceWorker" in navigator) && ("PushManager" in window) && ("showNotification" in ServiceWorkerRegistration.prototype);
	},

	IsPermissionGranted: function () {
		return this.GetCurrentPermission() == PushPermission.Granted;
	},

	CanSubscribe: function () {
		if (this.i._isSubscribed == true)
			return false;
		if (!this.IsPushSupported())
			return false;

		var p = this.GetCurrentPermission();
		switch (p) {
			case PushPermission.Undefined:
			case PushPermission.Granted:
				return true;
			default:
				return false;
		}
	},

	CanUnsubscribe: function () {
		if (!this.IsPushSupported())
			return false;
		var p = this.GetCurrentPermission();
		return p == PushPermission.Granted;
	},

	/// <summary>При загрузке формы предпринимаем попытку зарегистрировать ServiceWorker</summary>
	OnReadyStateChanged: function (auto) {
		if (auto)
			this.Subscribe();
		else
			this.TryRegisterWorker();
	},

	TryRegisterWorker: function () {
		if (this.IsPermissionGranted())
			this.i.RegisterWorker();
		else
			console.log("NoAccess: TryRegisterWorker()");
	},

	TrySubscribe: function () {
		if (this.IsPermissionGranted())
			this.Subscribe();
		else
			console.log("NoAccess: TrySubscribe()");
	},

	GetCurrentPermission: function () {
		var permission = null;
		if (this.IsPushSupported())
			permission = Notification.permission
				? Notification.permission
				: (new Notification("check")).permission;

		switch (permission) {
			case "granted":
				return PushPermission.Granted;
			case "denied":
				return PushPermission.Denied;
			case "default":
				return PushPermission.Undefined;
			default:
				return PushPermission.Unsupported;
		}
	},

	ForwardPayloadI: function (isSubscribe, payload) {
		this.Async.SendPayload(isSubscribe, payload);
	},

	Subscribe: function () {
		var _this = this;
		if (!_this.CanSubscribe())
			return;

		this.i.RegisterWorker();
		navigator.serviceWorker.ready.then(function (swRegistration) {
			swRegistration.pushManager.subscribe({ userVisibleOnly: true })
				.then(function (subscription) {
					if (subscription) {
						var payload = new PushEndpointPayload(subscription);
						_this.ForwardPayloadI(true, payload);
						console.log("OK: Subscribe()");
						_this.i._isSubscribed = true;
					}
				}).catch(function (err) {
					console.log("Error: Subscribe()", err);
				});
		});
	},

	Unsubscribe: function () {
		var _this = this;
		if (!_this.CanUnsubscribe())
			return;

		navigator.serviceWorker.ready.then(function (swRegistration) {
			swRegistration.pushManager.getSubscription()
				.then(function (subscription) {
					if (!subscription)
						return;

					var payload = new PushEndpointPayload(subscription);
					subscription.unsubscribe()
						.then(function () {
							_this.ForwardPayloadI(false, payload);
							console.log("OK: Unsubscribe()");
							_this.i._isSubscribed = false;
						}).catch(function (err) {
							console.log("Error: Unsubscribe()", err);
						});
				}).catch(function (err) {
					console.log("Error: Unsubscribe() -> getSubscription()", err);
				});
		});
	}
}