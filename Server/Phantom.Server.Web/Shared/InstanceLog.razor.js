// noinspection JSUnusedGlobalSymbols

let log;
let shouldAutoScroll = false;
let isAutoScrolling = false;

export function initLog() {
	log = document.getElementById("log");
	
	if (log) {
		shouldAutoScroll = true;
		log.scrollTop = log.scrollHeight;
		log.addEventListener("scroll", function() {
			if (isAutoScrolling) {
				isAutoScrolling = false;
			}
			else {
				setTimeout(function() {
					shouldAutoScroll = log.scrollHeight - log.scrollTop - log.clientHeight < 5;
				}, 10);
			}
		});
	}
	else {
		console.error("Missing log element.");
	}
}

export function scrollLog() {
	if (shouldAutoScroll) {
		isAutoScrolling = true;
		log.scrollTop = log.scrollHeight;
	}
}
