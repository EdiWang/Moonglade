export function createFormMixin() {
    return {
        isFormDirty: false,

        setupKeyboardShortcuts() {
            window.addEventListener('keydown', (event) => {
                if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 's') {
                    event.preventDefault();
                    this.submitAction = 'save';
                    this.handleSubmit();
                }
            });
        },

        setupDirtyFormWarning() {
            const form = document.getElementById('post-edit-form');
            if (!form) return;

            form.addEventListener('input', () => {
                this.isFormDirty = true;
            });

            window.addEventListener('beforeunload', (event) => {
                if (this.isFormDirty) {
                    const message = 'You have unsaved changes, are you sure to leave this page?';
                    event.returnValue = message;
                    return message;
                }
            });
        }
    };
}
