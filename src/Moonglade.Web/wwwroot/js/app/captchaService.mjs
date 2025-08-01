export function resetCaptchaImage() {
    const d = new Date();
    document.querySelector('#img-captcha').src = `/captcha-image?${d.getTime()}`;
}

export function showCaptcha() {
    const captchaContainer = document.getElementById('captcha-container');
    if (!captchaContainer) return false;

    if (captchaContainer.style.display === 'none') {
        captchaContainer.style.display = 'flex';
        resetCaptchaImage();
        return true;
    }
    return false;
}