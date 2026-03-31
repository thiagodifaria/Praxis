import { CommonModule } from '@angular/common';
import { Component, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { AuthStore } from '../../core/auth.store';
import { InventoryBalance, Product, StockMovement, Warehouse } from '../../core/app.models';
import { formatCurrency, formatDateTime, formatStatus, stockMovementLabels, toneForStatus } from '../../shared/ui.helpers';

@Component({
  selector: 'app-inventory-page',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <section class="page">
      <header class="page-header">
        <div>
          <p class="eyebrow">Inventory</p>
          <h1>Estoque e movimentacoes</h1>
          <p class="page-subtitle">
            Saldos, warehouses e ajustes manuais com rastreabilidade para a operacao.
          </p>
        </div>
      </header>

      <div class="grid cols-3">
        <article class="metric-card" *ngFor="let warehouse of warehouses()">
          <span class="metric-label">{{ warehouse.code }}</span>
          <strong class="metric-value">{{ warehouse.name }}</strong>
          <span class="metric-footnote">{{ warehouse.branchName || 'Global' }} · {{ warehouse.description }}</span>
        </article>
      </div>

      <div class="two-column-layout">
        <section class="panel">
          <div class="section-title compact">
            <div>
              <p class="eyebrow">Adjustments</p>
              <h3>Ajuste operacional</h3>
            </div>
          </div>

          <form class="stack" (ngSubmit)="adjust()">
            <div class="form-grid">
              <label class="field">
                <span>Produto</span>
                <select [(ngModel)]="form.productId" name="inventoryProduct" required>
                  <option value="">Selecione</option>
                  <option *ngFor="let product of products()" [value]="product.id">{{ product.sku }} · {{ product.name }}</option>
                </select>
              </label>
              <label class="field">
                <span>Warehouse</span>
                <select [(ngModel)]="form.warehouseLocationId" name="inventoryWarehouse" required>
                  <option value="">Selecione</option>
                  <option *ngFor="let warehouse of warehouses()" [value]="warehouse.id">{{ warehouse.code }} · {{ warehouse.name }}</option>
                </select>
              </label>
              <label class="field">
                <span>Delta</span>
                <input type="number" [(ngModel)]="form.quantityDelta" name="inventoryDelta" required />
              </label>
            </div>
            <label class="field">
              <span>Motivo</span>
              <textarea [(ngModel)]="form.reason" name="inventoryReason" required></textarea>
            </label>
            <div class="form-actions">
              <button class="button" type="submit">Aplicar ajuste</button>
            </div>
          </form>
        </section>

        <section class="panel">
          <div class="section-title compact">
            <div>
              <p class="eyebrow">Balances</p>
              <h3>Saldos disponiveis</h3>
            </div>
          </div>

          <div class="table-shell">
            <table>
              <thead>
                <tr>
                  <th>Produto</th>
                  <th>Warehouse</th>
                  <th>On hand</th>
                  <th>Reservado</th>
                  <th>Disponivel</th>
                </tr>
              </thead>
              <tbody>
                <tr *ngFor="let balance of balances()">
                  <td>{{ balance.productName }} <small>{{ balance.productSku }}</small></td>
                  <td>{{ balance.warehouseName }}</td>
                  <td>{{ balance.onHandQuantity }}</td>
                  <td>{{ balance.reservedQuantity }}</td>
                  <td>
                    <span class="badge" [ngClass]="balance.availableQuantity <= balance.reorderLevel ? 'tone-warn' : 'tone-good'">
                      {{ balance.availableQuantity }}
                    </span>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </section>
      </div>

      <section class="panel">
        <div class="section-title compact">
          <div>
            <p class="eyebrow">Ledger</p>
            <h3>Historico de movimentacoes</h3>
          </div>
        </div>

        <div class="table-shell">
          <table>
            <thead>
              <tr>
                <th>Produto</th>
                <th>Warehouse</th>
                <th>Tipo</th>
                <th>Quantidade</th>
                <th>Motivo</th>
                <th>Data</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let movement of movements()">
                <td>{{ movement.productName }} <small>{{ movement.productSku }}</small></td>
                <td>{{ movement.warehouseName }}</td>
                <td>
                  <span class="badge" [ngClass]="toneForStatus(movement.type, stockMovementLabels)">
                    {{ formatStatus(movement.type, stockMovementLabels) }}
                  </span>
                </td>
                <td>{{ movement.quantity }}</td>
                <td>{{ movement.reason }}</td>
                <td>{{ formatDateTime(movement.createdAtUtc) }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>
    </section>
  `,
})
export class InventoryPageComponent {
  private readonly api = inject(ApiService);
  private readonly auth = inject(AuthStore);

  readonly warehouses = signal<Warehouse[]>([]);
  readonly balances = signal<InventoryBalance[]>([]);
  readonly movements = signal<StockMovement[]>([]);
  readonly products = signal<Product[]>([]);

  readonly formatCurrency = formatCurrency;
  readonly formatDateTime = formatDateTime;
  readonly formatStatus = formatStatus;
  readonly toneForStatus = toneForStatus;
  readonly stockMovementLabels = stockMovementLabels;

  form = {
    productId: '',
    warehouseLocationId: '',
    quantityDelta: 0,
    reason: '',
  };

  constructor() {
    effect(() => {
      void this.load();
    });
  }

  async adjust(): Promise<void> {
    await firstValueFrom(this.api.adjustInventory({
      productId: this.form.productId,
      warehouseLocationId: this.form.warehouseLocationId,
      quantityDelta: Number(this.form.quantityDelta),
      reason: this.form.reason,
    }));

    this.form = { productId: '', warehouseLocationId: '', quantityDelta: 0, reason: '' };
    await this.load();
  }

  private async load(): Promise<void> {
    const branchId = this.auth.activeBranchId();
    const [warehouses, balances, movements, products] = await Promise.all([
      firstValueFrom(this.api.listWarehouses(branchId)),
      firstValueFrom(this.api.listBalances(branchId)),
      firstValueFrom(this.api.listMovements(branchId)),
      firstValueFrom(this.api.listProducts()),
    ]);

    this.warehouses.set(warehouses);
    this.balances.set(balances);
    this.movements.set(movements);
    this.products.set(products);
  }
}
