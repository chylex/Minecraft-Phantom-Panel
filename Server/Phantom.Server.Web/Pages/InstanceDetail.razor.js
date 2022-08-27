// noinspection JSUnusedGlobalSymbols

let log;
let shouldAutoScroll = false;
let isAutoScrolling = false;

export function initLog() {
	log = document.getElementById("log");
	
	if (log) {
		shouldAutoScroll = true;
		log.addEventListener("scroll", function() {
			if (isAutoScrolling) {
				isAutoScrolling = false;
			}
			else {
				setTimeout(function() {
					shouldAutoScroll = log.scrollHeight - log.scrollTop - log.clientHeight < 5;
				}, 20);
			}
		});
	}
	else {
		console.error("Missing log element.");
	}
	
}

export function scrollLog() {
	if (shouldAutoScroll) {
		setTimeout(function() {
			isAutoScrolling = true;
			log.scrollTop = log.scrollHeight;
		}, 20);
	}
}
