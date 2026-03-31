import { CommonModule } from '@angular/common';
import { Component, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { AuthStore } from '../../core/auth.store';
import { Branch, CostCenter, Customer, Product, SalesOrder, Warehouse } from '../../core/app.models';
import { approvalStatusOptions, formatCurrency, formatDateTime, formatStatus, salesOrderStatusLabels, toneForStatus } from '../../shared/ui.helpers';

@Component({
  selector: 'app-sales-page',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <section class="page">
      <header class="page-header">
        <div>
          <p class="eyebrow">Sales</p>
          <h1>Pedidos e expedicao</h1>
          <p class="page-subtitle">
            Montagem de pedidos, aprovacao por politica, reserva de estoque e expedicao dentro do fluxo comercial.
          </p>
        </div>
      </header>

      <div class="two-column-layout">
        <section class="panel">
          <div class="section-title compact">
            <div>
              <p class="eyebrow">Order Composer</p>
              <h3>Novo pedido de venda</h3>
            </div>
          </div>

          <form class="stack" (ngSubmit)="createOrder()">
            <div class="form-grid">
              <label class="field">
                <span>Cliente</span>
                <select [(ngModel)]="form.customerId" name="salesCustomer" required>
                  <option value="">Selecione</option>
                  <option *ngFor="let customer of customers()" [value]="customer.id">{{ customer.code }} · {{ customer.name }}</option>
                </select>
              </label>
              <label class="field">
                <span>Filial</span>
                <select [(ngModel)]="form.branchId" name="salesBranch" required (ngModelChange)="syncCostCenters()">
                  <option value="">Selecione</option>
                  <option *ngFor="let branch of branches()" [value]="branch.id">{{ branch.code }} · {{ branch.name }}</option>
                </select>
              </label>
              <label class="field">
                <span>Centro de custo</span>
                <select [(ngModel)]="form.costCenterId" name="salesCostCenter">
                  <option value="">Opcional</option>
                  <option *ngFor="let costCenter of filteredCostCenters()" [value]="costCenter.id">{{ costCenter.code }} · {{ costCenter.name }}</option>
                </select>
              </label>
              <label class="field">
                <span>Warehouse</span>
                <select [(ngModel)]="form.warehouseLocationId" name="salesWarehouse" required>
                  <option value="">Selecione</option>
                  <option *ngFor="let warehouse of filteredWarehouses()" [value]="warehouse.id">{{ warehouse.code }} · {{ warehouse.name }}</option>
                </select>
              </label>
            </div>

            <label class="field">
              <span>Notas</span>
              <textarea [(ngModel)]="form.notes" name="salesNotes"></textarea>
            </label>

            <div class="stack">
              <div class="toolbar">
                <strong>Itens</strong>
                <button class="button button-ghost" type="button" (click)="addLine()">Adicionar linha</button>
              </div>

              <div class="panel subtle-panel" *ngFor="let line of form.items; let index = index">
                <div class="form-grid">
                  <label class="field">
                    <span>Produto</span>
                    <select [(ngModel)]="line.productId" [name]="'salesProduct' + index" required>
                      <option value="">Selecione</option>
                      <option *ngFor="let product of products()" [value]="product.id">{{ product.sku }} · {{ product.name }}</option>
                    </select>
                  </label>
                  <label class="field">
                    <span>Quantidade</span>
                    <input type="number" min="1" [(ngModel)]="line.quantity" [name]="'salesQuantity' + index" required />
                  </label>
                </div>
                <div class="form-actions">
                  <button class="button button-ghost" type="button" (click)="removeLine(index)" [disabled]="form.items.length === 1">
                    Remover linha
                  </button>
                </div>
              </div>
            </div>

            <div class="form-actions">
              <button class="button" type="submit">Criar pedido</button>
            </div>
          </form>
        </section>

        <section class="panel">
          <div class="section-title compact">
            <div>
              <p class="eyebrow">Pipeline</p>
              <h3>Pedidos atuais</h3>
            </div>
            <label class="field" style="min-width: 180px;">
              <span>Aprovacao</span>
              <select [(ngModel)]="approvalFilter" name="approvalFilter" (ngModelChange)="loadOrders()">
                <option value="">Todos</option>
                <option *ngFor="let option of approvalStatusOptions" [value]="option.value">{{ option.label }}</option>
              </select>
            </label>
          </div>

          <div class="stack">
            <article class="list-card" *ngFor="let order of orders()">
              <div class="toolbar">
                <strong>{{ order.orderNumber }}</strong>
                <span class="badge" [ngClass]="toneForStatus(order.status, salesOrderStatusLabels)">
                  {{ formatStatus(order.status, salesOrderStatusLabels) }}
                </span>
                <span class="badge" [ngClass]="toneForStatus(order.approvalStatus)">
                  {{ formatStatus(order.approvalStatus) }}
                </span>
              </div>
              <p>{{ order.customerName }} · {{ order.branchName || 'Global' }} · {{ order.warehouseName }}</p>
              <small>{{ formatCurrency(order.totalAmount) }} · {{ formatDateTime(order.createdAtUtc) }}</small>
              <div class="line-divider"></div>
              <div class="toolbar">
                <button class="button button-ghost" type="button" (click)="approve(order.id)">Aprovar</button>
                <button class="button button-ghost" type="button" (click)="reject(order.id)">Rejeitar</button>
                <button class="button button-ghost" type="button" (click)="dispatch(order.id)">Expedir</button>
                <button class="button button-ghost" type="button" (click)="cancel(order.id)">Cancelar</button>
              </div>
            </article>

            <p class="empty-state" *ngIf="!orders().length">
              Nenhum pedido localizado para a filial e filtro atuais.
            </p>
          </div>
        </section>
      </div>
    </section>
  `,
})
export class SalesPageComponent {
  private readonly api = inject(ApiService);
  private readonly auth = inject(AuthStore);

  readonly orders = signal<SalesOrder[]>([]);
  readonly customers = signal<Customer[]>([]);
  readonly products = signal<Product[]>([]);
  readonly warehouses = signal<Warehouse[]>([]);
  readonly branches = signal<Branch[]>([]);
  readonly costCenters = signal<CostCenter[]>([]);

  readonly approvalStatusOptions = approvalStatusOptions;
  readonly formatCurrency = formatCurrency;
  readonly formatDateTime = formatDateTime;
  readonly formatStatus = formatStatus;
  readonly toneForStatus = toneForStatus;
  readonly salesOrderStatusLabels = salesOrderStatusLabels;

  approvalFilter = '';
  form = {
    customerId: '',
    branchId: '',
    costCenterId: '',
    warehouseLocationId: '',
    notes: '',
    items: [{ productId: '', quantity: 1 }],
  };

  constructor() {
    effect(() => {
      const branchId = this.auth.activeBranchId();
      if (branchId) {
        this.form.branchId = branchId;
      }

      void this.loadContext();
      void this.loadOrders();
    });
  }

  filteredCostCenters(): CostCenter[] {
    if (!this.form.branchId) {
      return this.costCenters();
    }

    return this.costCenters().filter((item) => item.branchId === this.form.branchId);
  }

  filteredWarehouses(): Warehouse[] {
    if (!this.form.branchId) {
      return this.warehouses();
    }

    return this.warehouses().filter((item) => !item.branchId || item.branchId === this.form.branchId);
  }

  syncCostCenters(): void {
    if (this.form.costCenterId && !this.filteredCostCenters().some((item) => item.id === this.form.costCenterId)) {
      this.form.costCenterId = '';
    }

    if (this.form.warehouseLocationId && !this.filteredWarehouses().some((item) => item.id === this.form.warehouseLocationId)) {
      this.form.warehouseLocationId = '';
    }
  }

  addLine(): void {
    this.form.items.push({ productId: '', quantity: 1 });
  }

  removeLine(index: number): void {
    this.form.items.splice(index, 1);
  }

  async createOrder(): Promise<void> {
    await firstValueFrom(this.api.createSalesOrder({
      customerId: this.form.customerId,
      branchId: this.form.branchId,
      costCenterId: this.form.costCenterId || null,
      warehouseLocationId: this.form.warehouseLocationId,
      notes: this.form.notes || null,
      items: this.form.items.map((item) => ({ productId: item.productId, quantity: Number(item.quantity) })),
    }));

    this.form = {
      customerId: '',
      branchId: this.auth.activeBranchId() ?? '',
      costCenterId: '',
      warehouseLocationId: '',
      notes: '',
      items: [{ productId: '', quantity: 1 }],
    };

    await this.loadOrders();
  }

  async approve(id: string): Promise<void> {
    await firstValueFrom(this.api.approveSalesOrder(id));
    await this.loadOrders();
  }

  async reject(id: string): Promise<void> {
    const notes = window.prompt('Motivo da rejeicao:', 'Ajustar margem / validar credito');
    await firstValueFrom(this.api.rejectSalesOrder(id, { notes: notes || null }));
    await this.loadOrders();
  }

  async dispatch(id: string): Promise<void> {
    await firstValueFrom(this.api.dispatchSalesOrder(id));
    await this.loadOrders();
  }

  async cancel(id: string): Promise<void> {
    const notes = window.prompt('Motivo do cancelamento:', 'Cancelado pelo comercial');
    await firstValueFrom(this.api.cancelSalesOrder(id, { notes: notes || null }));
    await this.loadOrders();
  }

  async loadOrders(): Promise<void> {
    const branchId = this.auth.activeBranchId();
    const approvalStatus = this.approvalFilter === '' ? null : Number(this.approvalFilter);
    this.orders.set(await firstValueFrom(this.api.listSalesOrders(branchId, null, approvalStatus)));
  }

  private async loadContext(): Promise<void> {
    const branchId = this.auth.activeBranchId();
    const [customers, products, warehouses, branches, costCenters] = await Promise.all([
      firstValueFrom(this.api.listCustomers()),
      firstValueFrom(this.api.listProducts()),
      firstValueFrom(this.api.listWarehouses(branchId)),
      firstValueFrom(this.api.listBranches()),
      firstValueFrom(this.api.listCostCenters(branchId)),
    ]);

    this.customers.set(customers);
    this.products.set(products);
    this.warehouses.set(warehouses);
    this.branches.set(branches);
    this.costCenters.set(costCenters);

    this.syncCostCenters();
  }
}
