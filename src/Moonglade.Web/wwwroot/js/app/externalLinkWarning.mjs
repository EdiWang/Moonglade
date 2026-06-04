const externalLinkModal = new bootstrap.Modal(document.getElementById('externalLinkModal'));
const modalUrlElement = document.getElementById('extlink-url');
const modalContinueButton = document.getElementById('extlink-continue');

function getExternalLinkUrl(link) {
    const href = link.getAttribute('href');
    if (!href) {
        return null;
    }

    try {
        const url = new URL(href, location.href);
        const isWarnableProtocol = url.protocol === 'http:' || url.protocol === 'https:';
        return isWarnableProtocol && url.hostname !== location.hostname ? href : null;
    } catch {
        return null;
    }
}

function markExternalLinks() {
    const links = document.querySelectorAll('.post-content a, .comment-list a');
    links.forEach(link => {
        if (getExternalLinkUrl(link)) {
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
    const linkHref = getExternalLinkUrl(event.currentTarget);
    if (!linkHref) {
        return;
    }

    event.preventDefault();
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
