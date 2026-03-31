import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { Customer } from '../../core/app.models';
import { customerStatusOptions, formatStatus, toneForStatus } from '../../shared/ui.helpers';

@Component({
  selector: 'app-customers-page',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <section class="page">
      <header class="page-header">
        <div>
          <p class="eyebrow">Customers</p>
          <h1>Carteira comercial</h1>
          <p class="page-subtitle">
            Cadastro, qualificacao e manutencao da base de clientes utilizada por vendas e faturamento.
          </p>
        </div>
      </header>

      <div class="two-column-layout">
        <section class="panel">
          <div class="section-title compact">
            <div>
              <p class="eyebrow">Profile</p>
              <h3>{{ form.id ? 'Editar cliente' : 'Novo cliente' }}</h3>
            </div>
          </div>

          <form class="stack" (ngSubmit)="save()">
            <div class="form-grid">
              <label class="field">
                <span>Codigo</span>
                <input [(ngModel)]="form.code" name="customerCode" required />
              </label>
              <label class="field">
                <span>Nome</span>
                <input [(ngModel)]="form.name" name="customerName" required />
              </label>
              <label class="field">
                <span>Documento</span>
                <input [(ngModel)]="form.document" name="customerDocument" required />
              </label>
              <label class="field">
                <span>E-mail</span>
                <input [(ngModel)]="form.email" name="customerEmail" />
              </label>
              <label class="field">
                <span>Telefone</span>
                <input [(ngModel)]="form.phone" name="customerPhone" />
              </label>
              <label class="field">
                <span>Status</span>
                <select [(ngModel)]="form.status" name="customerStatus">
                  <option *ngFor="let option of customerStatusOptions" [ngValue]="option.value">
                    {{ option.label }}
                  </option>
                </select>
              </label>
            </div>
            <div class="form-actions">
              <button class="button" type="submit">Salvar cliente</button>
              <button class="button button-ghost" type="button" (click)="reset()">Limpar</button>
            </div>
          </form>
        </section>

        <section class="panel">
          <div class="section-title compact">
            <div>
              <p class="eyebrow">Portfolio</p>
              <h3>Base atual</h3>
            </div>
          </div>

          <div class="table-shell">
            <table>
              <thead>
                <tr>
                  <th>Codigo</th>
                  <th>Cliente</th>
                  <th>Status</th>
                  <th>Contato</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                <tr *ngFor="let customer of customers()">
                  <td>{{ customer.code }}</td>
                  <td>
                    {{ customer.name }}
                    <small>{{ customer.document }}</small>
                  </td>
                  <td>
                    <span class="badge" [ngClass]="toneForStatus(customer.status)">
                      {{ formatStatus(customer.status) }}
                    </span>
                  </td>
                  <td>{{ customer.email || customer.phone || '--' }}</td>
                  <td>
                    <button class="button button-ghost" type="button" (click)="edit(customer)">
                      Editar
                    </button>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </section>
      </div>
    </section>
  `,
})
export class CustomersPageComponent {
  private readonly api = inject(ApiService);

  readonly customers = signal<Customer[]>([]);
  readonly customerStatusOptions = customerStatusOptions;
  readonly formatStatus = formatStatus;
  readonly toneForStatus = toneForStatus;

  form = { id: '', code: '', name: '', document: '', email: '', phone: '', status: 1 };

  constructor() {
    void this.load();
  }

  async save(): Promise<void> {
    const payload = {
      code: this.form.code,
      name: this.form.name,
      document: this.form.document,
      email: this.form.email || null,
      phone: this.form.phone || null,
      status: this.form.status,
    };

    if (this.form.id) {
      await firstValueFrom(this.api.updateCustomer(this.form.id, payload));
    } else {
      await firstValueFrom(this.api.createCustomer(payload));
    }

    this.reset();
    await this.load();
  }

  edit(customer: Customer): void {
    this.form = {
      id: customer.id,
      code: customer.code,
      name: customer.name,
      document: customer.document,
      email: customer.email ?? '',
      phone: customer.phone ?? '',
      status: Number(customer.status),
    };
  }

  reset(): void {
    this.form = { id: '', code: '', name: '', document: '', email: '', phone: '', status: 1 };
  }

  private async load(): Promise<void> {
    this.customers.set(await firstValueFrom(this.api.listCustomers()));
  }
}
