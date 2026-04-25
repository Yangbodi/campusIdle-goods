// 获取防伪令牌的辅助函数
function getRequestVerificationToken() {
    return document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
}

// 设置AJAX请求的防伪令牌
function setAntiForgeryToken(xhr) {
    const token = getRequestVerificationToken();
    if (token) {
        xhr.setRequestHeader('RequestVerificationToken', token);
    }
}
