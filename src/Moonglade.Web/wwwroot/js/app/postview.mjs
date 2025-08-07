import { moongladeFetch2 } from './httpService.mjs?v=1426';

const EXPIRATION_DAYS = 30;
const MILLISECONDS_IN_A_DAY = 24 * 60 * 60 * 1000;
const INTERACTION_TIMEOUT = 8000;
const POST_VIEW_KEY_PREFIX = 'post_viewed_';

// Helper function to check if localStorage is available
const isLocalStorageAvailable = () => {
    try {
        const testKey = '__localStorage_test__';
        localStorage.setItem(testKey, 'test');
        localStorage.removeItem(testKey);
        return true;
    } catch {
        return false;
    }
};

export const cleanupLocalStorage = () => {
    if (!isLocalStorageAvailable()) {
        console.warn('localStorage is not available');
        return;
    }

    const now = Date.now();
    const keysToRemove = [];

    // Collect keys to remove first to avoid modifying localStorage during iteration
    for (let i = 0; i < localStorage.length; i++) {
        const key = localStorage.key(i);
        if (key?.startsWith(POST_VIEW_KEY_PREFIX)) {
            try {
                const record = JSON.parse(localStorage.getItem(key));
                if (record?.timestamp) {
                    const recordTime = new Date(record.timestamp).getTime();
                    if (isNaN(recordTime) || now - recordTime > EXPIRATION_DAYS * MILLISECONDS_IN_A_DAY) {
                        keysToRemove.push(key);
                    }
                } else {
                    keysToRemove.push(key); // Remove records without timestamp
                }
            } catch (error) {
                console.error(`Error parsing localStorage key "${key}":`, error);
                keysToRemove.push(key);
            }
        }
    }

    // Remove collected keys
    keysToRemove.forEach(key => {
        try {
            localStorage.removeItem(key);
        } catch (error) {
            console.error(`Error removing localStorage key "${key}":`, error);
        }
    });

    if (keysToRemove.length > 0) {
        console.log(`Cleaned up ${keysToRemove.length} expired post view records`);
    }
};

export const recordPostView = (postId) => {
    // Input validation
    if (!postId || typeof postId !== 'string' && typeof postId !== 'number') {
        console.error('Invalid postId provided to recordPostView');
        return;
    }

    if (!isLocalStorageAvailable()) {
        console.warn('localStorage is not available, cannot record post view');
        return;
    }

    const localStorageKey = `${POST_VIEW_KEY_PREFIX}${postId}`;
    let hasInteracted = false;
    let timeoutId = null;
    let isRecording = false;

    // Check if already recorded
    try {
        if (localStorage.getItem(localStorageKey)) {
            return;
        }
    } catch (error) {
        console.error('Error checking localStorage:', error);
        return;
    }

    const handleInteraction = () => {
        if (hasInteracted) return; // Prevent duplicate calls
        hasInteracted = true;
        removeInteractionListeners();
    };

    const addInteractionListeners = () => {
        const options = { once: true, passive: true };
        window.addEventListener('scroll', handleInteraction, options);
        window.addEventListener('click', handleInteraction, options);
        window.addEventListener('keydown', handleInteraction, options);
        window.addEventListener('touchstart', handleInteraction, options); // Mobile support
    };

    const removeInteractionListeners = () => {
        window.removeEventListener('scroll', handleInteraction);
        window.removeEventListener('click', handleInteraction);
        window.removeEventListener('keydown', handleInteraction);
        window.removeEventListener('touchstart', handleInteraction);
    };

    const recordView = async () => {
        if (isRecording) return; // Prevent duplicate API calls
        isRecording = true;

        try {
            const timestamp = new Date().toISOString();
            
            await moongladeFetch2('/api/postview', 'POST', {
                postId: String(postId), // Ensure postId is a string
                clientTimeStamp: timestamp,
            });

            // Only save to localStorage after successful API call
            localStorage.setItem(localStorageKey, JSON.stringify({ 
                timestamp,
                postId: String(postId)
            }));

            console.debug(`Post view recorded for postId: ${postId}`);
        } catch (error) {
            console.error('Failed to record post view:', error);
            isRecording = false; // Reset flag on error
            throw error; // Re-throw to allow caller to handle
        }
    };

    const cleanup = () => {
        if (timeoutId) {
            clearTimeout(timeoutId);
            timeoutId = null;
        }
        removeInteractionListeners();
    };

    // Set up interaction detection
    addInteractionListeners();

    // Set timeout for recording
    timeoutId = setTimeout(async () => {
        try {
            if (hasInteracted && !isRecording) {
                await recordView();
            }
        } catch (error) {
            // Error already logged in recordView
        } finally {
            cleanup();
        }
    }, INTERACTION_TIMEOUT);

    // Clean up on page unload
    const handleBeforeUnload = () => {
        cleanup();
        window.removeEventListener('beforeunload', handleBeforeUnload);
    };
    window.addEventListener('beforeunload', handleBeforeUnload);
};
