import { CommonModule } from '@angular/common';
import { Component, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { AuthStore } from '../../core/auth.store';
import { Branch, CostCenter, InventoryTurnoverReport, OverdueReceivablesReport, ReportingOverview } from '../../core/app.models';
import { formatCurrency, formatDateOnly } from '../../shared/ui.helpers';

@Component({
  selector: 'app-reporting-page',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <section class="page">
      <header class="page-header">
        <div>
          <p class="eyebrow">Reporting</p>
          <h1>Margem, giro e exposicao</h1>
          <p class="page-subtitle">
            Visao analitica da operacao para tomada de decisao, com foco em receita, custos, giro e inadimplencia.
          </p>
        </div>
      </header>

      <section class="panel">
        <div class="section-title compact">
          <div>
            <p class="eyebrow">Filters</p>
            <h3>Recorte analitico</h3>
          </div>
        </div>

        <form class="form-grid" (ngSubmit)="refresh()">
          <label class="field">
            <span>De</span>
            <input type="date" [(ngModel)]="filters.fromUtc" name="reportFromUtc" />
          </label>
          <label class="field">
            <span>Ate</span>
            <input type="date" [(ngModel)]="filters.toUtc" name="reportToUtc" />
          </label>
          <label class="field">
            <span>Filial</span>
            <select [(ngModel)]="filters.branchId" name="reportBranch">
              <option value="">Todas</option>
              <option *ngFor="let branch of branches()" [value]="branch.id">{{ branch.code }} · {{ branch.name }}</option>
            </select>
          </label>
          <label class="field">
            <span>Centro de custo</span>
            <select [(ngModel)]="filters.costCenterId" name="reportCostCenter">
              <option value="">Todos</option>
              <option *ngFor="let costCenter of filteredCostCenters()" [value]="costCenter.id">{{ costCenter.code }} · {{ costCenter.name }}</option>
            </select>
          </label>
          <div class="form-actions">
            <button class="button" type="submit">Atualizar relatorios</button>
          </div>
        </form>
      </section>

      <section class="grid metrics" *ngIf="overview() as vm">
        <article class="metric-card">
          <span class="metric-label">Receita bruta</span>
          <strong class="metric-value">{{ formatCurrency(vm.grossRevenue) }}</strong>
          <span class="metric-footnote">Margem: {{ formatCurrency(vm.grossMargin) }}</span>
        </article>
        <article class="metric-card">
          <span class="metric-label">Custo bruto</span>
          <strong class="metric-value">{{ formatCurrency(vm.grossCost) }}</strong>
          <span class="metric-footnote">{{ vm.grossMarginPercentage | number:'1.0-2' }}% de margem</span>
        </article>
        <article class="metric-card">
          <span class="metric-label">Recebiveis em aberto</span>
          <strong class="metric-value">{{ formatCurrency(vm.openReceivablesAmount) }}</strong>
          <span class="metric-footnote">{{ formatCurrency(vm.overdueReceivablesAmount) }} vencidos</span>
        </article>
        <article class="metric-card">
          <span class="metric-label">Payables em aberto</span>
          <strong class="metric-value">{{ formatCurrency(vm.openPayablesAmount) }}</strong>
          <span class="metric-footnote">{{ vm.receivedPurchaseOrders }} compras recebidas</span>
        </article>
      </section>

      <div class="grid cols-2">
        <section class="panel" *ngIf="overview() as vm">
          <div class="section-title compact">
            <div>
              <p class="eyebrow">Topline</p>
              <h3>Clientes e fornecedores</h3>
            </div>
          </div>

          <div class="stack">
            <article class="list-card" *ngFor="let item of vm.topCustomers">
              <strong>{{ item.customerName }}</strong>
              <p>Receita {{ formatCurrency(item.revenue) }} · Margem {{ formatCurrency(item.margin) }}</p>
            </article>
            <article class="list-card" *ngFor="let item of vm.topSuppliers">
              <strong>{{ item.supplierName }}</strong>
              <p>Spend {{ formatCurrency(item.spend) }}</p>
            </article>
          </div>
        </section>

        <section class="panel" *ngIf="overview() as vm">
          <div class="section-title compact">
            <div>
              <p class="eyebrow">Network</p>
              <h3>Filiais e centros</h3>
            </div>
          </div>

          <div class="stack">
            <article class="list-card" *ngFor="let item of vm.branchPerformance">
              <strong>{{ item.branchName }}</strong>
              <p>Receita {{ formatCurrency(item.revenue) }} · Recebiveis {{ formatCurrency(item.openReceivablesAmount) }}</p>
            </article>
            <article class="list-card" *ngFor="let item of vm.costCenterPerformance">
              <strong>{{ item.costCenterName }}</strong>
              <p>Receita {{ formatCurrency(item.revenue) }} · Spend {{ formatCurrency(item.spend) }}</p>
            </article>
          </div>
        </section>
      </div>

      <div class="grid cols-2">
        <section class="panel" *ngIf="turnover() as vm">
          <div class="section-title compact">
            <div>
              <p class="eyebrow">Inventory Turnover</p>
              <h3>Giro de estoque</h3>
            </div>
          </div>

          <div class="kpi-strip">
            <article class="kpi-chip">
              <small>Valor em estoque</small>
              <strong>{{ formatCurrency(vm.inventoryValue) }}</strong>
            </article>
            <article class="kpi-chip">
              <small>Entradas</small>
              <strong>{{ vm.inboundQuantity }}</strong>
            </article>
            <article class="kpi-chip">
              <small>Saidas</small>
              <strong>{{ vm.outboundQuantity }}</strong>
            </article>
            <article class="kpi-chip">
              <small>Razao de giro</small>
              <strong>{{ vm.stockTurnoverRatio | number:'1.0-2' }}</strong>
            </article>
          </div>
        </section>

        <section class="panel" *ngIf="overdue() as vm">
          <div class="section-title compact">
            <div>
              <p class="eyebrow">Collections</p>
              <h3>Recebiveis em atraso</h3>
            </div>
          </div>

          <div class="kpi-strip">
            <article class="kpi-chip">
              <small>Titulos abertos</small>
              <strong>{{ vm.totalOpenTitles }}</strong>
            </article>
            <article class="kpi-chip">
              <small>Vencidos</small>
              <strong>{{ vm.overdueTitles }}</strong>
            </article>
            <article class="kpi-chip">
              <small>Montante vencido</small>
              <strong>{{ formatCurrency(vm.overdueAmount) }}</strong>
            </article>
          </div>
        </section>
      </div>
    </section>
  `,
})
export class ReportingPageComponent {
  private readonly api = inject(ApiService);
  private readonly auth = inject(AuthStore);

  readonly overview = signal<ReportingOverview | null>(null);
  readonly turnover = signal<InventoryTurnoverReport | null>(null);
  readonly overdue = signal<OverdueReceivablesReport | null>(null);
  readonly branches = signal<Branch[]>([]);
  readonly costCenters = signal<CostCenter[]>([]);
  readonly formatCurrency = formatCurrency;
  readonly formatDateOnly = formatDateOnly;

  filters = {
    fromUtc: new Date(Date.now() - (30 * 24 * 60 * 60 * 1000)).toISOString().slice(0, 10),
    toUtc: new Date().toISOString().slice(0, 10),
    branchId: '',
    costCenterId: '',
  };

  constructor() {
    effect(() => {
      const branchId = this.auth.activeBranchId();
      if (branchId) {
        this.filters.branchId = branchId;
      }

      void this.loadSupport();
      void this.refresh();
    });
  }

  filteredCostCenters(): CostCenter[] {
    if (!this.filters.branchId) {
      return this.costCenters();
    }

    return this.costCenters().filter((item) => item.branchId === this.filters.branchId);
  }

  async refresh(): Promise<void> {
    const fromUtc = this.filters.fromUtc ? new Date(`${this.filters.fromUtc}T00:00:00`).toISOString() : null;
    const toUtc = this.filters.toUtc ? new Date(`${this.filters.toUtc}T23:59:59`).toISOString() : null;
    const branchId = this.filters.branchId || null;
    const costCenterId = this.filters.costCenterId || null;

    const [overview, turnover, overdue] = await Promise.all([
      firstValueFrom(this.api.getReportingOverview(fromUtc, toUtc, branchId, costCenterId)),
      firstValueFrom(this.api.getInventoryTurnover(fromUtc, toUtc, branchId)),
      firstValueFrom(this.api.getOverdueReceivables(branchId, costCenterId)),
    ]);

    this.overview.set(overview);
    this.turnover.set(turnover);
    this.overdue.set(overdue);
  }

  private async loadSupport(): Promise<void> {
    const [branches, costCenters] = await Promise.all([
      firstValueFrom(this.api.listBranches()),
      firstValueFrom(this.api.listCostCenters(this.auth.activeBranchId())),
    ]);

    this.branches.set(branches);
    this.costCenters.set(costCenters);
  }
}
