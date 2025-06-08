import { moongladeFetch } from './httpService.mjs';

const EXPIRATION_DAYS = 30;
const MILLISECONDS_IN_A_DAY = 24 * 60 * 60 * 1000;

export const cleanupLocalStorage = () => {
    const now = Date.now();

    Object.keys(localStorage).forEach((key) => {
        if (key.startsWith("post_viewed_")) {
            try {
                const record = JSON.parse(localStorage.getItem(key));
                if (record?.timestamp) {
                    const recordTime = new Date(record.timestamp).getTime();
                    if (now - recordTime > EXPIRATION_DAYS * MILLISECONDS_IN_A_DAY) {
                        localStorage.removeItem(key);
                    }
                }
            } catch (error) {
                console.error(`Error parsing localStorage key "${key}":`, error);
                localStorage.removeItem(key); // Remove invalid or corrupted data
            }
        }
    });
};

export const recordPostView = (postId) => {
    const localStorageKey = `post_viewed_${postId}`;
    const INTERACTION_TIMEOUT = 8000;
    let hasInteracted = false;

    if (localStorage.getItem(localStorageKey)) return;

    const handleInteraction = () => {
        hasInteracted = true;
        removeInteractionListeners();
    };

    const addInteractionListeners = () => {
        window.addEventListener("scroll", handleInteraction, { once: true });
        window.addEventListener("click", handleInteraction, { once: true });
        window.addEventListener("keydown", handleInteraction, { once: true });
    };

    const removeInteractionListeners = () => {
        window.removeEventListener("scroll", handleInteraction);
        window.removeEventListener("click", handleInteraction);
        window.removeEventListener("keydown", handleInteraction);
    };

    addInteractionListeners();

    setTimeout(() => {
        if (hasInteracted) {
            moongladeFetch(
                `/api/postview`,
                'POST',
                {
                    postId,
                    clientTimeStamp: new Date().toISOString(),
                },
                () => {
                    localStorage.setItem(localStorageKey, JSON.stringify({ timestamp: new Date().toISOString() }));
                }
            );
        } else {
            removeInteractionListeners();
        }
    }, INTERACTION_TIMEOUT);
};
