const htmlEscapes = {
    '&': '&amp;',
    '<': '&lt;',
    '>': '&gt;',
    '"': '&quot;',
    "'": '&#39;'
};

function escapeHtml(value) {
    return `${value ?? ''}`.replace(/[&<>"']/g, char => htmlEscapes[char]);
}

function safeUrl(value) {
    const url = `${value ?? ''}`.trim();
    if (!url) return '#';

    try {
        const parsedUrl = new URL(url, window.location.origin);
        if (['http:', 'https:', 'mailto:', 'tel:'].includes(parsedUrl.protocol)) {
            return escapeHtml(url);
        }
    } catch (e) {
        return '#';
    }

    return '#';
}

export function renderWidgetContent(widget) {
    if (widget.widgetType === 'LinkList' && widget.contentCode) {
        try {
            const links = JSON.parse(widget.contentCode);
            if (!Array.isArray(links)) {
                return '<div class="text-muted small">Invalid link data</div>';
            }

            const sortedLinks = [...links].sort((a, b) => (a?.order ?? 0) - (b?.order ?? 0));

            return sortedLinks.map(link => {
                const target = link?.openInNewTab ? '_blank' : '_self';
                const rel = link?.openInNewTab ? 'noopener noreferrer' : '';
                const icon = link?.icon ? `<i class="${escapeHtml(link.icon)} me-1"></i>` : '';
                const externalIcon = link?.openInNewTab ? '<i class="bi-box-arrow-up-right ms-1 small"></i>' : '';

                return `<a href="${safeUrl(link?.url)}" target="${target}" rel="${rel}" class="d-block mb-2">${icon}${escapeHtml(link?.name)}${externalIcon}</a>`;
            }).join('');
        } catch (e) {
            return '<div class="text-muted small">Invalid link data</div>';
        }
    }
    if (widget.widgetType === 'ImageLink' && widget.contentCode) {
        try {
            const data = JSON.parse(widget.contentCode);
            if (!data || Array.isArray(data) || typeof data !== 'object') {
                return '<div class="text-muted small">Invalid image link data</div>';
            }

            const imgTag = `<img src="${safeUrl(data.imageUrl)}" class="${escapeHtml(data.cssClass)}" title="${escapeHtml(data.title)}" alt="${escapeHtml(data.altText)}" style="max-width:100%" />`;
            if (data.linkUrl) {
                const target = data.openInNewTab ? '_blank' : '_self';
                const rel = data.openInNewTab ? 'noopener noreferrer' : '';
                return `<a href="${safeUrl(data.linkUrl)}" target="${target}" rel="${rel}">${imgTag}</a>`;
            }
            return imgTag;
        } catch (e) {
            return '<div class="text-muted small">Invalid image link data</div>';
        }
    }
    if (widget.widgetType === 'ButtonLink' && widget.contentCode) {
        try {
            const buttons = JSON.parse(widget.contentCode);
            if (!Array.isArray(buttons)) {
                return '<div class="text-muted small">Invalid button link data</div>';
            }

            return '<div class="btn-group">' + buttons.map(btn => {
                const target = btn?.openInNewTab ? '_blank' : '_self';
                const rel = btn?.openInNewTab ? 'noopener noreferrer' : '';
                return `<a href="${safeUrl(btn?.url)}" target="${target}" rel="${rel}" class="btn ${escapeHtml(btn?.cssClass || 'btn-outline-primary')}">${escapeHtml(btn?.text)}</a>`;
            }).join('') + '</div>';
        } catch (e) {
            return '<div class="text-muted small">Invalid button link data</div>';
        }
    }
    return '';
}
