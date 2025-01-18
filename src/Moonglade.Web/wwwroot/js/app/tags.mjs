document.getElementById('tagFilter').addEventListener('keyup', function () {
    const value = this.value.toLowerCase();
    document.querySelectorAll('.ul-tags li').forEach(item => {
        const isVisible = item.textContent.toLowerCase().includes(value);
        item.classList.toggle('d-inline-block', isVisible);
        item.classList.toggle('d-none', !isVisible);
    });
});
