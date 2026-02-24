import { fetch2 } from './httpService.mjs?v=1500';

export const recordPostView = (postId) => {
    if (!postId) return;

    fetch2('/api/postview', 'POST', {
        postId: String(postId)
    }).catch(() => { });
};
