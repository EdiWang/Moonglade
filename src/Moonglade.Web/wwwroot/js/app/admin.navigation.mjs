const navigation = document.querySelector('.admin-secondary-nav');
const activeLink = navigation.querySelector('[aria-current="page"]');
const revealLink = link => link.scrollIntoView({ block: 'nearest', inline: 'nearest' });
const revealActiveLink = () => revealLink(activeLink);

for (const link of navigation.querySelectorAll('a')) {
    link.addEventListener('focus', () => revealLink(link));
}

document.fonts.ready.then(revealActiveLink);
new ResizeObserver(revealActiveLink).observe(navigation);
