export async function resetCaptchaImage() {
    const d = new Date();
    const response = await fetch(`/api/captcha/stateless?${d.getTime()}`);
    const data = await response.json();

    document.getElementById('captcha-token').value = data.token;
    document.getElementById('img-captcha').src = `data:image/png;base64,${data.imageBase64}`;
    document.getElementById('captcha-code').value = '';
}

export async function showCaptcha() {
    const captchaContainer = document.getElementById('captcha-container');
    if (!captchaContainer) return false;

    if (captchaContainer.style.display === 'none') {
        captchaContainer.style.display = 'flex';
        await resetCaptchaImage();
        return true;
    }
    return false;
}