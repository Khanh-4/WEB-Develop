import { useState } from 'react';
import { Link } from 'react-router';
import { products, Product } from '../data/products';
import { Search, ShoppingCart, Star } from 'lucide-react';
import { useCart } from '../context/CartContext';

const categories = [
  { value: 'all', label: 'All Components' },
  { value: 'cpu', label: 'Processors' },
  { value: 'motherboard', label: 'Motherboards' },
  { value: 'ram', label: 'Memory (RAM)' },
  { value: 'gpu', label: 'Graphics Cards' },
  { value: 'storage', label: 'Storage' },
  { value: 'psu', label: 'Power Supplies' },
  { value: 'case', label: 'Cases' },
  { value: 'cooling', label: 'Cooling' },
];

export default function ProductsPage() {
  const [selectedCategory, setSelectedCategory] = useState('all');
  const [searchQuery, setSearchQuery] = useState('');
  const [sortBy, setSortBy] = useState<'name' | 'price-low' | 'price-high' | 'performance'>('name');
  const { addToCart } = useCart();

  const filteredProducts = products
    .filter((p) => selectedCategory === 'all' || p.category === selectedCategory)
    .filter((p) =>
      p.name.toLowerCase().includes(searchQuery.toLowerCase())
    )
    .sort((a, b) => {
      switch (sortBy) {
        case 'price-low':
          return a.price - b.price;
        case 'price-high':
          return b.price - a.price;
        case 'performance':
          return b.performance - a.performance;
        default:
          return a.name.localeCompare(b.name);
      }
    });

  return (
    <div className="min-h-screen py-8 px-4">
      <div className="max-w-7xl mx-auto">
        <h1 className="text-4xl font-bold text-white mb-8">PC Components</h1>

        {/* Filters */}
        <div className="backdrop-blur-xl bg-white/10 rounded-2xl border border-white/20 p-6 mb-8 space-y-4">
          <div className="flex flex-col lg:flex-row gap-4">
            {/* Search */}
            <div className="flex-1 relative">
              <Search className="absolute left-4 top-1/2 -translate-y-1/2 text-white/50 w-5 h-5" />
              <input
                type="text"
                placeholder="Search components..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="w-full pl-12 pr-4 py-3 bg-white/5 border border-white/20 rounded-xl text-white placeholder:text-white/50 focus:outline-none focus:ring-2 focus:ring-purple-500"
              />
            </div>

            {/* Sort */}
            <select
              value={sortBy}
              onChange={(e) => setSortBy(e.target.value as any)}
              className="px-4 py-3 bg-white/5 border border-white/20 rounded-xl text-white focus:outline-none focus:ring-2 focus:ring-purple-500"
            >
              <option value="name">Sort by Name</option>
              <option value="price-low">Price: Low to High</option>
              <option value="price-high">Price: High to Low</option>
              <option value="performance">Performance</option>
            </select>
          </div>

          {/* Categories */}
          <div className="flex flex-wrap gap-2">
            {categories.map((cat) => (
              <button
                key={cat.value}
                onClick={() => setSelectedCategory(cat.value)}
                className={`px-4 py-2 rounded-lg transition-all ${
                  selectedCategory === cat.value
                    ? 'bg-gradient-to-r from-purple-500 to-blue-500 text-white'
                    : 'bg-white/5 text-white/70 hover:bg-white/10'
                }`}
              >
                {cat.label}
              </button>
            ))}
          </div>
        </div>

        {/* Products Grid */}
        <div className="grid sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
          {filteredProducts.map((product) => (
            <ProductCard key={product.id} product={product} onAddToCart={addToCart} />
          ))}
        </div>

        {filteredProducts.length === 0 && (
          <div className="text-center py-20">
            <p className="text-white/50 text-xl">No products found</p>
          </div>
        )}
      </div>
    </div>
  );
}

function ProductCard({
  product,
  onAddToCart,
}: {
  product: Product;
  onAddToCart: (item: any) => void;
}) {
  return (
    <div className="backdrop-blur-xl bg-white/10 rounded-2xl border border-white/20 overflow-hidden hover:bg-white/15 transition-all group">
      <Link to={`/products/${product.id}`}>
        <div className="aspect-square bg-white/5 overflow-hidden">
          <img
            src={product.image}
            alt={product.name}
            className="w-full h-full object-cover group-hover:scale-110 transition-transform duration-300"
          />
        </div>
      </Link>
      <div className="p-4 space-y-3">
        <div className="flex items-start justify-between gap-2">
          <Link to={`/products/${product.id}`} className="flex-1">
            <h3 className="text-white hover:text-purple-400 transition-colors line-clamp-2">
              {product.name}
            </h3>
          </Link>
          <div className="flex items-center gap-1 text-yellow-400">
            <Star className="w-4 h-4 fill-current" />
            <span className="text-sm">{(product.performance / 20).toFixed(1)}</span>
          </div>
        </div>
        <div className="flex items-center justify-between">
          <span className="text-2xl font-bold text-white">${product.price}</span>
          <button
            onClick={() =>
              onAddToCart({
                id: product.id,
                name: product.name,
                price: product.price,
                image: product.image,
                category: product.category,
              })
            }
            className="p-2 bg-gradient-to-r from-purple-500 to-blue-500 rounded-lg text-white hover:from-purple-600 hover:to-blue-600 transition-all hover:scale-110"
          >
            <ShoppingCart className="w-5 h-5" />
          </button>
        </div>
      </div>
    </div>
  );
}
