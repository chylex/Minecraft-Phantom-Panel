// noinspection JSUnusedGlobalSymbols

function showModal(id) {
	bootstrap.Modal.getOrCreateInstance(document.getElementById(id)).show();
}

function closeModal(id) {
	bootstrap.Modal.getInstance(document.getElementById(id)).hide();
}

/**
 * @param {HTMLButtonElement} button
 */
async function copyToClipboard(button) {
	if (button.getAttribute("data-clipboard-copying") !== null) {
		return;
	}
	
	button.setAttribute("data-clipboard-copying", "");
	try {
		const toCopy = button.getAttribute("data-clipboard");
		
		const originalText = button.textContent;
		const originalMinWidth = button.style.minWidth;
		
		try {
			await navigator.clipboard.writeText(toCopy);
		} catch (e) {
			console.error(e);
			alert("Could not copy to clipboard.");
			return;
		}
		
		button.style.minWidth = button.offsetWidth + "px";
		button.textContent = "Copied!";
		
		await new Promise(resolve => setTimeout(resolve, 2000));
		
		button.textContent = originalText;
		button.style.minWidth = originalMinWidth;
	} finally {
		button.removeAttribute("data-clipboard-copying");
	}
}
