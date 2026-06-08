// Shared cart utilities — available on every page

function updateCartBadge(count) {
    const label = count > 99 ? '99+' : String(count);
    const badge = document.getElementById('cartCount');
    if (badge) {
        if (count > 0) { badge.textContent = label; badge.style.display = ''; }
        else badge.style.display = 'none';
    }
    const bottomBadge = document.getElementById('bottomCartBadge');
    if (bottomBadge) {
        if (count > 0) { bottomBadge.textContent = label; bottomBadge.style.display = ''; }
        else bottomBadge.style.display = 'none';
    }
}

// Called from server-rendered product cards (data-* attrs avoid JS string escaping issues)
function addToCartFromData(btn) {
    addToCart(
        parseInt(btn.dataset.pid),
        btn.dataset.pcat,
        btn.dataset.pname,
        parseFloat(btn.dataset.pprice),
        btn.dataset.pimg || null,
        btn
    );
}

// P20: fly a dot from the clicked button to the cart icon
function flyToCart(triggerEl) {
    const cartIcon = document.getElementById('cartCount')?.closest('a') ||
                     document.querySelector('a[href="/cart"]');
    if (!cartIcon || !triggerEl) return;

    const src  = triggerEl.getBoundingClientRect();
    const dst  = cartIcon.getBoundingClientRect();

    const dot = document.createElement('div');
    dot.style.cssText = `
        position:fixed;
        width:12px; height:12px; border-radius:50%;
        background:linear-gradient(135deg,#a855f7,#3b82f6);
        box-shadow:0 0 8px rgba(168,85,247,.7);
        pointer-events:none; z-index:99999;
        left:${src.left + src.width/2 - 6}px;
        top:${src.top  + src.height/2 - 6}px;
        transition:left .55s cubic-bezier(.4,0,.2,1),
                   top  .55s cubic-bezier(.4,0,.2,1),
                   opacity .2s .4s, transform .55s;
    `;
    document.body.appendChild(dot);

    // force reflow then animate
    dot.getBoundingClientRect();
    dot.style.left    = `${dst.left + dst.width/2  - 6}px`;
    dot.style.top     = `${dst.top  + dst.height/2 - 6}px`;
    dot.style.opacity = '0';
    dot.style.transform = 'scale(.4)';

    setTimeout(() => dot.remove(), 700);
}

function addToCart(id, category, name, price, imageUrl, triggerEl) {
    if (triggerEl) flyToCart(triggerEl);

    fetch('/Cart/Add', {
        method: 'POST',
        credentials: 'same-origin',
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
        if (typeof invalidateMiniCart === 'function') invalidateMiniCart();
        showToast(`<i class="bi bi-check-circle-fill me-2"></i>${window.i18n?.addedToCart ?? 'Đã thêm vào giỏ'}`, 'success');
    })
    .catch(() => showToast(`<i class="bi bi-exclamation-circle me-2"></i>${window.i18n?.cannotAdd ?? 'Không thể thêm'}`, 'error'));
}

// Enhanced toast — supports HTML, 3 types
function showToast(html, type = 'success', duration = 2800) {
    const existing = document.querySelector('.ts-toast');
    if (existing) existing.remove();

    const colors = {
        success: 'rgba(22,163,74,.9)',
        error:   'rgba(220,38,38,.9)',
        info:    'rgba(99,102,241,.9)',
    };

    const toast = document.createElement('div');
    toast.className = 'ts-toast';
    toast.style.cssText = `
        position:fixed; bottom:28px; right:28px; z-index:99990;
        padding:12px 20px; border-radius:12px; font-size:.875rem;
        color:#fff; font-weight:500;
        background:${colors[type] ?? colors.success};
        backdrop-filter:blur(12px);
        box-shadow:0 6px 28px rgba(0,0,0,.35);
        display:flex; align-items:center; gap:6px;
        animation:tsToastIn .28s cubic-bezier(.34,1.56,.64,1) both;
        max-width:320px; pointer-events:none;
    `;
    toast.innerHTML = html;

    if (!document.getElementById('tsToastCss')) {
        const s = document.createElement('style');
        s.id = 'tsToastCss';
        s.textContent = `
            @keyframes tsToastIn {
                from { opacity:0; transform:translateY(16px) scale(.94); }
                to   { opacity:1; transform:translateY(0)    scale(1); }
            }
            @keyframes fadeInUp {
                from { opacity:0; transform:translateY(12px); }
                to   { opacity:1; transform:translateY(0); }
            }
        `;
        document.head.appendChild(s);
    }

    document.body.appendChild(toast);
    setTimeout(() => {
        toast.style.transition = 'opacity .25s, transform .25s';
        toast.style.opacity = '0';
        toast.style.transform = 'translateY(8px)';
        setTimeout(() => toast.remove(), 280);
    }, duration);
}
