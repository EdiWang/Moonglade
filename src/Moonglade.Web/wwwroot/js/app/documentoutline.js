let DocumentOutline;

(() => {
    const icons = Object.freeze({
        menu: '<i class="bi bi-list-ul"></i>',
        close: '<i class="bi bi-arrow-left-circle" role="button" aria-label="Hide outline navigation menu"></i>'
    });

    DocumentOutline = class DocumentOutline {
        constructor(querySelectors) {
            this._headingMap = Array.from(document.querySelectorAll(querySelectors), tag => ({
                tag,
                level: Number(tag.tagName.substring(1))
            }));
            this._parentList = [];
            this._open = window.innerWidth > 1440;

            this._buildOutline();
            this._renderOutline();
        }

        _buildOutline() {
            if (this._headingMap.length === 0) {
                return;
            }

            this._parentList = [{ elem: document.createElement('ul'), level: 0 }];

            this._headingMap.forEach(({ tag, level }) => {
                const parent = this._getParent(level);
                const node = this._createOutlineNode(tag, level, parent);

                const container = parent.elem.tagName === 'UL' && parent.elem.firstChild?.tagName === 'LI' && !this._hasSiblings(level)
                    ? parent.elem.firstChild
                    : parent.elem;

                container.classList.add('list-head');
                container.appendChild(node.elem);

                this._parentList.unshift(node);
            });

            this._root = this._parentList[this._parentList.length - 2].elem;
            this._root.id = 'outline-list-root';
        }

        _renderOutline() {
            if (this._headingMap.length === 0) {
                return;
            }

            this._nav = document.createElement('nav');
            this._main = document.createElement('div');
            this._menuIcon = document.createElement('div');
            this._navHeader = document.createElement('div');

            this._menuIcon.className = 'outline-menu-icon-container';
            this._navHeader.className = 'outline-nav-header';
            this._navHeader.appendChild(this._menuIcon);

            this._nav.className = 'outline-nav';
            this._nav.setAttribute('aria-label', 'Document outline');
            this._nav.appendChild(this._navHeader);

            if (!this._open) {
                this.hideOutline();
            }

            this._nav.appendChild(this._root);
            document.body.append(this._main, this._nav);

            this._setIcon(this._open ? 'close' : 'menu');

            this._menuIcon.addEventListener('click', () => {
                this._open ? this.hideOutline() : this.showOutline();
            });
        }

        _createOutlineNode(tag, level, parent) {
            const li = document.createElement('li');
            const span = document.createElement('span');
            const div = document.createElement('div');

            span.innerHTML = tag.innerHTML;
            span.addEventListener('click', () => {
                window.scrollTo({ top: tag.offsetTop, behavior: 'smooth' });
            });

            div.className = `li-content li-title-${level}`;
            div.setAttribute('role', 'link');
            div.appendChild(span);
            li.appendChild(div);

            const node = { elem: li, level };

            if (parent.elem.tagName === 'LI' || !this._hasSiblings(level)) {
                const ul = document.createElement('ul');
                ul.appendChild(li);
                node.elem = ul;
            }

            return node;
        }

        _getParent(level) {
            return this._parentList.find(node => node.level < level || (node.level === level && node.elem.tagName === 'UL'));
        }

        _hasSiblings(level) {
            const parent = this._getParent(level);
            return this._parentList.some(node => node.level === level && this._parentList.indexOf(node) <= this._parentList.indexOf(parent));
        }

        _setIcon(icon) {
            this._menuIcon.innerHTML = icons[icon];
        }

        showOutline() {
            this._toggleOutline(true);
        }

        hideOutline() {
            this._toggleOutline(false);
        }

        _toggleOutline(isVisible) {
            this._open = isVisible;
            const action = isVisible ? 'remove' : 'add';

            this._menuIcon.style.visibility = 'hidden';
            this._setIcon(isVisible ? 'close' : 'menu');
            this._menuIcon.classList[action]('outline-menu-container-collapsed');
            this._navHeader.classList[action]('outline-nav-header-collapsed');
            this._main.classList[action]('no-outline');
            this._nav.classList[action]('outline-nav-collapsed');

            this._root.style.visibility = isVisible ? 'visible' : 'hidden';
            this._root.style.opacity = isVisible ? 1 : 0;
            this._root.style.display = isVisible ? 'block' : 'none';

            if (isVisible) {
                setTimeout(() => {
                    this._nav.style.overflowY = 'visible';
                    this._menuIcon.style.visibility = 'visible';
                }, 400);
            } else {
                this._nav.style.overflowY = 'hidden';
                setTimeout(() => {
                    this._menuIcon.style.visibility = 'visible';
                }, 350);
            }
        }
    };
})();
