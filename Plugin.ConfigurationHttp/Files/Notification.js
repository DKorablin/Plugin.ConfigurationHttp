(function(global){
	var hideTimer = null; // track active auto-hide timer

	function ensureElement(){
		var el = document.getElementById('popupError');
		if(!el){
			el = document.createElement('div');
			el.id = 'popupError';
			el.onclick = function(){ hide(); };
			document.body.appendChild(el);
		}
		return el;
	}

	function cancelTimer(){
		if(hideTimer){
			clearTimeout(hideTimer);
			hideTimer = null;
		}
	}

	function show(message, title){
		cancelTimer(); // prevent previous timer from hiding new message
		var el = ensureElement();
		el.innerHTML = '';
		if(title){
			var s = document.createElement('strong');
			s.textContent = title;
			el.appendChild(s);
		}
		var span = document.createElement('span');
		span.textContent = message || 'An error occurred.';
		el.appendChild(span);
		requestAnimationFrame(function(){
			el.classList.remove('hide');
			el.classList.add('show');
		});
	}

	function hide(){
		cancelTimer();
		var el = document.getElementById('popupError');
		if(!el) return;
		el.classList.remove('show');
		el.classList.add('hide');
		setTimeout(function(){ if(el.classList.contains('hide')) el.remove(); }, 500);
	}

	function showAuto(message, title, timeoutMs){
		show(message, title);
		if(timeoutMs){
			hideTimer = setTimeout(hide, timeoutMs);
		}
	}

	global.PopupError = { show:show, hide:hide, showAuto:showAuto };
})(window);
