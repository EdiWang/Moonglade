const codeInput = document.getElementById('AuthenticatorCode');
const digits = [...document.querySelectorAll('.auth-code-digits input')];

const syncCode = () => {
    codeInput.value = digits.map(digit => digit.value).join('');
};

const focusDigit = index => {
    const digit = digits[Math.max(0, Math.min(index, digits.length - 1))];
    digit.focus();
    digit.select();
};

const enterDigits = (index, value) => {
    const numbers = value.replace(/[^0-9]/g, '');
    if (!numbers) {
        digits[index].value = '';
        syncCode();
        return;
    }

    const start = numbers.length >= digits.length ? 0 : index;
    [...numbers.slice(0, digits.length - start)].forEach((number, offset) => {
        digits[start + offset].value = number;
    });
    syncCode();
    focusDigit(start + numbers.length);
};

digits.forEach((digit, index) => {
    digit.addEventListener('focus', () => digit.select());
    digit.addEventListener('beforeinput', event => {
        if (event.data && /[^0-9]/.test(event.data) && event.inputType === 'insertText') {
            event.preventDefault();
        }
    });
    digit.addEventListener('input', () => enterDigits(index, digit.value));
    digit.addEventListener('paste', event => {
        event.preventDefault();
        enterDigits(index, event.clipboardData.getData('text'));
    });
    digit.addEventListener('keydown', event => {
        if (event.key === 'Backspace' && !digit.value && index > 0) {
            event.preventDefault();
            digits[index - 1].value = '';
            syncCode();
            focusDigit(index - 1);
        } else if (event.key === 'ArrowLeft' || event.key === 'ArrowRight') {
            event.preventDefault();
            focusDigit(index + (event.key === 'ArrowLeft' ? -1 : 1));
        }
    });
});

codeInput.form.addEventListener('submit', syncCode);
focusDigit(0);
