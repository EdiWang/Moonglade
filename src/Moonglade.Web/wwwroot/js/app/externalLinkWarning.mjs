export function warnExtLink() {
    function isExternalLink(link) {
        return !link.href.match(/^mailto:/) && (link.hostname !== location.hostname);
    }

    const links = document.querySelectorAll('.post-content a');
    links.forEach(link => {
        if (isExternalLink(link)) {
            link.classList.add('external');
        }
    });

    const externalLinkModal = new bootstrap.Modal(document.getElementById('externalLinkModal'));

    document.querySelectorAll('a.external').forEach(link => {
        link.addEventListener('click', function (e) {
            e.preventDefault();
            const linkHref = this.getAttribute('href');
            document.getElementById('extlink-url').innerHTML = linkHref;
            document.getElementById('extlink-continue').href = linkHref;
            externalLinkModal.show();
        });
    });
    document.getElementById('extlink-continue').addEventListener('click', function () {
        externalLinkModal.hide();
    });
}

warnExtLink();