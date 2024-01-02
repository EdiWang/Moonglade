function slugify(text) {
    var isValidTitle = /^[A-Za-z][A-Za-z0-9 \(\)#,\.\?]*$/.test(text);
    if (isValidTitle) {
        return text
            .toLowerCase()
            .replace('(', '')
            .replace(')', '')
            .replace('#', '')
            .replace(',', '')
            .replace('.', '')
            .replace('?', '')
            .replace(/[^\w ]+/g, '')
            .replace(/ +/g, '-');
    }
    return '';
}

export function initEvents(slugifyTitle) {
    if (slugifyTitle) {
        $('#ViewModel_Title').change(function () {
            var newSlug = slugify($(this).val());
            if (newSlug) {
                document.querySelector('#ViewModel_Slug').value = newSlug;
            }
        });
    }

    $('#btn-preview').click(function (e) {
        submitForm(e);
    });

    $('#btn-save').click(function (e) {
        submitForm(e);
    });

    $('#btn-publish').click(function (e) {
        $('input[name="ViewModel.IsPublished"]').val('True');
        submitForm(e);
    });

    $('.btn-modify-slug').click(function () {
        var message = 'This post was published for more than 7 days, changing slug will result in breaking SEO, would you like to continue?';

        if (confirm(message)) {
            $('#ViewModel_Slug').removeAttr('readonly');
            $('#ViewModel_Slug').focus();
            $('.btn-modify-slug').hide();
        }
    })

    function submitForm(e) {
        if (window.tinyMCE) {
            window.tinyMCE.triggerSave();
        }

        if (window.mdContentEditor) {
            assignEditorValues(window.mdContentEditor, ".post-content-textarea");
        }

        if ($('input[name="ViewModel.IsPublished"]').val() === 'True') {
            if (document.querySelector('#btn-publish')) {
                document.querySelector('#btn-publish').style.display = 'none';
            };

            if (document.querySelector('#btn-preview')) {
                document.querySelector('#btn-preview').style.display = 'none';
            }
        }
    }

    $('.post-edit-form').areYouSure({
        message: 'You have unsaved changes, are you sure to leave this page?'
    });

    callApi('/api/tags/names',
        'GET',
        {},
        async (resp) => {
            var data = await resp.json();

            var input = document.querySelector('#ViewModel_Tags'),
                tagify = new Tagify(input,
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
        });

    document.querySelector('#ViewModel_Title').focus();
}

export function loadTinyMCE(textareaSelector) {
    if (window.tinyMCE !== undefined) {
        window.tinyMCE.init({
            selector: textareaSelector,
            themes: 'silver',
            skin: 'tinymce-5',
            height: 'calc(100vh - 400px)',
            relative_urls: false, // avoid image upload fuck up
            browser_spellcheck: true,
            branding: false,
            promotion: false,
            block_formats: 'Paragraph=p; Header 2=h2; Header 3=h3; Header 4=h4; Preformatted=pre',
            plugins: 'advlist autolink autosave link image lists charmap preview anchor pagebreak searchreplace wordcount visualblocks visualchars code fullscreen insertdatetime media nonbreaking save table directionality template codesample emoticons',
            toolbar: 'undo redo | blocks | bold italic underline strikethrough | forecolor backcolor | paste pastetext removeformat | hr link image codesample | charmap emoticons table media | alignleft aligncenter alignright alignjustify | bullist numlist outdent indent | code | fullscreen',
            save_onsavecallback: function () {
                $('#btn-save').trigger('click');
            },
            paste_data_images: true,
            images_upload_url: '/image',
            images_upload_credentials: true,
            extended_valid_elements: 'img[class|src|border=0|alt|title|hspace|vspace|width|height|align|onmouseover|onmouseout|name|loading=lazy]',
            body_class: 'post-content',
            content_css: '/css/tinymce-custom.css',
            codesample_languages: [
                { text: 'Bash', value: 'bash' },
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
                { text: 'Lua', value: 'lua' },
                { text: 'Markdown', value: 'markdown' },
                { text: 'PowerShell', value: 'powershell' },
                { text: 'Plain Text', value: 'plaintext' },
                { text: 'Python', value: 'python' },
                { text: 'R', value: 'r' },
                { text: 'Ruby', value: 'ruby' },
                { text: 'Rust', value: 'rust' },
                { text: 'SCSS', value: 'scss' },
                { text: 'Shell', value: 'shell' },
                { text: 'SQL', value: 'sql' },
                { text: 'Swift', value: 'swift' },
                { text: 'TypeScript', value: 'typescript' },
                { text: 'WASM', value: 'wasm' },
                { text: 'Visual Basic (.NET)', value: 'vbnet' },
                { text: 'YAML', value: 'yaml' }
            ],
            setup: function (editor) {
                editor.on('NodeChange', function (e) {
                    if (e.element.tagName === 'IMG') {
                        e.element.setAttribute('loading', 'lazy');
                    }
                });
            }
        });
    }
}

export function keepAlive() {
    var tid = setInterval(postNonce, 60 * 1000);
    function postNonce() {
        var num = Math.random();
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
    function abortTimer() {
        clearInterval(tid);
    }
}