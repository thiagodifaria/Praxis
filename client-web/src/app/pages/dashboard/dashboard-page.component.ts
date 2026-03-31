import { CommonModule } from '@angular/common';
import { Component, computed, effect, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { AuthStore } from '../../core/auth.store';
import { DashboardSnapshot } from '../../core/app.models';
import { alertSeverityLabels, alertStatusLabels, formatCurrency, formatDateTime, formatStatus, invoiceStatusLabels, salesOrderStatusLabels, toneForStatus } from '../../shared/ui.helpers';

interface DashboardBar {
  height: number;
}

interface DashboardActivityItem {
  key: string;
  title: string;
  subtitle: string;
  value: string;
  timestamp: number;
  dateLabel: string;
  tone: string;
}

interface DashboardFocusMetric {
  label: string;
  value: string;
  progress: number;
}

interface DashboardBranchPulse {
  branchName: string;
  context: string;
  value: string;
  progress: number;
}

interface DashboardActionCard {
  title: string;
  description: string;
  route: string;
  cta: string;
}

@Component({
  selector: 'app-dashboard-page',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <section class="page dashboard-page" *ngIf="snapshot() as vm">
      <header class="page-header">
        <div>
          <p class="eyebrow">Painel geral</p>
          <h1>Dashboard operacional</h1>
          <p class="page-subtitle">
            Visao consolidada da operacao comercial, financeira e logistica do Praxis.
          </p>
        </div>

        <span class="badge tone-info">Atualizado em {{ formatDateTime(vm.generatedAtUtc) }}</span>
      </header>

      <section class="grid metrics">
        <article class="metric-card">
          <span class="metric-label">Pipeline de pedidos</span>
          <strong class="metric-value">{{ formatCurrency(vm.orderPipelineAmount) }}</strong>
          <span class="metric-footnote">{{ vm.draftOrders }} em rascunho - {{ vm.approvedOrders }} aprovados</span>
        </article>
        <article class="metric-card">
          <span class="metric-label">Recebiveis em aberto</span>
          <strong class="metric-value">{{ formatCurrency(vm.openReceivablesAmount) }}</strong>
          <span class="metric-footnote">{{ formatCurrency(vm.overdueReceivablesAmount) }} em atraso</span>
        </article>
        <article class="metric-card">
          <span class="metric-label">Pagamentos em aberto</span>
          <strong class="metric-value">{{ formatCurrency(vm.openPayablesAmount) }}</strong>
          <span class="metric-footnote">{{ vm.approvedPurchaseOrders }} compras em fluxo</span>
        </article>
        <article class="metric-card">
          <span class="metric-label">Governanca</span>
          <strong class="metric-value">{{ vm.pendingApprovals }}</strong>
          <span class="metric-footnote">{{ vm.unreadNotifications }} notificacoes nao lidas</span>
        </article>
      </section>

      <section class="dashboard-grid">
        <div class="dashboard-column dashboard-column--wide">
          <section class="panel">
            <div class="section-title compact">
              <div>
                <p class="eyebrow">Atividade</p>
                <h3>Resumo de atividade</h3>
              </div>
            </div>

            <div class="chart-bars" aria-hidden="true">
              <div class="chart-bars__item" *ngFor="let bar of heroBars()" [style.height.%]="bar.height"></div>
            </div>

            <div class="kpi-strip compact-strip">
              <article class="kpi-chip">
                <small>Clientes ativos</small>
                <strong>{{ vm.activeCustomers }}</strong>
              </article>
              <article class="kpi-chip">
                <small>Produtos ativos</small>
                <strong>{{ vm.activeProducts }}</strong>
              </article>
              <article class="kpi-chip">
                <small>Faturas emitidas</small>
                <strong>{{ vm.issuedInvoices }}</strong>
              </article>
              <article class="kpi-chip">
                <small>Baixo estoque</small>
                <strong>{{ vm.lowStockProducts }}</strong>
              </article>
            </div>
          </section>

          <section class="panel">
            <div class="section-title compact">
              <div>
                <p class="eyebrow">Vendas</p>
                <h3>Pedidos recentes</h3>
              </div>
              <a class="button button-ghost" routerLink="/sales">Abrir vendas</a>
            </div>

            <div class="table-shell">
              <table>
                <thead>
                  <tr>
                    <th>Pedido</th>
                    <th>Cliente</th>
                    <th>Status</th>
                    <th>Total</th>
                    <th>Criado</th>
                  </tr>
                </thead>
                <tbody>
                  <tr *ngFor="let item of vm.recentOrders">
                    <td>{{ item.orderNumber }}</td>
                    <td>{{ item.customerName }}</td>
                    <td>
                      <span class="badge" [ngClass]="toneForStatus(item.status, salesOrderStatusLabels)">
                        {{ formatStatus(item.status, salesOrderStatusLabels) }}
                      </span>
                    </td>
                    <td>{{ formatCurrency(item.totalAmount) }}</td>
                    <td>{{ formatDateTime(item.createdAtUtc) }}</td>
                  </tr>
                </tbody>
              </table>
            </div>
          </section>

          <section class="panel">
            <div class="section-title compact">
              <div>
                <p class="eyebrow">Faturamento</p>
                <h3>Faturas recentes</h3>
              </div>
              <a class="button button-ghost" routerLink="/billing">Abrir faturamento</a>
            </div>

            <div class="table-shell">
              <table>
                <thead>
                  <tr>
                    <th>Fatura</th>
                    <th>Cliente</th>
                    <th>Status</th>
                    <th>Total</th>
                    <th>Emissao</th>
                  </tr>
                </thead>
                <tbody>
                  <tr *ngFor="let item of vm.recentInvoices">
                    <td>{{ item.invoiceNumber }}</td>
                    <td>{{ item.customerName }}</td>
                    <td>
                      <span class="badge" [ngClass]="toneForStatus(item.status, invoiceStatusLabels)">
                        {{ formatStatus(item.status, invoiceStatusLabels) }}
                      </span>
                    </td>
                    <td>{{ formatCurrency(item.totalAmount) }}</td>
                    <td>{{ formatDateTime(item.issuedAtUtc) }}</td>
                  </tr>
                </tbody>
              </table>
            </div>
          </section>
        </div>

        <div class="dashboard-column">
          <section class="panel">
            <div class="section-title compact">
              <div>
                <p class="eyebrow">Movimentacao</p>
                <h3>Fluxo recente</h3>
              </div>
            </div>

            <div class="activity-list">
              <article class="activity-item" *ngFor="let item of activityStream()">
                <div class="activity-item__content">
                  <strong>{{ item.title }}</strong>
                  <p>{{ item.subtitle }}</p>
                </div>
                <div class="activity-item__meta">
                  <span class="badge" [ngClass]="item.tone">{{ item.value }}</span>
                  <small>{{ item.dateLabel }}</small>
                </div>
              </article>
              <p class="empty-state" *ngIf="!activityStream().length">
                Nenhum evento operacional recente para a filial atual.
              </p>
            </div>
          </section>

          <section class="panel">
            <div class="section-title compact">
              <div>
                <p class="eyebrow">Acompanhamento</p>
                <h3>Pontos de atencao</h3>
              </div>
            </div>

            <div class="focus-list">
              <article class="focus-item" *ngFor="let item of focusMetrics()">
                <div class="focus-item__copy">
                  <strong>{{ item.label }}</strong>
                  <span>{{ item.value }}</span>
                </div>
                <div class="focus-item__bar">
                  <span [style.width.%]="item.progress"></span>
                </div>
              </article>
            </div>
          </section>

          <section class="panel">
            <div class="section-title compact">
              <div>
                <p class="eyebrow">Filiais</p>
                <h3>Leitura por filial</h3>
              </div>
            </div>

            <div class="branch-list">
              <article class="branch-item" *ngFor="let branch of branchPulse()">
                <div class="branch-item__copy">
                  <strong>{{ branch.branchName }}</strong>
                  <p>{{ branch.context }}</p>
                </div>
                <div class="focus-item__bar">
                  <span [style.width.%]="branch.progress"></span>
                </div>
                <small>{{ branch.value }}</small>
              </article>
            </div>
          </section>

          <section class="panel">
            <div class="section-title compact">
              <div>
                <p class="eyebrow">Estoque</p>
                <h3>Itens em risco</h3>
              </div>
            </div>

            <div class="stack">
              <article class="list-card" *ngFor="let item of vm.lowStockItems.slice(0, 4)">
                <strong>{{ item.productName }}</strong>
                <p>{{ item.productSku }} - {{ item.warehouseName }}</p>
                <small class="warning-copy">Disponivel: {{ item.availableQuantity }} - Reorder: {{ item.reorderLevel }}</small>
              </article>
              <p class="empty-state" *ngIf="!vm.lowStockItems.length">
                Nenhum item abaixo do reorder level.
              </p>
            </div>
          </section>

          <section class="panel">
            <div class="section-title compact">
              <div>
                <p class="eyebrow">Alertas</p>
                <h3>Fila critica</h3>
              </div>
            </div>

            <div class="stack">
              <article class="list-card" *ngFor="let item of vm.openAlerts.slice(0, 3)">
                <div class="toolbar">
                  <strong>{{ item.code }}</strong>
                  <span class="badge" [ngClass]="toneForStatus(item.severity, alertSeverityLabels)">
                    {{ formatStatus(item.severity, alertSeverityLabels) }}
                  </span>
                </div>
                <p>{{ item.title }}</p>
                <small>{{ formatDateTime(item.createdAtUtc) }}</small>
              </article>
              <p class="empty-state" *ngIf="!vm.openAlerts.length">
                Nenhum alerta aberto no momento.
              </p>
            </div>
          </section>

          <section class="panel next-action">
            <p class="eyebrow">Proxima acao</p>
            <h3>{{ actionCard().title }}</h3>
            <p>{{ actionCard().description }}</p>
            <a class="button" [routerLink]="actionCard().route">{{ actionCard().cta }}</a>
          </section>
        </div>
      </section>
    </section>
  `,
  styles: [`
    .dashboard-grid {
      display: grid;
      gap: 1rem;
      grid-template-columns: minmax(0, 1.6fr) minmax(320px, 1fr);
      align-items: start;
    }

    .dashboard-column {
      display: grid;
      gap: 1rem;
    }

    .chart-bars {
      display: grid;
      grid-template-columns: repeat(12, minmax(10px, 1fr));
      gap: 0.5rem;
      align-items: end;
      min-height: 9rem;
      margin-bottom: 1rem;
    }

    .chart-bars__item {
      min-height: 1.25rem;
      border-radius: 0.5rem 0.5rem 0 0;
      background: linear-gradient(180deg, #8ea8ff 0%, #598bff 100%);
    }

    .compact-strip {
      gap: 0.65rem;
    }

    .activity-list,
    .focus-list,
    .branch-list {
      display: grid;
      gap: 0.75rem;
    }

    .activity-item {
      display: flex;
      align-items: flex-start;
      justify-content: space-between;
      gap: 0.75rem;
      padding: 0.75rem 0;
      border-bottom: 1px solid var(--praxis-border);
    }

    .activity-item:last-child {
      border-bottom: none;
      padding-bottom: 0;
    }

    .activity-item__content strong,
    .branch-item__copy strong,
    .next-action h3 {
      color: var(--praxis-heading);
      font-size: 0.88rem;
      font-weight: 600;
    }

    .activity-item__content p,
    .branch-item__copy p,
    .next-action p {
      margin: 0.2rem 0 0;
      color: var(--praxis-muted);
      font-size: 0.76rem;
      line-height: 1.55;
    }

    .activity-item__meta {
      display: grid;
      gap: 0.25rem;
      justify-items: end;
    }

    .activity-item__meta small,
    .branch-item small {
      color: var(--praxis-muted);
      font-size: 0.72rem;
    }

    .focus-item {
      display: grid;
      gap: 0.35rem;
    }

    .focus-item__copy {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 0.75rem;
    }

    .focus-item__copy strong,
    .focus-item__copy span {
      font-size: 0.78rem;
    }

    .focus-item__copy span {
      color: var(--praxis-muted);
    }

    .focus-item__bar {
      height: 0.35rem;
      border-radius: 999px;
      background: var(--praxis-surface-muted);
      overflow: hidden;
    }

    .focus-item__bar span {
      display: block;
      height: 100%;
      border-radius: inherit;
      background: var(--praxis-accent);
    }

    .branch-item {
      display: grid;
      gap: 0.4rem;
      padding: 0.85rem;
      border-radius: 0.75rem;
      background: var(--praxis-surface-strong);
      border: 1px solid var(--praxis-border);
    }

    .next-action p {
      margin-bottom: 0.9rem;
    }

    @media (max-width: 1100px) {
      .dashboard-grid {
        grid-template-columns: 1fr;
      }
    }
  `],
})
export class DashboardPageComponent {
  private readonly api = inject(ApiService);
  private readonly auth = inject(AuthStore);

  readonly snapshot = signal<DashboardSnapshot | null>(null);
  readonly heroBars = computed<DashboardBar[]>(() => {
    const vm = this.snapshot();
    if (!vm) {
      return [];
    }

    const values = [
      vm.activeCustomers + 1,
      vm.activeProducts + 1,
      vm.draftOrders + 1,
      vm.approvedOrders + 1,
      vm.approvedPurchaseOrders + 1,
      vm.issuedInvoices + 1,
      vm.lowStockProducts + 1,
      vm.pendingApprovals + 1,
      vm.unreadNotifications + 1,
      vm.recentOrders.length + 1,
      vm.recentInvoices.length + 1,
      vm.branches.length + 1,
    ];
    const highest = Math.max(...values, 1);

    return values.map((value) => ({
      height: Math.max(24, Math.round((value / highest) * 100)),
    }));
  });
  readonly activityStream = computed<DashboardActivityItem[]>(() => {
    const vm = this.snapshot();
    if (!vm) {
      return [];
    }

    const orders = vm.recentOrders.map((item) => ({
      key: `order-${item.orderNumber}`,
      title: item.customerName,
        subtitle: `${item.orderNumber} - ${this.formatStatus(item.status, this.salesOrderStatusLabels)}`,
      value: this.formatCurrency(item.totalAmount),
      timestamp: new Date(item.createdAtUtc).getTime(),
      dateLabel: this.formatDateTime(item.createdAtUtc),
      tone: this.toneForStatus(item.status, this.salesOrderStatusLabels),
    }));

    const invoices = vm.recentInvoices.map((item) => ({
      key: `invoice-${item.invoiceNumber}`,
      title: item.customerName,
        subtitle: `${item.invoiceNumber} - ${this.formatStatus(item.status, this.invoiceStatusLabels)}`,
      value: this.formatCurrency(item.totalAmount),
      timestamp: new Date(item.issuedAtUtc).getTime(),
      dateLabel: this.formatDateTime(item.issuedAtUtc),
      tone: this.toneForStatus(item.status, this.invoiceStatusLabels),
    }));

    const movements = vm.recentMovements.map((item, index) => ({
      key: `movement-${item.productSku}-${index}`,
      title: item.productName,
        subtitle: `${item.reason} - ${item.warehouseName}`,
      value: `${item.quantity} un`,
      timestamp: new Date(item.createdAtUtc).getTime(),
      dateLabel: this.formatDateTime(item.createdAtUtc),
      tone: 'tone-neutral',
    }));

    return [...orders, ...invoices, ...movements]
      .sort((left, right) => right.timestamp - left.timestamp)
      .slice(0, 7);
  });
  readonly focusMetrics = computed<DashboardFocusMetric[]>(() => {
    const vm = this.snapshot();
    if (!vm) {
      return [];
    }

    const metrics = [
      { label: 'Pedidos em draft', raw: vm.draftOrders, value: `${vm.draftOrders}` },
      { label: 'Pedidos aprovados', raw: vm.approvedOrders, value: `${vm.approvedOrders}` },
      { label: 'Compras abertas', raw: vm.approvedPurchaseOrders, value: `${vm.approvedPurchaseOrders}` },
      { label: 'Aprovacoes pendentes', raw: vm.pendingApprovals, value: `${vm.pendingApprovals}` },
      { label: 'Itens em risco', raw: vm.lowStockProducts, value: `${vm.lowStockProducts}` },
    ];
    const highest = Math.max(...metrics.map((item) => item.raw), 1);

    return metrics.map((item) => ({
      label: item.label,
      value: item.value,
      progress: Math.max(8, Math.round((item.raw / highest) * 100)),
    }));
  });
  readonly branchPulse = computed<DashboardBranchPulse[]>(() => {
    const vm = this.snapshot();
    if (!vm) {
      return [];
    }

    const highest = Math.max(...vm.branches.map((item) => item.openReceivablesAmount), 1);

    return vm.branches.map((item) => ({
      branchName: item.branchName,
      context: `${item.activeOrders} pedidos ativos - ${item.activePurchaseOrders} compras abertas - ${item.openAlerts} alertas`,
      value: this.formatCurrency(item.openReceivablesAmount),
      progress: Math.max(10, Math.round((item.openReceivablesAmount / highest) * 100)),
    }));
  });
  readonly actionCard = computed<DashboardActionCard>(() => {
    const vm = this.snapshot();
    if (!vm) {
      return {
        title: 'Abrir operacoes',
        description: 'Acesse o painel operacional para acompanhar a esteira principal do workspace.',
        route: '/operations',
        cta: 'Abrir operacoes',
      };
    }

    if (vm.pendingApprovals > 0) {
      return {
        title: 'Aprovacoes pendentes',
        description: `${vm.pendingApprovals} itens aguardam decisao e merecem revisao no fluxo operacional.`,
        route: '/operations',
        cta: 'Revisar aprovacoes',
      };
    }

    if (vm.lowStockProducts > 0) {
      return {
        title: 'Reposicao prioritaria',
        description: `${vm.lowStockProducts} itens estao abaixo do reorder level e precisam de acompanhamento.`,
        route: '/inventory',
        cta: 'Abrir estoque',
      };
    }

    if (vm.overdueReceivablesAmount > 0) {
      return {
        title: 'Recebimentos em atraso',
        description: `${this.formatCurrency(vm.overdueReceivablesAmount)} seguem em atraso no contas a receber.`,
        route: '/billing',
        cta: 'Ver faturamento',
      };
    }

    return {
      title: 'Operacao estabilizada',
      description: 'Sem gargalos urgentes no momento. Vale revisar margem, giro e exposicao financeira.',
      route: '/reporting',
      cta: 'Abrir relatorios',
    };
  });
  readonly formatCurrency = formatCurrency;
  readonly formatDateTime = formatDateTime;
  readonly formatStatus = formatStatus;
  readonly toneForStatus = toneForStatus;
  readonly salesOrderStatusLabels = salesOrderStatusLabels;
  readonly alertSeverityLabels = alertSeverityLabels;
  readonly alertStatusLabels = alertStatusLabels;
  readonly invoiceStatusLabels = invoiceStatusLabels;

  constructor() {
    effect(() => {
      void this.load(this.auth.activeBranchId());
    });
  }

  private async load(branchId?: string | null): Promise<void> {
    this.snapshot.set(await firstValueFrom(this.api.getDashboard(branchId, true)));
  }
}
