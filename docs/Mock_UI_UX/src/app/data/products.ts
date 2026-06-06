export interface Product {
  id: string;
  name: string;
  category: 'cpu' | 'motherboard' | 'ram' | 'gpu' | 'storage' | 'psu' | 'case' | 'cooling';
  price: number;
  image: string;
  specs: Record<string, string>;
  performance: number;
  compatibility: {
    socket?: string;
    chipset?: string;
    ramType?: string;
    ramSlots?: number;
    maxRamSpeed?: number;
    psuWattage?: number;
    requiredWattage?: number;
    formFactor?: string;
    maxGpuLength?: number;
    gpuLength?: number;
  };
}

export const products: Product[] = [
  // CPUs
  {
    id: 'cpu-1',
    name: 'AMD Ryzen 9 7950X',
    category: 'cpu',
    price: 549,
    image: 'https://images.unsplash.com/photo-1555680202-c0d1f8d84a96?w=400',
    specs: {
      'Cores': '16',
      'Threads': '32',
      'Base Clock': '4.5 GHz',
      'Boost Clock': '5.7 GHz',
      'TDP': '170W',
    },
    performance: 95,
    compatibility: {
      socket: 'AM5',
      requiredWattage: 170,
    },
  },
  {
    id: 'cpu-2',
    name: 'Intel Core i9-14900K',
    category: 'cpu',
    price: 589,
    image: 'https://images.unsplash.com/photo-1555680202-c0d1f8d84a96?w=400',
    specs: {
      'Cores': '24',
      'Threads': '32',
      'Base Clock': '3.2 GHz',
      'Boost Clock': '6.0 GHz',
      'TDP': '125W',
    },
    performance: 98,
    compatibility: {
      socket: 'LGA1700',
      requiredWattage: 125,
    },
  },
  {
    id: 'cpu-3',
    name: 'AMD Ryzen 7 7800X3D',
    category: 'cpu',
    price: 449,
    image: 'https://images.unsplash.com/photo-1555680202-c0d1f8d84a96?w=400',
    specs: {
      'Cores': '8',
      'Threads': '16',
      'Base Clock': '4.2 GHz',
      'Boost Clock': '5.0 GHz',
      'TDP': '120W',
    },
    performance: 92,
    compatibility: {
      socket: 'AM5',
      requiredWattage: 120,
    },
  },

  // Motherboards
  {
    id: 'mobo-1',
    name: 'ASUS ROG Strix X670E-E',
    category: 'motherboard',
    price: 499,
    image: 'https://images.unsplash.com/photo-1587202372634-32705e3bf49c?w=400',
    specs: {
      'Socket': 'AM5',
      'Chipset': 'X670E',
      'RAM Slots': '4',
      'Max RAM': '128GB',
      'Form Factor': 'ATX',
    },
    performance: 90,
    compatibility: {
      socket: 'AM5',
      chipset: 'X670E',
      ramType: 'DDR5',
      ramSlots: 4,
      maxRamSpeed: 6400,
      formFactor: 'ATX',
    },
  },
  {
    id: 'mobo-2',
    name: 'MSI MPG Z790 Carbon',
    category: 'motherboard',
    price: 469,
    image: 'https://images.unsplash.com/photo-1587202372634-32705e3bf49c?w=400',
    specs: {
      'Socket': 'LGA1700',
      'Chipset': 'Z790',
      'RAM Slots': '4',
      'Max RAM': '128GB',
      'Form Factor': 'ATX',
    },
    performance: 88,
    compatibility: {
      socket: 'LGA1700',
      chipset: 'Z790',
      ramType: 'DDR5',
      ramSlots: 4,
      maxRamSpeed: 7200,
      formFactor: 'ATX',
    },
  },

  // RAM
  {
    id: 'ram-1',
    name: 'G.Skill Trident Z5 RGB 32GB (2x16GB) DDR5-6000',
    category: 'ram',
    price: 159,
    image: 'https://images.unsplash.com/photo-1541348263662-e068662d82af?w=400',
    specs: {
      'Capacity': '32GB (2x16GB)',
      'Type': 'DDR5',
      'Speed': '6000 MHz',
      'CAS Latency': 'CL30',
    },
    performance: 85,
    compatibility: {
      ramType: 'DDR5',
      requiredWattage: 10,
    },
  },
  {
    id: 'ram-2',
    name: 'Corsair Vengeance 64GB (2x32GB) DDR5-5600',
    category: 'ram',
    price: 229,
    image: 'https://images.unsplash.com/photo-1541348263662-e068662d82af?w=400',
    specs: {
      'Capacity': '64GB (2x32GB)',
      'Type': 'DDR5',
      'Speed': '5600 MHz',
      'CAS Latency': 'CL36',
    },
    performance: 80,
    compatibility: {
      ramType: 'DDR5',
      requiredWattage: 12,
    },
  },

  // GPUs
  {
    id: 'gpu-1',
    name: 'NVIDIA RTX 4090',
    category: 'gpu',
    price: 1599,
    image: 'https://images.unsplash.com/photo-1591799264318-7e6ef8ddb7ea?w=400',
    specs: {
      'Memory': '24GB GDDR6X',
      'Boost Clock': '2.52 GHz',
      'TDP': '450W',
      'Length': '304mm',
    },
    performance: 100,
    compatibility: {
      requiredWattage: 450,
      gpuLength: 304,
    },
  },
  {
    id: 'gpu-2',
    name: 'AMD Radeon RX 7900 XTX',
    category: 'gpu',
    price: 999,
    image: 'https://images.unsplash.com/photo-1591799264318-7e6ef8ddb7ea?w=400',
    specs: {
      'Memory': '24GB GDDR6',
      'Boost Clock': '2.5 GHz',
      'TDP': '355W',
      'Length': '287mm',
    },
    performance: 92,
    compatibility: {
      requiredWattage: 355,
      gpuLength: 287,
    },
  },
  {
    id: 'gpu-3',
    name: 'NVIDIA RTX 4070 Ti',
    category: 'gpu',
    price: 799,
    image: 'https://images.unsplash.com/photo-1591799264318-7e6ef8ddb7ea?w=400',
    specs: {
      'Memory': '12GB GDDR6X',
      'Boost Clock': '2.61 GHz',
      'TDP': '285W',
      'Length': '267mm',
    },
    performance: 85,
    compatibility: {
      requiredWattage: 285,
      gpuLength: 267,
    },
  },

  // Storage
  {
    id: 'ssd-1',
    name: 'Samsung 990 PRO 2TB NVMe',
    category: 'storage',
    price: 189,
    image: 'https://images.unsplash.com/photo-1597872200969-2b65d56bd16b?w=400',
    specs: {
      'Capacity': '2TB',
      'Interface': 'PCIe 4.0 x4',
      'Read Speed': '7450 MB/s',
      'Write Speed': '6900 MB/s',
    },
    performance: 95,
    compatibility: {
      requiredWattage: 5,
    },
  },
  {
    id: 'ssd-2',
    name: 'WD Black SN850X 1TB NVMe',
    category: 'storage',
    price: 119,
    image: 'https://images.unsplash.com/photo-1597872200969-2b65d56bd16b?w=400',
    specs: {
      'Capacity': '1TB',
      'Interface': 'PCIe 4.0 x4',
      'Read Speed': '7300 MB/s',
      'Write Speed': '6300 MB/s',
    },
    performance: 90,
    compatibility: {
      requiredWattage: 5,
    },
  },

  // PSUs
  {
    id: 'psu-1',
    name: 'Corsair RM1000x 1000W 80+ Gold',
    category: 'psu',
    price: 199,
    image: 'https://images.unsplash.com/photo-1609592547853-aa318b8a0e5f?w=400',
    specs: {
      'Wattage': '1000W',
      'Efficiency': '80+ Gold',
      'Modular': 'Fully Modular',
    },
    performance: 90,
    compatibility: {
      psuWattage: 1000,
    },
  },
  {
    id: 'psu-2',
    name: 'Seasonic Focus GX-850 850W 80+ Gold',
    category: 'psu',
    price: 139,
    image: 'https://images.unsplash.com/photo-1609592547853-aa318b8a0e5f?w=400',
    specs: {
      'Wattage': '850W',
      'Efficiency': '80+ Gold',
      'Modular': 'Fully Modular',
    },
    performance: 88,
    compatibility: {
      psuWattage: 850,
    },
  },

  // Cases
  {
    id: 'case-1',
    name: 'Lian Li O11 Dynamic EVO',
    category: 'case',
    price: 179,
    image: 'https://images.unsplash.com/photo-1587202372583-49330a15584d?w=400',
    specs: {
      'Form Factor': 'Mid Tower ATX',
      'Max GPU Length': '420mm',
      'Material': 'Tempered Glass',
    },
    performance: 85,
    compatibility: {
      formFactor: 'ATX',
      maxGpuLength: 420,
    },
  },
  {
    id: 'case-2',
    name: 'NZXT H7 Flow',
    category: 'case',
    price: 129,
    image: 'https://images.unsplash.com/photo-1587202372583-49330a15584d?w=400',
    specs: {
      'Form Factor': 'Mid Tower ATX',
      'Max GPU Length': '400mm',
      'Material': 'Steel + Tempered Glass',
    },
    performance: 80,
    compatibility: {
      formFactor: 'ATX',
      maxGpuLength: 400,
    },
  },

  // Cooling
  {
    id: 'cool-1',
    name: 'NZXT Kraken Z73 RGB 360mm',
    category: 'cooling',
    price: 279,
    image: 'https://images.unsplash.com/photo-1587202372775-e229f172b9d7?w=400',
    specs: {
      'Type': 'AIO Liquid Cooler',
      'Radiator Size': '360mm',
      'Fan Speed': '500-2000 RPM',
    },
    performance: 92,
    compatibility: {
      requiredWattage: 15,
    },
  },
  {
    id: 'cool-2',
    name: 'Noctua NH-D15 chromax.black',
    category: 'cooling',
    price: 109,
    image: 'https://images.unsplash.com/photo-1587202372775-e229f172b9d7?w=400',
    specs: {
      'Type': 'Air Cooler',
      'Fan Size': '2x 140mm',
      'Height': '165mm',
    },
    performance: 88,
    compatibility: {
      requiredWattage: 5,
    },
  },
];
