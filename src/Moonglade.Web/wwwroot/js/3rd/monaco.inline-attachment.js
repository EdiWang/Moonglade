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