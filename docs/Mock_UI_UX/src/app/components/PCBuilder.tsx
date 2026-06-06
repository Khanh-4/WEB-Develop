import { useState } from 'react';
import { products, Product } from '../data/products';
import {
  Cpu,
  CircuitBoard,
  HardDrive,
  Box,
  Zap,
  Wind,
  ShoppingCart,
  AlertCircle,
  CheckCircle,
  TrendingUp,
} from 'lucide-react';
import { useCart } from '../context/CartContext';

interface BuildComponent {
  category: Product['category'];
  product: Product | null;
}

const buildSteps: Array<{
  category: Product['category'];
  label: string;
  icon: React.ReactNode;
  required: boolean;
}> = [
  { category: 'cpu', label: 'Processor', icon: <Cpu />, required: true },
  { category: 'motherboard', label: 'Motherboard', icon: <CircuitBoard />, required: true },
  { category: 'ram', label: 'Memory', icon: <HardDrive />, required: true },
  { category: 'gpu', label: 'Graphics Card', icon: <Box />, required: true },
  { category: 'storage', label: 'Storage', icon: <HardDrive />, required: true },
  { category: 'psu', label: 'Power Supply', icon: <Zap />, required: true },
  { category: 'case', label: 'Case', icon: <Box />, required: true },
  { category: 'cooling', label: 'Cooling', icon: <Wind />, required: false },
];

