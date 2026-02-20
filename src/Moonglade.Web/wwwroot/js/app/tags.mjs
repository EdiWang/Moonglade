document.getElementById('tagFilter').addEventListener('keyup', function () {
    const value = this.value.toLowerCase();

    document.querySelectorAll('.tag-letter-group').forEach(group => {
        const items = group.querySelectorAll('.ul-tags li');
        let visibleCount = 0;

        items.forEach(item => {
            const isVisible = item.textContent.toLowerCase().includes(value);
            item.classList.toggle('d-inline-block', isVisible);
            item.classList.toggle('d-none', !isVisible);
            if (isVisible) visibleCount++;
        });

        group.classList.toggle('d-none', visibleCount === 0);
    });
});
