import { fetch2 } from './httpService.mjs?v=1500';
import { success } from './toastService.mjs';

const editCanvas = new bootstrap.Offcanvas(document.getElementById('editCatCanvas'));

const grid = document.querySelector('#category-grid');
const emptyState = document.querySelector('#category-empty');
const loading = document.querySelector('#category-loading');
const editForm = document.querySelector('#edit-form');
const btnNewCategory = document.querySelector('#btn-new-cat');
const slugInput = document.querySelector('#EditCategoryRequest_Slug');
const displayNameInput = document.querySelector('#EditCategoryRequest_DisplayName');
const noteInput = document.querySelector('#EditCategoryRequest_Note');

let currentCategoryId = window.emptyGuid;

btnNewCategory.addEventListener('click', initCreateCategory);
editForm.addEventListener('submit', handleSubmit);

loadCategories();

function initCreateCategory() {
    currentCategoryId = window.emptyGuid;
    editForm.reset();
    editCanvas.show();
}

async function loadCategories() {
    loading.classList.remove('d-none');

    try {
        const list = (await fetch2('/api/category/list', 'GET')) ?? [];
        renderCategories(list);
    } finally {
        loading.classList.add('d-none');
    }
}

function renderCategories(list) {
    grid.innerHTML = '';

    if (!list.length) {
        emptyState.classList.remove('d-none');
        return;
    }

    emptyState.classList.add('d-none');

    const fragment = document.createDocumentFragment();

    list
        .slice()
        .sort((a, b) => a.displayName.localeCompare(b.displayName))
        .forEach(cat => fragment.appendChild(buildCategoryCard(cat)));

    grid.appendChild(fragment);
}

function buildCategoryCard(cat) {
    const col = document.createElement('div');
    col.className = 'col';

    const card = document.createElement('div');
    card.className = 'card shadow-sm';
    card.id = `card-${cat.id}`;

    const body = document.createElement('div');
    body.className = 'card-body';

    const title = document.createElement('h6');
    title.className = 'card-title';

    const link = document.createElement('a');
    link.href = `/CategoryList?slug=${encodeURIComponent(cat.slug)}`;
    link.target = '_blank';
    link.textContent = cat.displayName;

    const note = document.createElement('p');
    note.className = 'mt-2 h-30px';
    note.textContent = cat.note;

    const slug = document.createElement('small');
    slug.className = 'text-muted';
    slug.textContent = cat.slug;

    const footer = document.createElement('div');
    footer.className = 'card-footer';

    const btnEdit = document.createElement('a');
    btnEdit.className = 'btn btn-sm btn-outline-accent btn-edit me-2';
    btnEdit.innerHTML = '<i class="bi-pen"></i>';
    btnEdit.addEventListener('click', async () => await editCategory(cat.id));

    const btnDelete = document.createElement('a');
    btnDelete.className = 'btn btn-sm btn-outline-danger btn-delete';
    btnDelete.innerHTML = '<i class="bi-trash"></i>';
    btnDelete.addEventListener('click', async () => await confirmDelete(cat.id));

    title.appendChild(link);
    body.appendChild(title);
    body.appendChild(note);
    body.appendChild(slug);

    footer.appendChild(btnEdit);
    footer.appendChild(btnDelete);

    card.appendChild(body);
    card.appendChild(footer);
    col.appendChild(card);

    return col;
}

async function editCategory(id) {
    const data = await fetch2(`/api/category/${id}`, 'GET');

    currentCategoryId = data.id;
    setFormValues(data);
    editCanvas.show();
}

function setFormValues({ slug, displayName, note }) {
    slugInput.value = slug;
    displayNameInput.value = displayName;
    noteInput.value = note;
}

async function deleteCategory(id) {
    await fetch2(`/api/category/${id}`, 'DELETE');
    await loadCategories();
    success('Category deleted');
}

async function confirmDelete(id) {
    if (confirm('Delete?')) {
        await deleteCategory(id);
    }
}

async function handleSubmit(event) {
    event.preventDefault();

    const isCreate = currentCategoryId === window.emptyGuid;
    const apiAddress = isCreate ? '/api/category' : `/api/category/${currentCategoryId}`;
    const verb = isCreate ? 'POST' : 'PUT';

    const data = new FormData(event.target);
    const value = Object.fromEntries(data.entries());

    await fetch2(apiAddress, verb, {
        slug: value['EditCategoryRequest.Slug'],
        displayName: value['EditCategoryRequest.DisplayName'],
        note: value['EditCategoryRequest.Note']
    });

    editForm.reset();
    editCanvas.hide();
    await loadCategories();
    success(isCreate ? 'Category created' : 'Category updated');
}