export default function PCBuilder() {
  const [build, setBuild] = useState<Record<string, Product | null>>({});
  const [selectedCategory, setSelectedCategory] = useState<Product['category']>('cpu');
  const { addToCart } = useCart();

  const getCompatibleProducts = (category: Product['category']): Product[] => {
    const categoryProducts = products.filter((p) => p.category === category);

    return categoryProducts.filter((product) => {
      // Check CPU socket compatibility with motherboard
      if (category === 'motherboard' && build.cpu) {
        return product.compatibility.socket === build.cpu.compatibility.socket;
      }
      if (category === 'cpu' && build.motherboard) {
        return product.compatibility.socket === build.motherboard.compatibility.socket;
      }

      // Check RAM type compatibility
      if (category === 'ram' && build.motherboard) {
        return product.compatibility.ramType === build.motherboard.compatibility.ramType;
      }

      // Check GPU length compatibility with case
      if (category === 'gpu' && build.case) {
        return (
          !product.compatibility.gpuLength ||
          !build.case.compatibility.maxGpuLength ||
          product.compatibility.gpuLength <= build.case.compatibility.maxGpuLength
        );
      }
      if (category === 'case' && build.gpu) {
        return (
          !build.gpu.compatibility.gpuLength ||
          !product.compatibility.maxGpuLength ||
          build.gpu.compatibility.gpuLength <= product.compatibility.maxGpuLength
        );
      }

      // Check PSU wattage
      if (category === 'psu') {
        const totalWattage = getTotalWattage();
        return (product.compatibility.psuWattage || 0) >= totalWattage * 1.2; // 20% headroom
      }

      // Check form factor
      if (category === 'case' && build.motherboard) {
        return product.compatibility.formFactor === build.motherboard.compatibility.formFactor;
      }
      if (category === 'motherboard' && build.case) {
        return product.compatibility.formFactor === build.case.compatibility.formFactor;
      }

      return true;
    });
  };

  const getTotalWattage = (): number => {
    let total = 0;
    Object.values(build).forEach((product) => {
      if (product?.compatibility.requiredWattage) {
        total += product.compatibility.requiredWattage;
      }
    });
    return total;
  };

  const getTotalPrice = (): number => {
    return Object.values(build).reduce((sum, product) => sum + (product?.price || 0), 0);
  };

  const getPerformanceScore = (): number => {
    const products = Object.values(build).filter(Boolean) as Product[];
    if (products.length === 0) return 0;
    const avg = products.reduce((sum, p) => sum + p.performance, 0) / products.length;
    return Math.round(avg);
  };

  const getCompatibilityIssues = (): string[] => {
    const issues: string[] = [];

    // Check PSU wattage
    if (build.psu) {
      const totalWattage = getTotalWattage();
      const psuWattage = build.psu.compatibility.psuWattage || 0;
      if (psuWattage < totalWattage * 1.2) {
        issues.push(`PSU wattage (${psuWattage}W) may be insufficient for ${totalWattage}W system`);
      }
    }

    // Check GPU length
    if (build.gpu && build.case) {
      const gpuLength = build.gpu.compatibility.gpuLength || 0;
      const maxGpuLength = build.case.compatibility.maxGpuLength || 0;
      if (gpuLength > maxGpuLength) {
        issues.push(`GPU (${gpuLength}mm) is too long for case (max ${maxGpuLength}mm)`);
      }
    }

    return issues;
  };

  const handleSelectProduct = (product: Product) => {
    setBuild((prev) => ({ ...prev, [product.category]: product }));
  };

  const handleRemoveProduct = (category: string) => {
    setBuild((prev) => {
      const newBuild = { ...prev };
      delete newBuild[category];
      return newBuild;
    });
  };

  const handleAddAllToCart = () => {
    Object.values(build).forEach((product) => {
      if (product) {
        addToCart({
          id: product.id,
          name: product.name,
          price: product.price,
          image: product.image,
          category: product.category,
        });
      }
    });
  };

  const compatibleProducts = getCompatibleProducts(selectedCategory);
  const issues = getCompatibilityIssues();
  const isComplete = buildSteps.filter((s) => s.required).every((s) => build[s.category]);

  return (
    <div className="min-h-screen py-8 px-4">
      <div className="max-w-7xl mx-auto">
        <h1 className="text-4xl font-bold text-white mb-8">PC Builder</h1>

        <div className="grid lg:grid-cols-3 gap-6">
          {/* Build Summary */}
          <div className="lg:col-span-1 space-y-6">
            {/* Current Build */}
            <div className="backdrop-blur-xl bg-white/10 rounded-2xl border border-white/20 p-6 space-y-4">
              <h2 className="text-xl font-semibold text-white">Your Build</h2>

              {buildSteps.map((step) => {
                const product = build[step.category];
                return (
                  <div
                    key={step.category}
                    onClick={() => setSelectedCategory(step.category)}
                    className={`p-3 rounded-xl cursor-pointer transition-all ${
                      selectedCategory === step.category
                        ? 'bg-gradient-to-r from-purple-500/30 to-blue-500/30 border border-purple-500/50'
                        : 'bg-white/5 hover:bg-white/10'
                    }`}
                  >
                    <div className="flex items-center gap-3">
                      <div className="text-white/70">{step.icon}</div>
                      <div className="flex-1 min-w-0">
                        <div className="text-sm text-white/70">{step.label}</div>
                        <div className="text-white text-sm truncate">
                          {product ? product.name : 'Not selected'}
                        </div>
                      </div>
                      {product && (
                        <button
                          onClick={(e) => {
                            e.stopPropagation();
                            handleRemoveProduct(step.category);
                          }}
                          className="text-red-400 hover:text-red-300 text-sm"
                        >
                          Remove
                        </button>
                      )}
                    </div>
                    {product && (
                      <div className="text-white font-semibold mt-1">
                        ${product.price}
                      </div>
                    )}
                  </div>
                );
              })}
            </div>

            {/* Build Stats */}
            <div className="backdrop-blur-xl bg-white/10 rounded-2xl border border-white/20 p-6 space-y-4">
              <div className="flex justify-between items-center">
                <span className="text-white/70">Total Price</span>
                <span className="text-2xl font-bold text-white">
                  ${getTotalPrice()}
                </span>
              </div>
              <div className="flex justify-between items-center">
                <span className="text-white/70">Performance</span>
                <div className="flex items-center gap-2">
                  <div className="w-24 bg-white/10 rounded-full h-2">
                    <div
                      className="h-full bg-gradient-to-r from-purple-500 to-blue-500 rounded-full"
                      style={{ width: `${getPerformanceScore()}%` }}
                    />
                  </div>
                  <span className="text-white font-semibold">
                    {getPerformanceScore()}
                  </span>
                </div>
              </div>
              <div className="flex justify-between items-center">
                <span className="text-white/70">Power Draw</span>
                <span className="text-white font-semibold">
                  {getTotalWattage()}W
                </span>
              </div>
            </div>

            {/* Compatibility Status */}
            {issues.length > 0 && (
              <div className="backdrop-blur-xl bg-red-500/20 rounded-2xl border border-red-500/50 p-4 space-y-2">
                <div className="flex items-center gap-2 text-red-300">
                  <AlertCircle className="w-5 h-5" />
                  <span className="font-semibold">Compatibility Issues</span>
                </div>
                {issues.map((issue, i) => (
                  <p key={i} className="text-sm text-red-200">
                    {issue}
                  </p>
                ))}
              </div>
            )}

            {isComplete && issues.length === 0 && (
              <div className="backdrop-blur-xl bg-green-500/20 rounded-2xl border border-green-500/50 p-4">
                <div className="flex items-center gap-2 text-green-300 mb-3">
                  <CheckCircle className="w-5 h-5" />
                  <span className="font-semibold">Build Complete!</span>
                </div>
                <button
                  onClick={handleAddAllToCart}
                  className="w-full flex items-center justify-center gap-2 px-4 py-3 bg-gradient-to-r from-purple-500 to-blue-500 text-white rounded-xl hover:from-purple-600 hover:to-blue-600 transition-all"
                >
                  <ShoppingCart className="w-5 h-5" />
                  Add All to Cart
                </button>
              </div>
            )}
          </div>

          {/* Product Selection */}
          <div className="lg:col-span-2">
            <div className="backdrop-blur-xl bg-white/10 rounded-2xl border border-white/20 p-6">
              <div className="flex items-center gap-3 mb-6">
                <div className="text-white">
                  {buildSteps.find((s) => s.category === selectedCategory)?.icon}
                </div>
                <h2 className="text-2xl font-semibold text-white">
                  Select {buildSteps.find((s) => s.category === selectedCategory)?.label}
                </h2>
                <span className="text-white/70">
                  ({compatibleProducts.length} compatible)
                </span>
              </div>

              <div className="grid sm:grid-cols-2 gap-4">
                {compatibleProducts.map((product) => (
                  <button
                    key={product.id}
                    onClick={() => handleSelectProduct(product)}
                    className={`text-left backdrop-blur-xl bg-white/5 rounded-xl border transition-all hover:bg-white/10 p-4 ${
                      build[selectedCategory]?.id === product.id
                        ? 'border-purple-500 ring-2 ring-purple-500/50'
                        : 'border-white/20'
                    }`}
                  >
                    <div className="aspect-video bg-white/5 rounded-lg mb-3 overflow-hidden">
                      <img
                        src={product.image}
                        alt={product.name}
                        className="w-full h-full object-cover"
                      />
                    </div>
                    <h3 className="text-white mb-2 line-clamp-2">{product.name}</h3>
                    <div className="flex items-center justify-between">
                      <span className="text-xl font-bold text-white">
                        ${product.price}
                      </span>
                      <div className="flex items-center gap-1">
                        <TrendingUp className="w-4 h-4 text-purple-400" />
                        <span className="text-white/70 text-sm">
                          {product.performance}
                        </span>
                      </div>
                    </div>
                  </button>
                ))}
              </div>

              {compatibleProducts.length === 0 && (
                <div className="text-center py-12">
                  <AlertCircle className="w-12 h-12 text-white/50 mx-auto mb-4" />
                  <p className="text-white/70 text-lg">
                    No compatible {selectedCategory} found
                  </p>
                  <p className="text-white/50 text-sm mt-2">
                    Try changing your current selections
                  </p>
                </div>
              )}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
