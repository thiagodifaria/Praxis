import { CommonModule } from '@angular/common';
import { Component, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { AuthStore } from '../../core/auth.store';
import { Branch, CostCenter, Product, PurchaseOrder, Supplier, Warehouse } from '../../core/app.models';
import { approvalStatusOptions, formatCurrency, formatDateOnly, formatDateTime, formatStatus, purchaseOrderStatusLabels, toneForStatus } from '../../shared/ui.helpers';

@Component({
  selector: 'app-purchasing-page',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <section class="page">
      <header class="page-header">
        <div>
          <p class="eyebrow">Purchasing</p>
          <h1>Compras e recebimento</h1>
          <p class="page-subtitle">
            Planejamento de reposicao, aprovacao por alcada e recebimento com reflexo financeiro e de estoque.
          </p>
        </div>
      </header>

      <div class="two-column-layout">
        <section class="panel">
          <div class="section-title compact">
            <div>
              <p class="eyebrow">Inbound Order</p>
              <h3>Novo pedido de compra</h3>
            </div>
          </div>

          <form class="stack" (ngSubmit)="createOrder()">
            <div class="form-grid">
              <label class="field">
                <span>Fornecedor</span>
                <select [(ngModel)]="form.supplierId" name="purchaseSupplier" required>
                  <option value="">Selecione</option>
                  <option *ngFor="let supplier of suppliers()" [value]="supplier.id">{{ supplier.code }} · {{ supplier.name }}</option>
                </select>
              </label>
              <label class="field">
                <span>Filial</span>
                <select [(ngModel)]="form.branchId" name="purchaseBranch" required (ngModelChange)="syncContext()">
                  <option value="">Selecione</option>
                  <option *ngFor="let branch of branches()" [value]="branch.id">{{ branch.code }} · {{ branch.name }}</option>
                </select>
              </label>
              <label class="field">
                <span>Centro de custo</span>
                <select [(ngModel)]="form.costCenterId" name="purchaseCostCenter">
                  <option value="">Opcional</option>
                  <option *ngFor="let costCenter of filteredCostCenters()" [value]="costCenter.id">{{ costCenter.code }} · {{ costCenter.name }}</option>
                </select>
              </label>
              <label class="field">
                <span>Warehouse</span>
                <select [(ngModel)]="form.warehouseLocationId" name="purchaseWarehouse" required>
                  <option value="">Selecione</option>
                  <option *ngFor="let warehouse of filteredWarehouses()" [value]="warehouse.id">{{ warehouse.code }} · {{ warehouse.name }}</option>
                </select>
              </label>
              <label class="field">
                <span>Entrega prevista</span>
                <input type="date" [(ngModel)]="form.expectedDeliveryDateUtc" name="purchaseExpectedDate" />
              </label>
            </div>

            <label class="field">
              <span>Notas</span>
              <textarea [(ngModel)]="form.notes" name="purchaseNotes"></textarea>
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
                    <select [(ngModel)]="line.productId" [name]="'purchaseProduct' + index" required>
                      <option value="">Selecione</option>
                      <option *ngFor="let product of products()" [value]="product.id">{{ product.sku }} · {{ product.name }}</option>
                    </select>
                  </label>
                  <label class="field">
                    <span>Quantidade</span>
                    <input type="number" min="1" [(ngModel)]="line.quantity" [name]="'purchaseQuantity' + index" required />
                  </label>
                  <label class="field">
                    <span>Custo unitario</span>
                    <input type="number" min="0" step="0.01" [(ngModel)]="line.unitCost" [name]="'purchaseCost' + index" required />
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
              <button class="button" type="submit">Criar compra</button>
            </div>
          </form>
        </section>

        <section class="panel">
          <div class="section-title compact">
            <div>
              <p class="eyebrow">Inbound Flow</p>
              <h3>Pedidos atuais</h3>
            </div>
            <label class="field" style="min-width: 180px;">
              <span>Aprovacao</span>
              <select [(ngModel)]="approvalFilter" name="purchaseApprovalFilter" (ngModelChange)="loadOrders()">
                <option value="">Todos</option>
                <option *ngFor="let option of approvalStatusOptions" [value]="option.value">{{ option.label }}</option>
              </select>
            </label>
          </div>

          <div class="stack">
            <article class="list-card" *ngFor="let order of orders()">
              <div class="toolbar">
                <strong>{{ order.orderNumber }}</strong>
                <span class="badge" [ngClass]="toneForStatus(order.status, purchaseOrderStatusLabels)">
                  {{ formatStatus(order.status, purchaseOrderStatusLabels) }}
                </span>
                <span class="badge" [ngClass]="toneForStatus(order.approvalStatus)">
                  {{ formatStatus(order.approvalStatus) }}
                </span>
              </div>
              <p>{{ order.supplierName }} · {{ order.branchName || 'Global' }} · {{ order.warehouseName }}</p>
              <small>
                {{ formatCurrency(order.totalAmount) }}
                <span *ngIf="order.expectedDeliveryDateUtc"> · entrega {{ formatDateOnly(order.expectedDeliveryDateUtc) }}</span>
              </small>
              <div class="line-divider"></div>
              <div class="toolbar">
                <button class="button button-ghost" type="button" (click)="approve(order.id)">Aprovar</button>
                <button class="button button-ghost" type="button" (click)="reject(order.id)">Rejeitar</button>
                <button class="button button-ghost" type="button" (click)="receive(order)">Receber saldo</button>
                <button class="button button-ghost" type="button" (click)="cancel(order.id)">Cancelar</button>
              </div>
            </article>

            <p class="empty-state" *ngIf="!orders().length">
              Nenhuma compra localizada para a filial e filtro atuais.
            </p>
          </div>
        </section>
      </div>
    </section>
  `,
})
export class PurchasingPageComponent {
  private readonly api = inject(ApiService);
  private readonly auth = inject(AuthStore);

  readonly orders = signal<PurchaseOrder[]>([]);
  readonly suppliers = signal<Supplier[]>([]);
  readonly products = signal<Product[]>([]);
  readonly warehouses = signal<Warehouse[]>([]);
  readonly branches = signal<Branch[]>([]);
  readonly costCenters = signal<CostCenter[]>([]);

  readonly approvalStatusOptions = approvalStatusOptions;
  readonly formatCurrency = formatCurrency;
  readonly formatDateOnly = formatDateOnly;
  readonly formatDateTime = formatDateTime;
  readonly formatStatus = formatStatus;
  readonly toneForStatus = toneForStatus;
  readonly purchaseOrderStatusLabels = purchaseOrderStatusLabels;

  approvalFilter = '';
  form = {
    supplierId: '',
    branchId: '',
    costCenterId: '',
    warehouseLocationId: '',
    expectedDeliveryDateUtc: '',
    notes: '',
    items: [{ productId: '', quantity: 1, unitCost: 0 }],
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

  syncContext(): void {
    if (this.form.costCenterId && !this.filteredCostCenters().some((item) => item.id === this.form.costCenterId)) {
      this.form.costCenterId = '';
    }

    if (this.form.warehouseLocationId && !this.filteredWarehouses().some((item) => item.id === this.form.warehouseLocationId)) {
      this.form.warehouseLocationId = '';
    }
  }

  addLine(): void {
    this.form.items.push({ productId: '', quantity: 1, unitCost: 0 });
  }

  removeLine(index: number): void {
    this.form.items.splice(index, 1);
  }

  async createOrder(): Promise<void> {
    await firstValueFrom(this.api.createPurchaseOrder({
      supplierId: this.form.supplierId,
      branchId: this.form.branchId,
      costCenterId: this.form.costCenterId || null,
      warehouseLocationId: this.form.warehouseLocationId,
      expectedDeliveryDateUtc: this.form.expectedDeliveryDateUtc ? new Date(`${this.form.expectedDeliveryDateUtc}T00:00:00`).toISOString() : null,
      notes: this.form.notes || null,
      items: this.form.items.map((item) => ({
        productId: item.productId,
        quantity: Number(item.quantity),
        unitCost: Number(item.unitCost),
      })),
    }));

    this.form = {
      supplierId: '',
      branchId: this.auth.activeBranchId() ?? '',
      costCenterId: '',
      warehouseLocationId: '',
      expectedDeliveryDateUtc: '',
      notes: '',
      items: [{ productId: '', quantity: 1, unitCost: 0 }],
    };

    await this.loadOrders();
  }

  async approve(id: string): Promise<void> {
    await firstValueFrom(this.api.approvePurchaseOrder(id));
    await this.loadOrders();
  }

  async reject(id: string): Promise<void> {
    const notes = window.prompt('Motivo da rejeicao:', 'Revisar custo / fornecedor');
    await firstValueFrom(this.api.rejectPurchaseOrder(id, { notes: notes || null }));
    await this.loadOrders();
  }

  async receive(order: PurchaseOrder): Promise<void> {
    const items = order.items
      .map((item) => ({
        productId: item.productId,
        quantity: item.quantity - item.receivedQuantity,
      }))
      .filter((item) => item.quantity > 0);

    if (!items.length) {
      return;
    }

    const dueDate = window.prompt(
      'Data de vencimento do payable (YYYY-MM-DD):',
      new Date(Date.now() + (21 * 24 * 60 * 60 * 1000)).toISOString().slice(0, 10),
    );

    if (!dueDate) {
      return;
    }

    await firstValueFrom(this.api.receivePurchaseOrder(order.id, {
      receivedAtUtc: new Date().toISOString(),
      dueDateUtc: new Date(`${dueDate}T00:00:00`).toISOString(),
      notes: 'Recebimento integral via client-web',
      items,
    }));

    await this.loadOrders();
  }

  async cancel(id: string): Promise<void> {
    const notes = window.prompt('Motivo do cancelamento:', 'Replanejado para novo fornecedor');
    await firstValueFrom(this.api.cancelPurchaseOrder(id, { notes: notes || null }));
    await this.loadOrders();
  }

  async loadOrders(): Promise<void> {
    const branchId = this.auth.activeBranchId();
    const approvalStatus = this.approvalFilter === '' ? null : Number(this.approvalFilter);
    this.orders.set(await firstValueFrom(this.api.listPurchaseOrders(branchId, null, approvalStatus)));
  }

  private async loadContext(): Promise<void> {
    const branchId = this.auth.activeBranchId();
    const [suppliers, products, warehouses, branches, costCenters] = await Promise.all([
      firstValueFrom(this.api.listSuppliers()),
      firstValueFrom(this.api.listProducts()),
      firstValueFrom(this.api.listWarehouses(branchId)),
      firstValueFrom(this.api.listBranches()),
      firstValueFrom(this.api.listCostCenters(branchId)),
    ]);

    this.suppliers.set(suppliers);
    this.products.set(products);
    this.warehouses.set(warehouses);
    this.branches.set(branches);
    this.costCenters.set(costCenters);

    this.syncContext();
  }
}
