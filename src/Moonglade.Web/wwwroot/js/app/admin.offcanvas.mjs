export function createOffcanvas(element) {
    let returnTarget;

    element.addEventListener('show.bs.offcanvas', event => {
        const trigger = event.relatedTarget ?? element.ownerDocument.activeElement;
        returnTarget = trigger?.closest('.dropdown')?.querySelector('[data-bs-toggle="dropdown"]') ?? trigger;
    });

    element.addEventListener('hidden.bs.offcanvas', () => {
        const trigger = returnTarget;
        returnTarget = null;
        if (trigger?.isConnected && !trigger.disabled && trigger.getClientRects().length) {
            trigger.focus();
        }
    });

    return new bootstrap.Offcanvas(element);
}
