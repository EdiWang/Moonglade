(function () {
    'use strict';

    function insertText(editor, text) {
        var selection = editor.getSelection();
        var range = selection;

        if (!range && typeof monaco !== 'undefined' && monaco.Range) {
            var position = editor.getPosition();
            range = new monaco.Range(position.lineNumber, position.column, position.lineNumber, position.column);
        }

        editor.executeEdits('inline-attachment', [{
            range: range,
            text: text,
            forceMoveMarkers: true
        }]);
    }

    inlineAttachment.editors.monaco = {
        Editor: function (monacoEditor) {

            var me = monacoEditor;

            return {
                getValue: function () {
                    return me.getValue();
                },
                insertValue: function (val) {
                    insertText(me, "\n" + val);
                },
                setValue: function (val) {
                    me.setValue(val);
                }
            };
        },
        attach: function (input, eventArea, options) {
            options = options || {};

            var editor = new inlineAttachment.editors.monaco.Editor(input),
                inlineattach = new inlineAttachment(options, editor);

            eventArea.addEventListener('paste', function (e) {
                inlineattach.onPaste(e);
            });

            eventArea.addEventListener('drop', function (e) {
                eventArea.classList.remove('drag-over');

                e.stopPropagation();
                e.preventDefault();
                inlineattach.onDrop(e);
            }, false);

            eventArea.addEventListener('dragleave', function (e) {
                eventArea.classList.remove('drag-over');
                e.stopPropagation();
                e.preventDefault();
            }, false);

            ['dragenter', 'dragover'].forEach(eventName => {
                eventArea.addEventListener(eventName, function (e) {
                    eventArea.classList.add('drag-over');

                    e.stopPropagation();
                    e.preventDefault();
                }, false);
            });
        }
    };

})();