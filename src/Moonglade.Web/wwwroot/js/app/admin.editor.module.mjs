import { fetch2 } from './httpService.mjs?v=1500'
import { getLocalizedString, slugify } from './utils.module.mjs';

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

export async function initEvents(slugifyTitle) {

    if (slugifyTitle) {
        document.querySelector('#ViewModel_Title').addEventListener('change', function () {
            var newSlug = slugify(this.value);
            if (newSlug) {
                document.querySelector('#ViewModel_Slug').value = newSlug;
            }
        });
    }

    document.querySelector('#btn-preview')?.addEventListener('click', function (e) {
        submitForm(e);
    });

    document.querySelector('#btn-save').addEventListener('click', function (e) {
        submitForm(e);
    });

    document.querySelector('#btn-publish')?.addEventListener('click', function (e) {
        document.querySelector('input[name="ViewModel.PostStatus"]').value = 'published';
        submitForm(e);
    });

    document.querySelector('.btn-modify-slug')?.addEventListener('click', function () {
        var message = getLocalizedString('modifySlugWarning');

        if (confirm(message)) {
            var slugInput = document.getElementById('ViewModel_Slug');
            slugInput.removeAttribute('readonly');
            slugInput.focus();
            document.querySelector('.btn-modify-slug').style.display = 'none';
        }
    });

    function submitForm(e) {
        if (window.htmlContentEditor) {
            window.htmlContentEditor.syncToTextarea();
        }

        if (window.mdContentEditor) {
            assignEditorValues(window.mdContentEditor, ".post-content-textarea");
        }

        if (document.querySelector('input[name="ViewModel.PostStatus"]').value === 'published') {
            const btnPublish = document.querySelector('#btn-publish');
            if (btnPublish) {
                btnPublish.style.display = 'none';
            }

            const btnPreview = document.querySelector('#btn-preview');
            if (btnPreview) {
                btnPreview.style.display = 'none';
            }
        }
    }

    const data = await fetch2('/api/tags/names', 'GET', {});
    const input = document.querySelector('#ViewModel_Tags');
    const tagify = new Tagify(input,
        {
            pattern: /^[a-zA-Z 0-9\.\-\+\#\s]*$/i,
            whitelist: data,
            originalInputValueFormat: valuesArr => valuesArr.map(item => item.value).join(','),
            maxTags: 10,
            dropdown: {
                maxItems: 30,
                classname: 'tags-dropdown',
                enabled: 0,
                closeOnSelect: false
            }
        });

    document.querySelector('#ViewModel_Title').focus();
}

export function warnDirtyForm(selector) {
    const form = document.querySelector(selector);
    let isFormDirty = false;

    form.addEventListener('input', function () {
        isFormDirty = true;
    });

    window.addEventListener('beforeunload', function (event) {
        if (isFormDirty) {
            const message = getLocalizedString('unsavedChanges');
            event.returnValue = message;
            return message;
        }
    });

    form.addEventListener('submit', function () {
        isFormDirty = false;
    });
}

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
