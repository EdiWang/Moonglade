export const codeSampleLanguages = [
    { text: 'Bash', value: 'bash' },
    { text: 'Bicep', value: 'bicep' },
    { text: 'C#', value: 'csharp' },
    { text: 'C', value: 'c' },
    { text: 'C++', value: 'cpp' },
    { text: 'CSS', value: 'css' },
    { text: 'Dockerfile', value: 'dockerfile' },
    { text: 'Go', value: 'go' },
    { text: 'GraphQL', value: 'graphql' },
    { text: 'HTML/XML', value: 'xml' },
    { text: 'JavaScript', value: 'javascript' },
    { text: 'Json', value: 'json' },
    { text: 'Kotlin', value: 'kotlin' },
    { text: 'Kusto', value: 'kusto' },
    { text: 'LaTeX', value: 'latex' },
    { text: 'Lua', value: 'lua' },
    { text: 'Markdown', value: 'markdown' },
    { text: 'Mermaid', value: 'mermaid' },
    { text: 'Nginx', value: 'nginx' },
    { text: 'PowerShell', value: 'powershell' },
    { text: 'Plain Text', value: 'plaintext' },
    { text: 'Puppet', value: 'puppet' },
    { text: 'Python', value: 'python' },
    { text: 'R', value: 'r' },
    { text: 'Rust', value: 'rust' },
    { text: 'SCSS', value: 'scss' },
    { text: 'Shell', value: 'shell' },
    { text: 'SQL', value: 'sql' },
    { text: 'Swift', value: 'swift' },
    { text: 'TypeScript', value: 'typescript' },
    { text: 'WASM', value: 'wasm' },
    { text: 'YAML', value: 'yaml' }
];

export function keepAlive() {
    const tid = setInterval(postNonce, 60 * 1000);
    function postNonce() {
        const num = Math.random();
        fetch('/api/post/keep-alive',
            {
                method: 'POST',
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json'
                },
                credentials: 'include',
                body: JSON.stringify({ nonce: num })
            }).then(async (response) => {
                console.info('live');
            });
    }
}
