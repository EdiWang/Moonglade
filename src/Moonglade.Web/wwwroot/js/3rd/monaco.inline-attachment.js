(function () {
    'use strict';

    inlineAttachment.editors.monaco = {
        Editor: function (monacoEditor) {

            var me = monacoEditor;

            return {
                getValue: function () {
                    return me.getValue();
                },
                insertValue: function (val) {
                    //inlineAttachment.util.insertTextAtCursor(input, val);
                    insertTextToMonacoEditor(me, "\n" + val);
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
                e.stopPropagation();
                e.preventDefault();
                inlineattach.onDrop(e);
            }, false);

            eventArea.addEventListener('dragenter', function (e) {
                e.stopPropagation();
                e.preventDefault();
            }, false);

            eventArea.addEventListener('dragover', function (e) {
                e.stopPropagation();
                e.preventDefault();
            }, false);
        }
    };

})();