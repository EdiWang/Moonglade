const externalLinkModalElement = document.getElementById('externalLinkModal');
const externalLinkModal = new bootstrap.Modal(externalLinkModalElement);
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
    const selectors = [];
    if (externalLinkModalElement.dataset.warnPostLinks === 'true') {
        selectors.push('.post-content a');
    }
    if (externalLinkModalElement.dataset.warnCommentLinks === 'true') {
        selectors.push('.comment-list a');
    }
    if (selectors.length === 0) {
        return;
    }

    const links = document.querySelectorAll(selectors.join(', '));
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
