import { CommonModule } from '@angular/common';
import { Component, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { AuthStore } from '../../core/auth.store';
import { Invoice, Payable, Receivable, SalesOrder } from '../../core/app.models';
import { financialStatusLabels, formatCurrency, formatDateOnly, formatDateTime, formatStatus, invoiceStatusLabels, salesOrderStatusLabels, toneForStatus } from '../../shared/ui.helpers';

@Component({
  selector: 'app-billing-page',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <section class="page">
      <header class="page-header">
        <div>
          <p class="eyebrow">Billing & Finance</p>
          <h1>Faturamento e liquidacao</h1>
          <p class="page-subtitle">
            Emissao de faturas a partir da expedicao e administracao dos titulos de contas a receber e pagar.
          </p>
        </div>
      </header>

      <div class="two-column-layout">
        <section class="panel">
          <div class="section-title compact">
            <div>
              <p class="eyebrow">Invoicing</p>
              <h3>Emitir fatura</h3>
            </div>
          </div>

          <form class="stack" (ngSubmit)="issueInvoice()">
            <div class="form-grid">
              <label class="field">
                <span>Pedido expedido</span>
                <select [(ngModel)]="form.salesOrderId" name="billingSalesOrder" required>
                  <option value="">Selecione</option>
                  <option *ngFor="let order of dispatchableOrders()" [value]="order.id">
                    {{ order.orderNumber }} · {{ order.customerName }}
                  </option>
                </select>
              </label>
              <label class="field">
                <span>Vencimento</span>
                <input type="date" [(ngModel)]="form.dueDateUtc" name="billingDueDate" required />
              </label>
            </div>

            <label class="field">
              <span>Notas</span>
              <textarea [(ngModel)]="form.notes" name="billingNotes"></textarea>
            </label>

            <div class="form-actions">
              <button class="button" type="submit">Emitir fatura</button>
            </div>
          </form>
        </section>

        <section class="panel">
          <div class="section-title compact">
            <div>
              <p class="eyebrow">Invoices</p>
              <h3>Documentos emitidos</h3>
            </div>
          </div>

          <div class="stack">
            <article class="list-card" *ngFor="let invoice of invoices()">
              <div class="toolbar">
                <strong>{{ invoice.invoiceNumber }}</strong>
                <span class="badge" [ngClass]="toneForStatus(invoice.status, invoiceStatusLabels)">
                  {{ formatStatus(invoice.status, invoiceStatusLabels) }}
                </span>
              </div>
              <p>{{ invoice.customerName }} · {{ invoice.salesOrderNumber }}</p>
              <small>{{ formatCurrency(invoice.totalAmount) }} · vence {{ formatDateOnly(invoice.dueDateUtc) }}</small>
              <div class="form-actions">
                <button class="button button-ghost" type="button" (click)="cancelInvoice(invoice.id)">Cancelar</button>
              </div>
            </article>
          </div>
        </section>
      </div>

      <div class="grid cols-2">
        <section class="panel">
          <div class="section-title compact">
            <div>
              <p class="eyebrow">Receivables</p>
              <h3>Carteira a receber</h3>
            </div>
          </div>

          <div class="table-shell">
            <table>
              <thead>
                <tr>
                  <th>Titulo</th>
                  <th>Cliente</th>
                  <th>Status</th>
                  <th>Aberto</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                <tr *ngFor="let item of receivables()">
                  <td>{{ item.documentNumber }}</td>
                  <td>{{ item.customerName }}</td>
                  <td>
                    <span class="badge" [ngClass]="toneForStatus(item.status, financialStatusLabels)">
                      {{ formatStatus(item.status, financialStatusLabels) }}
                    </span>
                  </td>
                  <td>{{ formatCurrency(item.outstandingAmount) }}</td>
                  <td><button class="button button-ghost" type="button" (click)="settleReceivable(item)">Liquidar</button></td>
                </tr>
              </tbody>
            </table>
          </div>
        </section>

        <section class="panel">
          <div class="section-title compact">
            <div>
              <p class="eyebrow">Payables</p>
              <h3>Carteira a pagar</h3>
            </div>
          </div>

          <div class="table-shell">
            <table>
              <thead>
                <tr>
                  <th>Titulo</th>
                  <th>Fornecedor</th>
                  <th>Status</th>
                  <th>Aberto</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                <tr *ngFor="let item of payables()">
                  <td>{{ item.documentNumber }}</td>
                  <td>{{ item.supplierName }}</td>
                  <td>
                    <span class="badge" [ngClass]="toneForStatus(item.status, financialStatusLabels)">
                      {{ formatStatus(item.status, financialStatusLabels) }}
                    </span>
                  </td>
                  <td>{{ formatCurrency(item.outstandingAmount) }}</td>
                  <td><button class="button button-ghost" type="button" (click)="settlePayable(item)">Liquidar</button></td>
                </tr>
              </tbody>
            </table>
          </div>
        </section>
      </div>
    </section>
  `,
})
export class BillingPageComponent {
  private readonly api = inject(ApiService);
  private readonly auth = inject(AuthStore);

  readonly invoices = signal<Invoice[]>([]);
  readonly receivables = signal<Receivable[]>([]);
  readonly payables = signal<Payable[]>([]);
  readonly salesOrders = signal<SalesOrder[]>([]);

  readonly formatCurrency = formatCurrency;
  readonly formatDateOnly = formatDateOnly;
  readonly formatDateTime = formatDateTime;
  readonly formatStatus = formatStatus;
  readonly toneForStatus = toneForStatus;
  readonly invoiceStatusLabels = invoiceStatusLabels;
  readonly financialStatusLabels = financialStatusLabels;
  readonly salesOrderStatusLabels = salesOrderStatusLabels;

  form = {
    salesOrderId: '',
    dueDateUtc: new Date(Date.now() + (15 * 24 * 60 * 60 * 1000)).toISOString().slice(0, 10),
    notes: '',
  };

  constructor() {
    effect(() => {
      void this.load();
    });
  }

  dispatchableOrders(): SalesOrder[] {
    return this.salesOrders().filter((item) => Number(item.status) === 2);
  }

  async issueInvoice(): Promise<void> {
    await firstValueFrom(this.api.issueInvoice({
      salesOrderId: this.form.salesOrderId,
      dueDateUtc: new Date(`${this.form.dueDateUtc}T00:00:00`).toISOString(),
      notes: this.form.notes || null,
    }));

    this.form = {
      salesOrderId: '',
      dueDateUtc: new Date(Date.now() + (15 * 24 * 60 * 60 * 1000)).toISOString().slice(0, 10),
      notes: '',
    };

    await this.load();
  }

  async cancelInvoice(id: string): Promise<void> {
    const notes = window.prompt('Motivo do cancelamento:', 'Documento emitido em duplicidade');
    await firstValueFrom(this.api.cancelInvoice(id, { notes: notes || null }));
    await this.load();
  }

  async settleReceivable(item: Receivable): Promise<void> {
    const amount = window.prompt('Valor da liquidacao:', String(item.outstandingAmount));
    if (!amount) {
      return;
    }

    const paymentMethod = window.prompt('Metodo de pagamento:', 'pix');
    if (!paymentMethod) {
      return;
    }

    await firstValueFrom(this.api.settleReceivable(item.id, {
      amount: Number(amount),
      paidAtUtc: new Date().toISOString(),
      paymentMethod,
      notes: 'Liquidado via client-web',
    }));

    await this.load();
  }

  async settlePayable(item: Payable): Promise<void> {
    const amount = window.prompt('Valor da liquidacao:', String(item.outstandingAmount));
    if (!amount) {
      return;
    }

    const paymentMethod = window.prompt('Metodo de pagamento:', 'ted');
    if (!paymentMethod) {
      return;
    }

    await firstValueFrom(this.api.settlePayable(item.id, {
      amount: Number(amount),
      paidAtUtc: new Date().toISOString(),
      paymentMethod,
      notes: 'Liquidado via client-web',
    }));

    await this.load();
  }

  private async load(): Promise<void> {
    const branchId = this.auth.activeBranchId();
    const [invoices, receivables, payables, salesOrders] = await Promise.all([
      firstValueFrom(this.api.listInvoices(branchId)),
      firstValueFrom(this.api.listReceivables(branchId)),
      firstValueFrom(this.api.listPayables(branchId)),
      firstValueFrom(this.api.listSalesOrders(branchId)),
    ]);

    this.invoices.set(invoices);
    this.receivables.set(receivables);
    this.payables.set(payables);
    this.salesOrders.set(salesOrders);
  }
}
