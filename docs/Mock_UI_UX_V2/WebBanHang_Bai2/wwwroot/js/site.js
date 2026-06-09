/* ============================================================
   TechStore — Site interactive layer
   - Cart drawer & AJAX add-to-cart
   - Wishlist toggle
   - Dark mode persistence
   - Toast notifications
   ============================================================ */
(function () {
    'use strict';

    const VND = n => new Intl.NumberFormat('vi-VN').format(n) + 'đ';

    /* ---------- Toast ---------- */
    const toastWrap = () => document.getElementById('toastWrap');
    function toast(message, type = 'info', timeout = 3200) {
        const wrap = toastWrap();
        if (!wrap) return;
        const el = document.createElement('div');
        el.className = 'tx-toast ' + type;
        const icon = type === 'success' ? 'bi-check-circle-fill'
            : type === 'error' ? 'bi-exclamation-octagon-fill'
            : 'bi-info-circle-fill';
        el.innerHTML = `<i class="bi ${icon}"></i><div>${message}</div>`;
        wrap.appendChild(el);
        requestAnimationFrame(() => el.classList.add('show'));
        setTimeout(() => {
            el.classList.remove('show');
            setTimeout(() => el.remove(), 400);
        }, timeout);
    }

    /* ---------- Dark mode ---------- */
    const themeKey = 'techstore.theme';
    function applyTheme(t) {
        document.documentElement.setAttribute('data-theme', t);
        const icon = document.querySelector('#darkToggle i');
        if (icon) icon.className = t === 'dark' ? 'bi bi-sun' : 'bi bi-moon-stars';
    }
    applyTheme(localStorage.getItem(themeKey) || 'light');
    document.addEventListener('click', e => {
        if (e.target.closest('#darkToggle')) {
            const cur = document.documentElement.getAttribute('data-theme') === 'dark' ? 'light' : 'dark';
            localStorage.setItem(themeKey, cur);
            applyTheme(cur);
        }
    });

    /* ---------- Cart drawer ---------- */
    const drawer = document.getElementById('cartDrawer');
    const backdrop = document.getElementById('cartBackdrop');
    function openDrawer() {
        if (!drawer) return;
        renderCart();
        drawer.classList.add('open');
        backdrop?.classList.add('show');
        document.body.style.overflow = 'hidden';
    }
    function closeDrawer() {
        drawer?.classList.remove('open');
        backdrop?.classList.remove('show');
        document.body.style.overflow = '';
    }
    document.addEventListener('click', e => {
        if (e.target.closest('#cartOpen')) { e.preventDefault(); openDrawer(); }
        if (e.target.closest('#cartClose') || e.target === backdrop) closeDrawer();
    });

    /* ---------- Cart API ---------- */
    function getToken() {
        const el = document.querySelector('input[name="__RequestVerificationToken"]');
        return el ? el.value : '';
    }

    async function cartGet() {
        const r = await fetch('/Cart/Json');
        return r.ok ? r.json() : { items: [], totalQuantity: 0, subtotal: 0, shippingFee: 0, total: 0 };
    }

    async function cartPost(action, body) {
        const token = getToken();
        const form = new URLSearchParams(body);
        if (token) form.append('__RequestVerificationToken', token);
        const r = await fetch('/Cart/' + action, {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded', 'X-Requested-With': 'XMLHttpRequest' },
            body: form
        });
        return r.ok ? r.json() : null;
    }

    function updateBadge(cart) {
        const badge = document.getElementById('cartCount');
        if (!badge) return;
        if (cart.totalQuantity > 0) {
            badge.textContent = cart.totalQuantity;
            badge.style.display = '';
            badge.classList.remove('pulse');
            void badge.offsetWidth;
            badge.classList.add('pulse');
        } else {
            badge.style.display = 'none';
        }
    }

    function renderCart() {
        const list = document.getElementById('cartItems');
        if (!list) return;
        list.innerHTML = '<div class="text-center py-4"><span class="spinner"></span></div>';
        cartGet().then(cart => {
            updateBadge(cart);
            document.getElementById('cartSubtotal').textContent = VND(cart.subtotal);
            document.getElementById('cartShipping').textContent = cart.shippingFee === 0 ? 'Miễn phí' : VND(cart.shippingFee);
            document.getElementById('cartTotal').textContent = VND(cart.total);

            if (!cart.items.length) {
                list.innerHTML = `
                    <div class="empty-state">
                        <i class="bi bi-bag"></i>
                        <p class="mt-2">Giỏ hàng của bạn đang trống</p>
                        <a class="btn btn-soft" href="/Shop">Khám phá sản phẩm</a>
                    </div>`;
                return;
            }

            list.innerHTML = cart.items.map(it => `
                <div class="cart-line" data-id="${it.productId}">
                    <img src="${it.imageUrl || '/images/placeholder.svg'}" alt="" />
                    <div class="meta">
                        <div class="name">${it.name}</div>
                        <div class="price">${VND(it.price)}</div>
                        <div class="d-flex justify-content-between align-items-center mt-2">
                            <div class="qty-control">
                                <button data-action="dec"><i class="bi bi-dash"></i></button>
                                <input type="text" value="${it.quantity}" readonly />
                                <button data-action="inc"><i class="bi bi-plus"></i></button>
                            </div>
                            <button class="btn btn-sm text-danger" data-action="del"><i class="bi bi-trash"></i></button>
                        </div>
                    </div>
                </div>`).join('');
        });
    }

    document.addEventListener('click', async e => {
        const line = e.target.closest('.cart-line');
        if (!line) return;
        const btn = e.target.closest('button[data-action]');
        if (!btn) return;
        const id = parseInt(line.dataset.id, 10);
        const qtyInput = line.querySelector('.qty-control input');
        const cur = parseInt(qtyInput.value, 10);
        let cart = null;
        if (btn.dataset.action === 'inc') cart = await cartPost('Update', { productId: id, quantity: cur + 1 });
        else if (btn.dataset.action === 'dec') cart = await cartPost('Update', { productId: id, quantity: cur - 1 });
        else if (btn.dataset.action === 'del') cart = await cartPost('Remove', { productId: id });
        if (cart) { updateBadge(cart); renderCart(); }
    });

    /* ---------- Add to cart (delegate) ---------- */
    document.addEventListener('click', async e => {
        const btn = e.target.closest('[data-add-to-cart]');
        if (!btn) return;
        e.preventDefault();
        const id = parseInt(btn.dataset.addToCart, 10);
        const qty = parseInt(btn.dataset.qty || '1', 10);
        btn.disabled = true;
        const cart = await cartPost('Add', { productId: id, quantity: qty });
        btn.disabled = false;
        if (cart) {
            updateBadge(cart);
            toast('Đã thêm vào giỏ hàng', 'success');
            if (btn.dataset.openDrawer === '1') openDrawer();
        } else {
            toast('Không thể thêm sản phẩm', 'error');
        }
    });

    /* ---------- Buy now → add then go to checkout ---------- */
    document.addEventListener('click', async e => {
        const btn = e.target.closest('[data-buy-now]');
        if (!btn) return;
        e.preventDefault();
        const id = parseInt(btn.dataset.buyNow, 10);
        const qty = parseInt(btn.dataset.qty || '1', 10);
        const cart = await cartPost('Add', { productId: id, quantity: qty });
        if (cart) window.location.href = '/Checkout';
        else toast('Không thể đặt hàng', 'error');
    });

    /* ---------- Wishlist toggle ---------- */
    document.addEventListener('click', async e => {
        const btn = e.target.closest('[data-wishlist]');
        if (!btn) return;
        e.preventDefault();
        const id = parseInt(btn.dataset.wishlist, 10);
        const token = getToken();
        const r = await fetch('/Wishlist/Toggle', {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded', 'X-Requested-With': 'XMLHttpRequest' },
            body: new URLSearchParams({ productId: id, __RequestVerificationToken: token })
        });
        if (!r.ok) return toast('Không thể cập nhật yêu thích', 'error');
        const data = await r.json();
        btn.classList.toggle('active', data.added);
        const heart = btn.querySelector('i');
        if (heart) heart.className = data.added ? 'bi bi-heart-fill' : 'bi bi-heart';
        const badge = document.getElementById('wishlistCount');
        if (badge) {
            if (data.count > 0) { badge.textContent = data.count; badge.style.display = ''; }
            else badge.style.display = 'none';
        }
        toast(data.added ? 'Đã thêm vào yêu thích' : 'Đã bỏ khỏi yêu thích', 'success');
    });

    /* ---------- Quantity stepper (Cart, Detail) ---------- */
    document.addEventListener('click', e => {
        const inc = e.target.closest('[data-qty-inc]');
        const dec = e.target.closest('[data-qty-dec]');
        if (!inc && !dec) return;
        const target = (inc || dec).closest('.qty-stepper');
        if (!target) return;
        const input = target.querySelector('input');
        let v = parseInt(input.value, 10) || 1;
        v = inc ? v + 1 : Math.max(1, v - 1);
        input.value = v;
        input.dispatchEvent(new Event('change', { bubbles: true }));
    });

    /* ---------- Expose ---------- */
    window.TechStore = { toast, renderCart, openCart: openDrawer, closeCart: closeDrawer };

    /* ---------- Fade-up animations ---------- */
    document.querySelectorAll('[data-anim]').forEach((el, i) => {
        el.style.animationDelay = (i * 60) + 'ms';
        el.classList.add('fade-up');
    });
})();
