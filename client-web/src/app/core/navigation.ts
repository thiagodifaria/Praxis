export interface NavigationItem {
  label: string;
  route: string;
  moduleKey: string;
  accent: string;
  icon: string;
  description: string;
}

export const NAVIGATION_ITEMS: NavigationItem[] = [
  { label: 'Dashboard', route: '/dashboard', moduleKey: 'dashboard', accent: '#3f8cff', icon: 'DB', description: 'Visao operacional em tempo real' },
  { label: 'Catalogo', route: '/catalog', moduleKey: 'catalog', accent: '#ffb648', icon: 'CT', description: 'Produtos, categorias e fornecedores' },
  { label: 'Clientes', route: '/customers', moduleKey: 'customers', accent: '#31ba96', icon: 'CL', description: 'Carteira, cadastro e contexto comercial' },
  { label: 'Vendas', route: '/sales', moduleKey: 'sales', accent: '#e45b68', icon: 'VD', description: 'Pedidos, aprovacao e expedicao' },
  { label: 'Compras', route: '/purchasing', moduleKey: 'purchasing', accent: '#ff8a3d', icon: 'CP', description: 'Compras, recebimento e reposicao' },
  { label: 'Estoque', route: '/inventory', moduleKey: 'inventory', accent: '#48a7ff', icon: 'ET', description: 'Saldos, movimentos e ajustes' },
  { label: 'Faturamento', route: '/billing', moduleKey: 'billing', accent: '#5bbb55', icon: 'FT', description: 'Faturas, recebiveis e payables' },
  { label: 'Relatorios', route: '/reporting', moduleKey: 'reporting', accent: '#f3cf7a', icon: 'RL', description: 'Margem, giro e exposicao financeira' },
  { label: 'Operacoes', route: '/operations', moduleKey: 'operations', accent: '#c05ddd', icon: 'OP', description: 'Alertas, auditoria e fila de aprovacao' },
  { label: 'Configuracoes', route: '/settings', moduleKey: 'settings', accent: '#8e9bb2', icon: 'CF', description: 'Filiais, centros de custo e feature flags' },
];
