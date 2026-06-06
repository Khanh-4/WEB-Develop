import { Link } from 'react-router';
import { Cpu, Zap, Shield, TrendingUp } from 'lucide-react';

export default function HomePage() {
  return (
    <div className="min-h-screen">
      {/* Hero Section */}
      <section className="relative py-20 px-4 overflow-hidden">
        <div className="max-w-7xl mx-auto">
          <div className="text-center space-y-8">
            <h1 className="text-5xl md:text-7xl font-bold text-white">
              Build Your Dream PC
              <span className="block bg-gradient-to-r from-purple-400 to-blue-400 bg-clip-text text-transparent">
                With Confidence
              </span>
            </h1>
            <p className="text-xl text-white/80 max-w-2xl mx-auto">
              Advanced compatibility engine ensures every component works
              perfectly together. Get the best performance per dollar with our
              AI-powered recommendations.
            </p>
            <div className="flex flex-col sm:flex-row gap-4 justify-center">
              <Link
                to="/builder"
                className="px-8 py-4 bg-gradient-to-r from-purple-500 to-blue-500 text-white rounded-xl hover:from-purple-600 hover:to-blue-600 transition-all shadow-lg hover:shadow-xl hover:scale-105"
              >
                Start Building
              </Link>
              <Link
                to="/products"
                className="px-8 py-4 backdrop-blur-xl bg-white/10 text-white rounded-xl border border-white/20 hover:bg-white/20 transition-all"
              >
                Browse Components
              </Link>
            </div>
          </div>
        </div>
      </section>

      {/* Features */}
      <section className="py-20 px-4">
        <div className="max-w-7xl mx-auto">
          <div className="grid md:grid-cols-2 lg:grid-cols-4 gap-6">
            <FeatureCard
              icon={<Cpu className="w-8 h-8" />}
              title="Smart Compatibility"
              description="Real-time validation prevents incompatible parts from being selected together."
            />
            <FeatureCard
              icon={<Zap className="w-8 h-8" />}
              title="Performance Scoring"
              description="Know exactly how your build performs with our P/P rating system."
            />
            <FeatureCard
              icon={<Shield className="w-8 h-8" />}
              title="Quality Components"
              description="Curated selection of top-tier PC components from trusted brands."
            />
            <FeatureCard
              icon={<TrendingUp className="w-8 h-8" />}
              title="Price Tracking"
              description="Get the best deals with real-time pricing from multiple retailers."
            />
          </div>
        </div>
      </section>

      {/* CTA Section */}
      <section className="py-20 px-4">
        <div className="max-w-4xl mx-auto">
          <div className="backdrop-blur-xl bg-gradient-to-r from-purple-500/20 to-blue-500/20 rounded-3xl border border-white/20 p-12 text-center space-y-6">
            <h2 className="text-4xl font-bold text-white">
              Ready to Build Something Amazing?
            </h2>
            <p className="text-xl text-white/80">
              Join thousands of satisfied builders who trust TechSpecs for their
              custom PC needs.
            </p>
            <Link
              to="/builder"
              className="inline-block px-8 py-4 bg-white text-purple-600 rounded-xl hover:bg-white/90 transition-all shadow-lg hover:shadow-xl hover:scale-105"
            >
              Launch PC Builder
            </Link>
          </div>
        </div>
      </section>
    </div>
  );
}

function FeatureCard({
  icon,
  title,
  description,
}: {
  icon: React.ReactNode;
  title: string;
  description: string;
}) {
  return (
    <div className="backdrop-blur-xl bg-white/10 rounded-2xl border border-white/20 p-6 hover:bg-white/15 transition-all group">
      <div className="bg-gradient-to-br from-purple-500 to-blue-500 w-16 h-16 rounded-xl flex items-center justify-center text-white mb-4 group-hover:scale-110 transition-transform">
        {icon}
      </div>
      <h3 className="text-xl font-semibold text-white mb-2">{title}</h3>
      <p className="text-white/70">{description}</p>
    </div>
  );
}
