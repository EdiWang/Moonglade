import bicep from '../3rd/highlight.bicep.js'

export function renderCodeHighlighter() {
    const pres = document.querySelectorAll('pre');
    pres.forEach(pre => {
        // Find <pre> that doesn't have a <code> inside it.
        if (!pre.querySelector('code')) {
            const code = document.createElement('code');
            while (pre.firstChild) {
                code.appendChild(pre.firstChild);
            }
            pre.appendChild(code);
        }

        // For code that can't be automatically detected, fall back to use XML
        if (pre.classList.contains('language-markup')) {
            pre.querySelector('code').classList.add('lang-xml');
        }
    });

    hljs.registerLanguage('bicep', bicep);

    const codeBlocks = document.querySelectorAll('pre code');
    codeBlocks.forEach(block => {
        hljs.highlightElement(block);
    });
}

export function renderLaTeX(selector) {
    const codeBlocks = document.querySelectorAll(selector);
    codeBlocks.forEach(block => {
        const latex = block.textContent.trim();
        const container = document.createElement('div');
        try {
            katex.render(latex, container, { output: 'mathml' });
            block.parentNode.replaceWith(container);
        } catch (error) {
            console.error(error);
        }
    });
}