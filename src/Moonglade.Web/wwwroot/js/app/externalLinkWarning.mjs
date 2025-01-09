const externalLinkModal = new bootstrap.Modal(document.getElementById('externalLinkModal'));
const modalUrlElement = document.getElementById('extlink-url');
const modalContinueButton = document.getElementById('extlink-continue');

function isExternalLink(link) {
    return !link.href.startsWith('mailto:') && link.hostname !== location.hostname;
}

function markExternalLinks() {
    const links = document.querySelectorAll('.post-content a');
    links.forEach(link => {
        if (isExternalLink(link)) {
            link.classList.add('external');
        }
    });
}

function bindExternalLinkEvents() {
    document.querySelectorAll('a.external').forEach(link => {
        link.addEventListener('click', handleExternalLinkClick);
    });
}

function handleExternalLinkClick(event) {
    event.preventDefault();
    const linkHref = event.currentTarget.getAttribute('href');
    modalUrlElement.textContent = linkHref;
    modalContinueButton.href = linkHref;
    externalLinkModal.show();
}

function bindModalContinueEvent() {
    modalContinueButton.addEventListener('click', () => {
        externalLinkModal.hide();
    });
}

function init() {
    markExternalLinks();
    bindExternalLinkEvents();
    bindModalContinueEvent();
}

init();
