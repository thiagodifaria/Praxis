export interface NavigationItem {
  label: string;
  route: string;
  moduleKey: string;
  accent: string;
  description: string;
}

export const NAVIGATION_ITEMS: NavigationItem[] = [
  { label: 'Dashboard', route: '/dashboard', moduleKey: 'dashboard', accent: 'var(--praxis-accent)', description: 'Visao operacional em tempo real' },
  { label: 'Catalogo', route: '/catalog', moduleKey: 'catalog', accent: 'var(--praxis-amber)', description: 'Produtos, categorias e fornecedores' },
  { label: 'Clientes', route: '/customers', moduleKey: 'customers', accent: 'var(--praxis-teal)', description: 'Carteira, cadastro e contexto comercial' },
  { label: 'Vendas', route: '/sales', moduleKey: 'sales', accent: 'var(--praxis-red)', description: 'Pedidos, aprovacao e expedicao' },
  { label: 'Compras', route: '/purchasing', moduleKey: 'purchasing', accent: 'var(--praxis-copper)', description: 'Compras, recebimento e reposicao' },
  { label: 'Estoque', route: '/inventory', moduleKey: 'inventory', accent: 'var(--praxis-ice)', description: 'Saldos, movimentos e ajustes' },
  { label: 'Faturamento', route: '/billing', moduleKey: 'billing', accent: 'var(--praxis-lime)', description: 'Faturas, recebiveis e payables' },
  { label: 'Relatorios', route: '/reporting', moduleKey: 'reporting', accent: 'var(--praxis-sand)', description: 'Margem, giro e exposicao financeira' },
  { label: 'Operacoes', route: '/operations', moduleKey: 'operations', accent: 'var(--praxis-rose)', description: 'Alertas, auditoria e fila de aprovacao' },
  { label: 'Configuracoes', route: '/settings', moduleKey: 'settings', accent: 'var(--praxis-steel)', description: 'Filiais, centros de custo e feature flags' },
];
