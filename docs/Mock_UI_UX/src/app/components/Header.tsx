import { Link, useLocation } from 'react-router';
import { ShoppingCart, Cpu, Search, Menu } from 'lucide-react';
import { useCart } from '../context/CartContext';
import { useState } from 'react';

export default function Header() {
  const location = useLocation();
  const { getItemCount } = useCart();
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false);

  const isActive = (path: string) => location.pathname === path;

  return (
    <header className="sticky top-0 z-50 backdrop-blur-xl bg-white/10 border-b border-white/20 shadow-lg">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex items-center justify-between h-16">
          {/* Logo */}
          <Link to="/" className="flex items-center space-x-2 group">
            <div className="bg-gradient-to-br from-purple-500 to-blue-500 p-2 rounded-lg group-hover:scale-110 transition-transform">
              <Cpu className="w-6 h-6 text-white" />
            </div>
            <span className="text-xl font-bold text-white">TechSpecs</span>
          </Link>

          {/* Desktop Navigation */}
          <nav className="hidden md:flex items-center space-x-8">
            <Link
              to="/"
              className={`transition-colors ${
                isActive('/')
                  ? 'text-white'
                  : 'text-white/70 hover:text-white'
              }`}
            >
              Home
            </Link>
            <Link
              to="/products"
              className={`transition-colors ${
                isActive('/products')
                  ? 'text-white'
                  : 'text-white/70 hover:text-white'
              }`}
            >
              Products
            </Link>
            <Link
              to="/builder"
              className={`px-4 py-2 rounded-lg bg-gradient-to-r from-purple-500 to-blue-500 text-white hover:from-purple-600 hover:to-blue-600 transition-all ${
                isActive('/builder') ? 'ring-2 ring-white/50' : ''
              }`}
            >
              PC Builder
            </Link>
          </nav>

          {/* Right Actions */}
          <div className="flex items-center space-x-4">
            <button className="hidden md:block text-white/70 hover:text-white transition-colors">
              <Search className="w-5 h-5" />
            </button>
            <Link
              to="/cart"
              className="relative text-white/70 hover:text-white transition-colors"
            >
              <ShoppingCart className="w-5 h-5" />
              {getItemCount() > 0 && (
                <span className="absolute -top-2 -right-2 bg-red-500 text-white text-xs rounded-full w-5 h-5 flex items-center justify-center">
                  {getItemCount()}
                </span>
              )}
            </Link>
            <button
              className="md:hidden text-white"
              onClick={() => setMobileMenuOpen(!mobileMenuOpen)}
            >
              <Menu className="w-6 h-6" />
            </button>
          </div>
        </div>

        {/* Mobile Menu */}
        {mobileMenuOpen && (
          <div className="md:hidden py-4 space-y-2">
            <Link
              to="/"
              className={`block px-4 py-2 rounded-lg transition-colors ${
                isActive('/') ? 'bg-white/20 text-white' : 'text-white/70'
              }`}
              onClick={() => setMobileMenuOpen(false)}
            >
              Home
            </Link>
            <Link
              to="/products"
              className={`block px-4 py-2 rounded-lg transition-colors ${
                isActive('/products')
                  ? 'bg-white/20 text-white'
                  : 'text-white/70'
              }`}
              onClick={() => setMobileMenuOpen(false)}
            >
              Products
            </Link>
            <Link
              to="/builder"
              className={`block px-4 py-2 rounded-lg transition-colors ${
                isActive('/builder')
                  ? 'bg-white/20 text-white'
                  : 'text-white/70'
              }`}
              onClick={() => setMobileMenuOpen(false)}
            >
              PC Builder
            </Link>
          </div>
        )}
      </div>
    </header>
  );
}
