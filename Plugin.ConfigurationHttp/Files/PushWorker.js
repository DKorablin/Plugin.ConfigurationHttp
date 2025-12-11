self.addEventListener("push", function (event) {
	console.log("Push message received", event);
	var payload = event.data ? event.data.text() : "undefined";
	let data = JSON.parse(payload);

	var title = "SAL.Flatbed: "+data.Title;
	var icon = "/favicon.ico";
	var tag = "push-notification-tag";
	event.waitUntil(self.registration.showNotification(title, {
		body: payload,
		icon: icon,
		tag: tag,
	}));
});
self.addEventListener("notificationclick", function (event) {
	console.log("Push message clicked", event);
	event.notification.close();
});