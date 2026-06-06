// Shared cart utilities — available on every page

function updateCartBadge(count) {
    const badge = document.getElementById('cartCount');
    if (!badge) return;
    if (count > 0) {
        badge.textContent = count > 99 ? '99+' : count;
        badge.style.display = '';
    } else {
        badge.style.display = 'none';
    }
}

// Called from server-rendered product cards (data-* attrs avoid JS string escaping issues)
function addToCartFromData(btn) {
    addToCart(
        parseInt(btn.dataset.pid),
        btn.dataset.pcat,
        btn.dataset.pname,
        parseFloat(btn.dataset.pprice),
        btn.dataset.pimg || null
    );
}

function addToCart(id, category, name, price, imageUrl) {
    fetch('/Cart/Add', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': document.querySelector('#csrfForm input[name="__RequestVerificationToken"]')?.value || ''
        },
        body: JSON.stringify({ componentId: id, category, name, price, imageUrl: imageUrl || null })
    })
    .then(r => {
        if (r.status === 401) { window.location.href = '/Account/Login'; return null; }
        return r.ok ? r.json() : null;
    })
    .then(d => {
        if (!d) return;
        updateCartBadge(d.count);
        showToast('Đã thêm vào giỏ hàng', 'success');
    })
    .catch(() => showToast('Không thể thêm vào giỏ', 'error'));
}

function showToast(message, type) {
    const existing = document.getElementById('cartToast');
    if (existing) existing.remove();

    const toast = document.createElement('div');
    toast.id = 'cartToast';
    toast.style.cssText = `
        position:fixed; bottom:24px; right:24px; z-index:9999;
        padding:12px 20px; border-radius:10px; font-size:.875rem;
        color:#fff; font-weight:500; pointer-events:none;
        background:${type === 'success' ? 'rgba(34,197,94,.85)' : 'rgba(239,68,68,.85)'};
        backdrop-filter:blur(8px); box-shadow:0 4px 20px rgba(0,0,0,.3);
        animation:fadeInUp .25s ease;
    `;
    toast.textContent = message;

    if (!document.getElementById('toastStyle')) {
        const s = document.createElement('style');
        s.id = 'toastStyle';
        s.textContent = '@keyframes fadeInUp{from{opacity:0;transform:translateY(12px)}to{opacity:1;transform:translateY(0)}}';
        document.head.appendChild(s);
    }

    document.body.appendChild(toast);
    setTimeout(() => toast.remove(), 2500);
}
