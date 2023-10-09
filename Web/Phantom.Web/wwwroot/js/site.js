// noinspection JSUnusedGlobalSymbols

function showModal(id) {
	bootstrap.Modal.getOrCreateInstance(document.getElementById(id)).show();
}

function closeModal(id) {
	bootstrap.Modal.getInstance(document.getElementById(id)).hide();
}
