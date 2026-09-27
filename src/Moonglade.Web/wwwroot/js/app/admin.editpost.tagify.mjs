import { fetch2 } from './httpService.mjs?v=1500';

export function createTagifyMixin() {
    return {
        tagifyInstance: null,
        categoryTagifyInstance: null,

        async initTagify() {
            this.initCategoryTagify();

            const input = document.querySelector('#post-tags-input');
            if (!input) return;

            let data;
            try {
                data = await fetch2('/api/tags/names', 'GET', {});
            } catch (error) {
                console.error('Failed to fetch tag names:', error);
                return;
            }

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

        initCategoryTagify() {
            const input = document.querySelector('#post-categories-input');
            if (!input) return;

            const whitelist = this.categories.map(category => ({
                value: category.id,
                name: category.displayName,
                title: category.displayName
            }));

            this.categoryTagifyInstance = new Tagify(input, {
                whitelist,
                enforceWhitelist: true,
                tagTextProp: 'name',
                delimiters: null,
                editTags: false,
                a11y: { inputAriaLabel: input.placeholder },
                transformTag: tag => {
                    const category = whitelist.find(category =>
                        category.value === tag.value || category.name === tag.value);
                    if (category) Object.assign(tag, category);
                },
                dropdown: {
                    maxItems: 30,
                    classname: 'tags-dropdown',
                    enabled: 0,
                    closeOnSelect: false,
                    mapValueTo: 'name',
                    searchKeys: ['name']
                }
            });

            this.categoryTagifyInstance.addTags(this.formData.selectedCatIds
                .map(id => whitelist.find(category => category.value === id))
                .filter(Boolean));

            this.categoryTagifyInstance.on('add remove', () => {
                const ids = this.categoryTagifyInstance.value.map(category => category.value);
                if (JSON.stringify(ids) !== JSON.stringify(this.formData.selectedCatIds)) {
                    this.formData.selectedCatIds = ids;
                    this.isFormDirty = true;
                }
            });
        },

        syncTags() {
            if (this.tagifyInstance) {
                this.formData.tags = this.tagifyInstance.value.map(t => t.value).join(',');
            }

            if (this.categoryTagifyInstance) {
                this.formData.selectedCatIds = this.categoryTagifyInstance.value.map(category => category.value);
            }
        }
    };
}
