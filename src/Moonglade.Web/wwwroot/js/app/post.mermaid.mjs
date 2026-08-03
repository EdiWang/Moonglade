const mermaidCodeBlockSelector = [
    '.post-content pre code.language-mermaid',
    '.post-content pre code.lang-mermaid'
].join(',');

function getMermaidTheme() {
    return document.documentElement.getAttribute('data-bs-theme') === 'dark'
        ? 'dark'
        : 'default';
}

export async function renderMermaid() {
    const mermaid = globalThis.mermaid;
    if (!mermaid) {
        return;
    }

    const codeBlocks = document.querySelectorAll(mermaidCodeBlockSelector);
    if (!codeBlocks.length) {
        return;
    }

    try {
        mermaid.initialize({
            startOnLoad: false,
            securityLevel: 'strict',
            theme: getMermaidTheme()
        });
    } catch (err) {
        console.error(err);
        return;
    }

    const nodes = [];
    codeBlocks.forEach((block) => {
        const pre = block.closest('pre');
        if (!pre) {
            return;
        }

        pre.classList.add('mermaid');
        pre.classList.remove('language-mermaid', 'lang-mermaid');
        pre.textContent = block.textContent.trim();
        nodes.push(pre);
    });

    try {
        await mermaid.run({ nodes });
    } catch (err) {
        console.error(err);
    }
}
