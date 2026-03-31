import { CommonModule } from '@angular/common';
import { Component, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { AuthStore } from '../../core/auth.store';
import { ApprovalQueueItem, AuditEntry, OperationalAlert } from '../../core/app.models';
import { alertSeverityLabels, alertStatusLabels, approvalDecisionOptions, approvalModuleOptions, formatCurrency, formatDateTime, formatStatus, toneForStatus } from '../../shared/ui.helpers';

@Component({
  selector: 'app-operations-page',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <section class="page">
      <header class="page-header">
        <div>
          <p class="eyebrow">Operations</p>
          <h1>Governanca e auditoria</h1>
          <p class="page-subtitle">
            Fila de aprovacao, alertas abertos e trilha de auditoria para supervisao operacional.
          </p>
        </div>
      </header>

      <div class="grid cols-2">
        <section class="panel">
          <div class="section-title compact">
            <div>
              <p class="eyebrow">Approvals</p>
              <h3>Fila de decisao</h3>
            </div>
          </div>

          <div class="stack">
            <article class="list-card" *ngFor="let item of approvals()">
              <div class="toolbar">
                <strong>{{ item.referenceNumber }}</strong>
                <span class="badge" [ngClass]="toneForStatus(item.status)">
                  {{ formatStatus(item.status) }}
                </span>
              </div>
              <p>
                {{ approvalModuleLabel(item.module) }} · {{ item.requiredRoleName }} ·
                {{ formatCurrency(item.requestedAmount) }}
              </p>
              <small>{{ item.branchName || 'Global' }} · {{ formatDateTime(item.requestedAtUtc) }}</small>
              <div class="form-actions">
                <button class="button button-ghost" type="button" (click)="approve(item)">Aprovar</button>
                <button class="button button-ghost" type="button" (click)="reject(item)">Rejeitar</button>
              </div>
            </article>
            <p class="empty-state" *ngIf="!approvals().length">Nenhuma aprovacao pendente no momento.</p>
          </div>
        </section>

        <section class="panel">
          <div class="section-title compact">
            <div>
              <p class="eyebrow">Alerts</p>
              <h3>Alertas operacionais</h3>
            </div>
          </div>

          <div class="stack">
            <article class="list-card" *ngFor="let alert of alerts()">
              <div class="toolbar">
                <strong>{{ alert.code }}</strong>
                <span class="badge" [ngClass]="toneForStatus(alert.severity, alertSeverityLabels)">
                  {{ formatStatus(alert.severity, alertSeverityLabels) }}
                </span>
                <span class="badge" [ngClass]="toneForStatus(alert.status, alertStatusLabels)">
                  {{ formatStatus(alert.status, alertStatusLabels) }}
                </span>
              </div>
              <p>{{ alert.title }}</p>
              <small>{{ alert.branchName || 'Global' }} · {{ formatDateTime(alert.createdAtUtc) }}</small>
              <div class="form-actions" *ngIf="isAlertOpen(alert)">
                <button class="button button-ghost" type="button" (click)="resolve(alert.id)">Resolver</button>
              </div>
            </article>
          </div>
        </section>
      </div>

      <section class="panel">
        <div class="section-title compact">
          <div>
            <p class="eyebrow">Audit Trail</p>
            <h3>Eventos recentes</h3>
          </div>
        </div>

        <div class="table-shell">
          <table>
            <thead>
              <tr>
                <th>Evento</th>
                <th>Entidade</th>
                <th>Actor</th>
                <th>Data</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let item of auditEntries()">
                <td>{{ item.eventType }}</td>
                <td>{{ item.entityName }} <small>{{ item.entityId }}</small></td>
                <td>{{ item.actorName || '--' }}</td>
                <td>{{ formatDateTime(item.createdAtUtc) }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>
    </section>
  `,
})
export class OperationsPageComponent {
  private readonly api = inject(ApiService);
  private readonly auth = inject(AuthStore);

  readonly approvals = signal<ApprovalQueueItem[]>([]);
  readonly alerts = signal<OperationalAlert[]>([]);
  readonly auditEntries = signal<AuditEntry[]>([]);
  readonly approvalModuleOptions = approvalModuleOptions;
  readonly approvalDecisionOptions = approvalDecisionOptions;
  readonly formatCurrency = formatCurrency;
  readonly formatDateTime = formatDateTime;
  readonly formatStatus = formatStatus;
  readonly toneForStatus = toneForStatus;
  readonly alertSeverityLabels = alertSeverityLabels;
  readonly alertStatusLabels = alertStatusLabels;

  constructor() {
    effect(() => {
      void this.load();
    });
  }

  approvalModuleLabel(value: string | number): string {
    return this.approvalModuleOptions.find((option) => option.value === Number(value))?.label ?? String(value);
  }

  isAlertOpen(alert: OperationalAlert): boolean {
    return Number(alert.status) === 0;
  }

  async approve(item: ApprovalQueueItem): Promise<void> {
    if (Number(item.module) === 0) {
      await firstValueFrom(this.api.approveSalesOrder(item.entityId));
    } else {
      await firstValueFrom(this.api.approvePurchaseOrder(item.entityId));
    }

    await this.load();
  }

  async reject(item: ApprovalQueueItem): Promise<void> {
    const notes = window.prompt('Motivo da rejeicao:', 'Fora da politica atual');
    if (Number(item.module) === 0) {
      await firstValueFrom(this.api.rejectSalesOrder(item.entityId, { notes: notes || null }));
    } else {
      await firstValueFrom(this.api.rejectPurchaseOrder(item.entityId, { notes: notes || null }));
    }

    await this.load();
  }

  async resolve(id: string): Promise<void> {
    await firstValueFrom(this.api.resolveAlert(id));
    await this.load();
  }

  private async load(): Promise<void> {
    const branchId = this.auth.activeBranchId();
    const [approvals, alerts, auditEntries] = await Promise.all([
      firstValueFrom(this.api.listApprovalQueue(0, null, branchId)),
      firstValueFrom(this.api.listAlerts(branchId, true)),
      firstValueFrom(this.api.listAuditEntries(null, null, 30)),
    ]);

    this.approvals.set(approvals);
    this.alerts.set(alerts);
    this.auditEntries.set(auditEntries);
  }
}
