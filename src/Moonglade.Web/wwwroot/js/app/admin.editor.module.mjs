import { fetch2 } from './httpService.mjs?v=1426'
import { getPreferredTheme } from './themeService.mjs';

function slugify(text) {
    if (!/^[A-Za-z][A-Za-z0-9 \(\)#,\.\?]*$/.test(text)) {
        return '';
    }

    return text
        .toLowerCase()
        .replace(/[()#,.?]/g, '')
        .replace(/[^\w ]+/g, '')
        .replace(/ +/g, '-');
}

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
        var message = 'This post was published for a period of time, changing slug will result in breaking SEO, would you like to continue?';

        if (confirm(message)) {
            var slugInput = document.getElementById('ViewModel_Slug');
            slugInput.removeAttribute('readonly');
            slugInput.focus();
            document.querySelector('.btn-modify-slug').style.display = 'none';
        }
    });

    function submitForm(e) {
        if (window.tinyMCE) {
            window.tinyMCE.triggerSave();
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
            const message = 'You have unsaved changes, are you sure to leave this page?';
            event.returnValue = message;
            return message;
        }
    });

    form.addEventListener('submit', function () {
        isFormDirty = false;
    });
}

export function loadTinyMCE(textareaSelector) {
    if (typeof window.tinyMCE === 'undefined') {
        console.error('TinyMCE is not loaded.');
        return;
    }

    const preferredTheme = getPreferredTheme();
    const isDarkTheme = preferredTheme === 'dark';

    window.tinyMCE.init({
        license_key: 'gpl', // https://www.tiny.cloud/docs/tinymce/latest/license-key/
        selector: textareaSelector,
        themes: 'silver',
        skin: isDarkTheme ? 'oxide-dark' : 'oxide',
        height: 'calc(100vh - 400px)',
        relative_urls: false,
        browser_spellcheck: true,
        branding: false,
        promotion: false,
        block_formats: 'Paragraph=p; Header 2=h2; Header 3=h3; Header 4=h4; Preformatted=pre',
        plugins: 'advlist autolink autosave link image lists charmap preview anchor pagebreak searchreplace wordcount visualblocks visualchars code fullscreen insertdatetime media nonbreaking save table directionality codesample emoticons',
        toolbar: 'undo redo | blocks | bold italic underline strikethrough | forecolor backcolor | paste pastetext removeformat | hr link image codesample | charmap emoticons table media | alignleft aligncenter alignright alignjustify | bullist numlist outdent indent | code | fullscreen',
        save_onsavecallback: () => {
            document.querySelector('#btn-save').click();
        },
        paste_data_images: true,
        images_file_types: 'png,jpg,jpeg,gif,webp,svg',
        images_upload_url: '/image',
        images_upload_credentials: true,
        extended_valid_elements: 'img[class|src|border=0|alt|title|hspace|vspace|width|height|align|onmouseover|onmouseout|name|loading=lazy]',
        body_class: 'post-content',
        content_css: isDarkTheme ? '/css/tinymce-custom-dark.css' : '/css/tinymce-custom.css',
        codesample_languages: [
            { text: 'Bash', value: 'bash' },
            { text: 'Bicep', value: 'bicep' },
            { text: 'C#', value: 'csharp' },
            { text: 'C', value: 'c' },
            { text: 'C++', value: 'cpp' },
            { text: 'CSS', value: 'css' },
            { text: 'Dockerfile', value: 'dockerfile' },
            { text: 'F#', value: 'fsharp' },
            { text: 'Go', value: 'go' },
            { text: 'HTML/XML', value: 'xml' },
            { text: 'JavaScript', value: 'javascript' },
            { text: 'Json', value: 'json' },
            { text: 'Kotlin', value: 'kotlin' },
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
        ],
        setup: (editor) => {
            editor.on('init', () => {
                if (isDarkTheme) {
                    const container = editor.getContainer();
                    const iframe = container.querySelector('iframe');
                    const innerDoc = iframe.contentDocument || iframe.contentWindow.document;
                    innerDoc.documentElement.setAttribute('data-bs-theme', 'dark');
                }
            });

            editor.on('NodeChange', (e) => {
                if (e.element.tagName === 'IMG') {
                    e.element.setAttribute('loading', 'lazy');
                }
            });
        }
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