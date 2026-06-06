import { useParams, Link } from 'react-router';
import { products } from '../data/products';
import { ArrowLeft, ShoppingCart, Star, TrendingUp } from 'lucide-react';
import { useCart } from '../context/CartContext';

export default function ProductDetail() {
  const { id } = useParams<{ id: string }>();
  const { addToCart } = useCart();
  const product = products.find((p) => p.id === id);

  if (!product) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="text-center space-y-4">
          <h2 className="text-3xl font-bold text-white">Product Not Found</h2>
          <Link
            to="/products"
            className="inline-block px-6 py-3 bg-gradient-to-r from-purple-500 to-blue-500 text-white rounded-xl hover:from-purple-600 hover:to-blue-600 transition-all"
          >
            Back to Products
          </Link>
        </div>
      </div>
    );
  }

  const handleAddToCart = () => {
    addToCart({
      id: product.id,
      name: product.name,
      price: product.price,
      image: product.image,
      category: product.category,
    });
  };

  return (
    <div className="min-h-screen py-8 px-4">
      <div className="max-w-7xl mx-auto">
        <Link
          to="/products"
          className="inline-flex items-center gap-2 text-white/70 hover:text-white transition-colors mb-6"
        >
          <ArrowLeft className="w-5 h-5" />
          Back to Products
        </Link>

        <div className="backdrop-blur-xl bg-white/10 rounded-3xl border border-white/20 overflow-hidden">
          <div className="grid lg:grid-cols-2 gap-8 p-8">
            {/* Image */}
            <div className="aspect-square bg-white/5 rounded-2xl overflow-hidden">
              <img
                src={product.image}
                alt={product.name}
                className="w-full h-full object-cover"
              />
            </div>

            {/* Details */}
            <div className="space-y-6">
              <div>
                <div className="flex items-center gap-2 mb-2">
                  <span className="px-3 py-1 bg-purple-500/20 text-purple-300 rounded-lg text-sm">
                    {product.category.toUpperCase()}
                  </span>
                  <div className="flex items-center gap-1 text-yellow-400">
                    <Star className="w-4 h-4 fill-current" />
                    <span>{(product.performance / 20).toFixed(1)}</span>
                  </div>
                </div>
                <h1 className="text-4xl font-bold text-white mb-4">
                  {product.name}
                </h1>
                <div className="flex items-baseline gap-4">
                  <span className="text-5xl font-bold text-white">
                    ${product.price}
                  </span>
                </div>
              </div>

              {/* Performance Score */}
              <div className="backdrop-blur-xl bg-white/5 rounded-2xl border border-white/20 p-4">
                <div className="flex items-center gap-2 mb-2">
                  <TrendingUp className="w-5 h-5 text-purple-400" />
                  <span className="text-white">Performance Score</span>
                </div>
                <div className="flex items-center gap-4">
                  <div className="flex-1 bg-white/10 rounded-full h-3 overflow-hidden">
                    <div
                      className="h-full bg-gradient-to-r from-purple-500 to-blue-500"
                      style={{ width: `${product.performance}%` }}
                    />
                  </div>
                  <span className="text-2xl font-bold text-white">
                    {product.performance}
                  </span>
                </div>
              </div>

              {/* Specifications */}
              <div className="space-y-3">
                <h3 className="text-xl font-semibold text-white">
                  Specifications
                </h3>
                <div className="space-y-2">
                  {Object.entries(product.specs).map(([key, value]) => (
                    <div
                      key={key}
                      className="flex justify-between py-2 border-b border-white/10"
                    >
                      <span className="text-white/70">{key}</span>
                      <span className="text-white font-medium">{value}</span>
                    </div>
                  ))}
                </div>
              </div>

              {/* Actions */}
              <div className="flex gap-4">
                <button
                  onClick={handleAddToCart}
                  className="flex-1 flex items-center justify-center gap-2 px-8 py-4 bg-gradient-to-r from-purple-500 to-blue-500 text-white rounded-xl hover:from-purple-600 hover:to-blue-600 transition-all shadow-lg hover:shadow-xl"
                >
                  <ShoppingCart className="w-5 h-5" />
                  Add to Cart
                </button>
                <Link
                  to="/builder"
                  className="px-8 py-4 backdrop-blur-xl bg-white/10 text-white rounded-xl border border-white/20 hover:bg-white/20 transition-all text-center"
                >
                  Use in Builder
                </Link>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
