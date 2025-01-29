import { callApi } from './httpService.mjs'

export const cleanupLocalStorage = () => {
    const expirationDays = 30;
    const now = new Date().getTime();

    Object.keys(localStorage).forEach(key => {
        if (key.startsWith("post_viewed_")) {
            const record = JSON.parse(localStorage.getItem(key));
            if (record && record.timestamp) {
                const recordTime = new Date(record.timestamp).getTime();
                if ((now - recordTime) > expirationDays * 24 * 60 * 60 * 1000) {
                    localStorage.removeItem(key);
                }
            }
        }
    });
};

export const recordPostView = (postId) => {
    const localStorageKey = `post_viewed_${postId}`;
    let hasInteracted = false;

    if (localStorage.getItem(localStorageKey)) return;

    window.addEventListener("scroll", () => {
        hasInteracted = true;
    });

    window.addEventListener("click", () => {
        hasInteracted = true;
    });

    window.addEventListener("keydown", () => {
        hasInteracted = true;
    });

    setTimeout(() => {
        if (hasInteracted) {
            callApi(`/api/postview`, 'POST', {
                postId: postId,
                clientTimeStamp: new Date().toISOString()
            }, () => {
                localStorage.setItem(localStorageKey, "true");
            });
        }
    }, 8000);
};
