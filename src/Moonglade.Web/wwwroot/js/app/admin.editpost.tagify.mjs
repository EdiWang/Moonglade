import { fetch2 } from './httpService.mjs?v=1500';

export function createTagifyMixin() {
    return {
        tagifyInstance: null,

        async initTagify() {
            const data = await fetch2('/api/tags/names', 'GET', {});
            const input = document.querySelector('#post-tags-input');
            if (!input) return;

            this.tagifyInstance = new Tagify(input, {
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

            // Load existing tags
            if (this.formData.tags) {
                const existingTags = this.formData.tags.split(',').filter(t => t.trim());
                this.tagifyInstance.addTags(existingTags);
            }
        },

        syncTags() {
            if (this.tagifyInstance) {
                this.formData.tags = this.tagifyInstance.value.map(t => t.value).join(',');
            }
        }
    };
}
