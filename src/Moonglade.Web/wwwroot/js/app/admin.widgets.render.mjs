export function renderWidgetContent(widget) {
    if (widget.widgetType === 'LinkList' && widget.contentCode) {
        try {
            const links = JSON.parse(widget.contentCode);
            const sortedLinks = links.sort((a, b) => a.order - b.order);

            return sortedLinks.map(link => {
                const target = link.openInNewTab ? '_blank' : '_self';
                const icon = link.icon ? `<i class="${link.icon} me-1"></i>` : '';
                const externalIcon = link.openInNewTab ? '<i class="bi-box-arrow-up-right ms-1 small"></i>' : '';

                return `<a href="${link.url}" target="${target}" class="d-block mb-2">${icon}${link.name}${externalIcon}</a>`;
            }).join('');
        } catch (e) {
            return '<div class="text-muted small">Invalid link data</div>';
        }
    }
    if (widget.widgetType === 'ImageLink' && widget.contentCode) {
        try {
            const data = JSON.parse(widget.contentCode);
            const imgTag = `<img src="${data.imageUrl}" class="${data.cssClass || ''}" title="${data.title || ''}" alt="${data.altText || ''}" style="max-width:100%" />`;
            if (data.linkUrl) {
                const target = data.openInNewTab ? '_blank' : '_self';
                const rel = data.openInNewTab ? 'noopener noreferrer' : '';
                return `<a href="${data.linkUrl}" target="${target}" rel="${rel}">${imgTag}</a>`;
            }
            return imgTag;
        } catch (e) {
            return '<div class="text-muted small">Invalid image link data</div>';
        }
    }
    if (widget.widgetType === 'ButtonLink' && widget.contentCode) {
        try {
            const buttons = JSON.parse(widget.contentCode);
            return '<div class="btn-group">' + buttons.map(btn => {
                const target = btn.openInNewTab ? '_blank' : '_self';
                const rel = btn.openInNewTab ? 'noopener noreferrer' : '';
                return `<a href="${btn.url}" target="${target}" rel="${rel}" class="btn ${btn.cssClass || 'btn-outline-primary'}">${btn.text}</a>`;
            }).join('') + '</div>';
        } catch (e) {
            return '<div class="text-muted small">Invalid button link data</div>';
        }
    }
    return '';
}
