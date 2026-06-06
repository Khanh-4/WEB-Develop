import { Link } from 'react-router';
import { useCart } from '../context/CartContext';
import { Trash2, Plus, Minus, ShoppingBag, ArrowRight } from 'lucide-react';

export default function Cart() {
  const { cart, removeFromCart, updateQuantity, getTotal, clearCart } = useCart();

  if (cart.length === 0) {
    return (
      <div className="min-h-screen flex items-center justify-center px-4">
        <div className="text-center space-y-6">
          <div className="backdrop-blur-xl bg-white/10 rounded-full w-32 h-32 mx-auto flex items-center justify-center">
            <ShoppingBag className="w-16 h-16 text-white/50" />
          </div>
          <h2 className="text-3xl font-bold text-white">Your Cart is Empty</h2>
          <p className="text-white/70 text-lg">
            Start building your dream PC or browse our components
          </p>
          <div className="flex flex-col sm:flex-row gap-4 justify-center">
            <Link
              to="/builder"
              className="px-8 py-4 bg-gradient-to-r from-purple-500 to-blue-500 text-white rounded-xl hover:from-purple-600 hover:to-blue-600 transition-all"
            >
              PC Builder
            </Link>
            <Link
              to="/products"
              className="px-8 py-4 backdrop-blur-xl bg-white/10 text-white rounded-xl border border-white/20 hover:bg-white/20 transition-all"
            >
              Browse Products
            </Link>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen py-8 px-4">
      <div className="max-w-6xl mx-auto">
        <div className="flex items-center justify-between mb-8">
          <h1 className="text-4xl font-bold text-white">Shopping Cart</h1>
          <button
            onClick={clearCart}
            className="text-red-400 hover:text-red-300 transition-colors"
          >
            Clear Cart
          </button>
        </div>

        <div className="grid lg:grid-cols-3 gap-6">
          {/* Cart Items */}
          <div className="lg:col-span-2 space-y-4">
            {cart.map((item) => (
              <div
                key={item.id}
                className="backdrop-blur-xl bg-white/10 rounded-2xl border border-white/20 p-6"
              >
                <div className="flex gap-6">
                  {/* Image */}
                  <div className="w-32 h-32 bg-white/5 rounded-xl overflow-hidden flex-shrink-0">
                    <img
                      src={item.image}
                      alt={item.name}
                      className="w-full h-full object-cover"
                    />
                  </div>

                  {/* Details */}
                  <div className="flex-1 min-w-0">
                    <div className="flex items-start justify-between gap-4 mb-2">
                      <div>
                        <span className="text-xs text-white/50 uppercase">
                          {item.category}
                        </span>
                        <h3 className="text-lg text-white font-medium">
                          {item.name}
                        </h3>
                      </div>
                      <button
                        onClick={() => removeFromCart(item.id)}
                        className="text-red-400 hover:text-red-300 transition-colors"
                      >
                        <Trash2 className="w-5 h-5" />
                      </button>
                    </div>

                    <div className="flex items-center justify-between mt-4">
                      {/* Quantity Controls */}
                      <div className="flex items-center gap-3">
                        <button
                          onClick={() => updateQuantity(item.id, item.quantity - 1)}
                          className="w-8 h-8 bg-white/10 rounded-lg hover:bg-white/20 transition-all flex items-center justify-center text-white"
                        >
                          <Minus className="w-4 h-4" />
                        </button>
                        <span className="text-white font-semibold w-8 text-center">
                          {item.quantity}
                        </span>
                        <button
                          onClick={() => updateQuantity(item.id, item.quantity + 1)}
                          className="w-8 h-8 bg-white/10 rounded-lg hover:bg-white/20 transition-all flex items-center justify-center text-white"
                        >
                          <Plus className="w-4 h-4" />
                        </button>
                      </div>

                      {/* Price */}
                      <div className="text-right">
                        <div className="text-2xl font-bold text-white">
                          ${(item.price * item.quantity).toFixed(2)}
                        </div>
                        {item.quantity > 1 && (
                          <div className="text-sm text-white/50">
                            ${item.price} each
                          </div>
                        )}
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            ))}
          </div>

          {/* Order Summary */}
          <div className="lg:col-span-1">
            <div className="backdrop-blur-xl bg-white/10 rounded-2xl border border-white/20 p-6 sticky top-24 space-y-6">
              <h2 className="text-2xl font-semibold text-white">Order Summary</h2>

              <div className="space-y-3">
                <div className="flex justify-between text-white/70">
                  <span>Subtotal</span>
                  <span>${getTotal().toFixed(2)}</span>
                </div>
                <div className="flex justify-between text-white/70">
                  <span>Shipping</span>
                  <span className="text-green-400">FREE</span>
                </div>
                <div className="flex justify-between text-white/70">
                  <span>Tax (estimated)</span>
                  <span>${(getTotal() * 0.08).toFixed(2)}</span>
                </div>
                <div className="h-px bg-white/20" />
                <div className="flex justify-between text-white">
                  <span className="text-xl">Total</span>
                  <span className="text-3xl font-bold">
                    ${(getTotal() * 1.08).toFixed(2)}
                  </span>
                </div>
              </div>

              <Link
                to="/checkout"
                className="w-full flex items-center justify-center gap-2 px-6 py-4 bg-gradient-to-r from-purple-500 to-blue-500 text-white rounded-xl hover:from-purple-600 hover:to-blue-600 transition-all shadow-lg hover:shadow-xl"
              >
                Proceed to Checkout
                <ArrowRight className="w-5 h-5" />
              </Link>

              <Link
                to="/products"
                className="block text-center text-white/70 hover:text-white transition-colors"
              >
                Continue Shopping
              </Link>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
