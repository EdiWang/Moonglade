let DocumentOutline;

(function () {
    const menuIcon = '<i class="bi bi-list-ul"></i>';
    const closeIcon = '<i class="bi bi-arrow-left-circle"></i>';

    DocumentOutline = class DocumentOutline {

        constructor(querySelectors) {
            this._headingMap = [];
            this._parentList = [];
            this._open = window.innerWidth > 1440;

            // get heading tags
            const headingList = document.querySelectorAll(querySelectors);
            headingList.forEach(tag => {
                this._headingMap.push({
                    tag,
                    level: Number(tag.tagName[1])
                });
            });

            this._buildOutline();
            this._renderOutline();
        }

        _getParent = level => {
            for (let i = 0; i < this._parentList.length; i++) {
                let node = this._parentList[i];

                if (node.level < level) {
                    return node;
                }

                if (node.level === level && node.elem.tagName == 'UL') {
                    return node;
                }
            }
        }

        _hasSibilings = level => {
            const parent = this._getParent(level);
            const family = this._parentList.slice(0, this._parentList.indexOf(parent) + 1);

            for (let i = 0; i < family.length; i++) {
                let node = family[i];

                if (node.level === level) return true;
            }
            return false;
        }

        _buildOutline = () => {
            this._parentList = [{
                elem: document.createElement('ul'),
                level: 0
            }];

            for (let i = 0; i < this._headingMap.length; i++) {
                const level = this._headingMap[i].level;
                const parent = this._getParent(level);

                let li = document.createElement('li');
                let span = document.createElement('span');
                let div = document.createElement('div');

                // add navigation
                span.innerHTML = this._headingMap[i].tag.innerHTML;
                span.addEventListener('click', e => {
                    window.scrollTo(0, this._headingMap[i].tag.offsetTop);
                });

                // build dom
                div.setAttribute('class', `li-content li-title-${level}`);
                div.appendChild(span);
                li.appendChild(div);

                let node = { elem: li, level };
                if (parent.elem.tagName == 'LI' || !this._hasSibilings(level)) {
                    let ul = document.createElement('ul');
                    ul.appendChild(li);
                    node.elem = ul;
                }

                // attach to parent
                let container = parent.elem;
                if (parent.elem.tagName == 'UL'
                    && parent.elem?.childNodes[0]?.tagName == 'LI'
                    && !this._hasSibilings(level)) {
                    container = parent.elem.firstChild;
                }

                // attach to list
                container.setAttribute('class', 'list-head');
                container.appendChild(node.elem);
                this._parentList.unshift(node);
            }

            if (this._headingMap.length > 0) {
                // save list root
                this._root = this._parentList[this._parentList.length - 2].elem;
                this._root.setAttribute('id', 'outline-list-root');
            }
        }

        _renderOutline = () => {
            if (this._headingMap.length <= 0) return;

            this._nav = document.createElement('nav');
            this._main = document.createElement('div');

            // menu icon
            this._menuIcon = document.createElement('div');
            this._menuIcon.classList = 'outline-menu-icon-container';

            // header
            this._navHeader = document.createElement('div');
            this._navHeader.classList = 'outline-nav-header';
            this._navHeader.appendChild(this._menuIcon);

            // outline
            this._nav.appendChild(this._navHeader);
            this._nav.classList = 'outline-nav';
            if (!this._open)
                this.hideOutline();

            // add to DOM 
            this._nav.appendChild(this._root);
            document.body.appendChild(this._main);
            document.body.appendChild(this._nav);

            this._addIcon(this._menuIcon, this._open ? 'close' : 'menu');

            this._menuIcon.addEventListener('click', e => {
                if (this._open) this.hideOutline();
                else this.showOutline();
                this._open = !this._open;
            });
        }

        _addIcon = (container, icon) => {
            let html = icon === 'menu' ? menuIcon : closeIcon;
            container.innerHTML = html;
        }

        showOutline = () => {
            this._menuIcon.style.visibility = 'hidden';
            this._menuIcon.classList.remove('outline-menu-container-collapsed');

            this._navHeader.classList.remove('outline-nav-header-collapsed');
            this._main.classList.remove('no-outline');

            this._nav.classList.remove('outline-nav-collapsed');

            this._root.style.display = 'block';
            this._root.style.visibility = 'visible';

            setTimeout(() => {
                this._root.style.opacity = 1;
                this._nav.style.overflowY = 'visible';
                this._menuIcon.style.visibility = 'visible';
                this._addIcon(this._menuIcon, 'close');
            }, 400);
        }

        hideOutline = () => {
            this._menuIcon.style.visibility = 'hidden';
            this._addIcon(this._menuIcon, 'menu');
            this._navHeader.classList.add('outline-nav-header-collapsed');

            this._nav.style.overflowY = 'hidden';
            this._nav.classList.add('outline-nav-collapsed');
            this._main.classList.add('no-outline');

            this._root.style.visibility = 'hidden';
            this._root.style.opacity = 0;

            setTimeout(() => {
                this._root.style.display = 'none';
                this._menuIcon.classList.add('outline-menu-container-collapsed');
                this._menuIcon.style.visibility = 'visible';
            }, 350);
        }
    };
})();