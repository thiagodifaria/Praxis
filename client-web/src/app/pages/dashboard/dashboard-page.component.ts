import { CommonModule } from '@angular/common';
import { Component, effect, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { AuthStore } from '../../core/auth.store';
import { DashboardSnapshot } from '../../core/app.models';
import { alertSeverityLabels, alertStatusLabels, formatCurrency, formatDateTime, formatStatus, salesOrderStatusLabels, toneForStatus, invoiceStatusLabels } from '../../shared/ui.helpers';

@Component({
  selector: 'app-dashboard-page',
  standalone: true,
  imports: [CommonModule],
  template: `
    <section class="page">
      <header class="page-header">
        <div>
          <p class="eyebrow">Command Center</p>
          <h1>Dashboard operacional</h1>
          <p class="page-subtitle">
            Visao consolidada da esteira comercial, financeira e logistica do Praxis, com
            foco naquilo que precisa de acao imediata.
          </p>
        </div>

        <span class="badge tone-info" *ngIf="snapshot()">
          Atualizado em {{ formatDateTime(snapshot()?.generatedAtUtc) }}
        </span>
      </header>

      <section class="grid metrics" *ngIf="snapshot() as vm">
        <article class="metric-card">
          <span class="metric-label">Pipeline de pedidos</span>
          <strong class="metric-value">{{ formatCurrency(vm.orderPipelineAmount) }}</strong>
          <span class="metric-footnote">{{ vm.draftOrders }} em draft · {{ vm.approvedOrders }} aprovados</span>
        </article>
        <article class="metric-card">
          <span class="metric-label">Recebiveis em aberto</span>
          <strong class="metric-value">{{ formatCurrency(vm.openReceivablesAmount) }}</strong>
          <span class="metric-footnote">{{ formatCurrency(vm.overdueReceivablesAmount) }} em atraso</span>
        </article>
        <article class="metric-card">
          <span class="metric-label">Payables em aberto</span>
          <strong class="metric-value">{{ formatCurrency(vm.openPayablesAmount) }}</strong>
          <span class="metric-footnote">{{ vm.approvedPurchaseOrders }} POs em fluxo</span>
        </article>
        <article class="metric-card">
          <span class="metric-label">Governanca</span>
          <strong class="metric-value">{{ vm.pendingApprovals }}</strong>
          <span class="metric-footnote">{{ vm.unreadNotifications }} notificacoes nao lidas</span>
        </article>
      </section>

      <section class="kpi-strip" *ngIf="snapshot() as vm">
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
      </section>

      <section class="two-column-layout" *ngIf="snapshot() as vm">
        <div class="stack">
          <section class="panel">
            <div class="section-title compact">
              <div>
                <p class="eyebrow">Sales</p>
                <h3>Pedidos recentes</h3>
              </div>
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
                <p class="eyebrow">Billing</p>
                <h3>Faturas recentes</h3>
              </div>
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

          <section class="panel">
            <div class="section-title compact">
              <div>
                <p class="eyebrow">Network</p>
                <h3>Performance por filial</h3>
              </div>
            </div>

            <div class="stack">
              <article class="list-card" *ngFor="let branch of vm.branches">
                <strong>{{ branch.branchName }}</strong>
                <p>
                  {{ branch.activeOrders }} pedidos ativos ·
                  {{ branch.activePurchaseOrders }} compras abertas ·
                  {{ formatCurrency(branch.openReceivablesAmount) }} em recebiveis.
                </p>
                <small>{{ branch.openAlerts }} alertas operacionais em aberto.</small>
              </article>
            </div>
          </section>
        </div>

        <div class="stack">
          <section class="panel">
            <div class="section-title compact">
              <div>
                <p class="eyebrow">Inventory</p>
                <h3>Itens em risco</h3>
              </div>
            </div>

            <div class="stack">
              <article class="list-card" *ngFor="let item of vm.lowStockItems">
                <strong>{{ item.productName }} <small>{{ item.productSku }}</small></strong>
                <p>{{ item.warehouseName }}</p>
                <small class="warning-copy">
                  Disponivel: {{ item.availableQuantity }} · Reorder level: {{ item.reorderLevel }}
                </small>
              </article>
              <p class="empty-state" *ngIf="!vm.lowStockItems.length">
                Nenhum item abaixo do reorder level.
              </p>
            </div>
          </section>

          <section class="panel">
            <div class="section-title compact">
              <div>
                <p class="eyebrow">Alerts</p>
                <h3>Fila de atencao</h3>
              </div>
            </div>

            <div class="stack">
              <article class="list-card" *ngFor="let item of vm.openAlerts">
                <div class="toolbar">
                  <strong>{{ item.code }}</strong>
                  <span class="badge" [ngClass]="toneForStatus(item.severity, alertSeverityLabels)">
                    {{ formatStatus(item.severity, alertSeverityLabels) }}
                  </span>
                  <span class="badge" [ngClass]="toneForStatus(item.status, alertStatusLabels)">
                    {{ formatStatus(item.status, alertStatusLabels) }}
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

          <section class="panel">
            <div class="section-title compact">
              <div>
                <p class="eyebrow">Flow</p>
                <h3>Movimentacoes recentes</h3>
              </div>
            </div>

            <div class="stack">
              <article class="list-card" *ngFor="let item of vm.recentMovements">
                <strong>{{ item.productName }} <small>{{ item.productSku }}</small></strong>
                <p>{{ item.reason }} · {{ item.quantity }} un · {{ item.warehouseName }}</p>
                <small>{{ formatDateTime(item.createdAtUtc) }}</small>
              </article>
              <p class="empty-state" *ngIf="!vm.recentMovements.length">
                Nenhuma movimentacao recente registrada.
              </p>
            </div>
          </section>
        </div>
      </section>
    </section>
  `,
})
export class DashboardPageComponent {
  private readonly api = inject(ApiService);
  private readonly auth = inject(AuthStore);

  readonly snapshot = signal<DashboardSnapshot | null>(null);
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
