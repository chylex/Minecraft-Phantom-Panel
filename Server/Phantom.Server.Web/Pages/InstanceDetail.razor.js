const log = document.getElementById("log");
let shouldAutoScroll = false;
let isAutoScrolling = false;

if (log) {
	shouldAutoScroll = true;
	log.addEventListener("scroll", function() {
		if (!isAutoScrolling) {
			shouldAutoScroll = log.scrollHeight - log.scrollTop - log.clientHeight < 5;
		}
	});
}

// noinspection JSUnusedGlobalSymbols
export function scrollLog() {
	if (shouldAutoScroll) {
		isAutoScrolling = true;
		log.scrollTop = log.scrollHeight;
		isAutoScrolling = false;
	}
}
